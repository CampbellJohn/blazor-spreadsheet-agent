using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using blazor_spreadsheet_agent.Models;

public class OpenAIService : IAsyncDisposable
{
    private readonly ILogger<OpenAIService> _logger;
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private bool _disposed;
    private const string SystemPrompt = @"You are a helpful AI assistant that helps users with their questions and tasks. 
Be concise, helpful, and friendly in your responses. If you don't know something, just say so.";

    public OpenAIService(ILogger<OpenAIService> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _ = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        
        _apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey configuration value is missing");
        
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("OpenAI API key not found in configuration");
            throw new InvalidOperationException("OpenAI API key is not configured");
        }
        
        _httpClient = httpClientFactory?.CreateClient() ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> GenerateSqlQueryAsync(string naturalLanguageQuery, string tableName, string schemaInfo)
    {
        if (string.IsNullOrWhiteSpace(naturalLanguageQuery))
            throw new ArgumentException("Query cannot be empty", nameof(naturalLanguageQuery));
            
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be empty", nameof(tableName));
            
        if (string.IsNullOrWhiteSpace(schemaInfo))
            throw new ArgumentException("Schema info cannot be empty", nameof(schemaInfo));

        try
        {
            var prompt = BuildPrompt(naturalLanguageQuery, tableName, schemaInfo);
            
            var requestBody = new
            {
                model = "gpt-4-turbo-preview",
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = prompt }
                },
                temperature = 0.2,
                max_tokens = 500
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                _logger.LogError("Received empty response from OpenAI API");
                throw new InvalidOperationException("Received empty response from OpenAI API");
            }

            using var jsonResponse = JsonDocument.Parse(responseContent);
            var root = jsonResponse.RootElement;
            
            if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                _logger.LogError("No choices in response from OpenAI API");
                throw new InvalidOperationException("No choices in response from OpenAI API");
            }

            var firstChoice = choices[0];
            if (!firstChoice.TryGetProperty("message", out var messageElement) || 
                !messageElement.TryGetProperty("content", out var contentElement))
            {
                _logger.LogError("Invalid response format from OpenAI API: {Response}", responseContent);
                throw new InvalidOperationException("Invalid response format from OpenAI API");
            }
            
            var sqlQuery = contentElement.GetString()?.Trim() ?? 
                throw new InvalidOperationException("Content is null in the response from OpenAI API");
            
            // Basic validation of the SQL query
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                _logger.LogWarning("Generated SQL query is null or empty");
                throw new InvalidOperationException("Generated SQL query is empty");
            }

            return sqlQuery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SQL query");
            throw new ApplicationException("Failed to generate SQL query. Please try again.", ex);
        }
    }

    private static string BuildPrompt(string naturalLanguageQuery, string tableName, string schemaInfo)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine($"""
            I have a SQL table named "{tableName}" with the following schema:
            {schemaInfo}

            Please generate a SQL query for the following question:
            "{naturalLanguageQuery}"

            Respond with only the SQL query, no explanations or markdown formatting.
            The query should be properly formatted and easy to read.
            """);

        return prompt.ToString();
    }

    public async Task<string> GenerateChatResponseAsync(string userMessage, List<ChatMessage>? conversationHistory = null)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message cannot be empty", nameof(userMessage));

        try
        {
            var messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = SystemPrompt }
            };

            // Add conversation history if provided
            if (conversationHistory?.Any() == true)
            {
                messages.AddRange(conversationHistory);
            }

            // Add the current user message
            messages.Add(new ChatMessage { Role = "user", Content = userMessage });

            var request = new ChatRequest
            {
                Messages = messages,
                Temperature = 0.7,
                MaxTokens = 1000
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                _logger.LogError("Received empty response from OpenAI API");
                throw new InvalidOperationException("Received empty response from OpenAI API");
            }

            var chatResponse = JsonSerializer.Deserialize<ChatResponse>(
                responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (chatResponse?.Choices?.FirstOrDefault()?.Message?.Content is not { } assistantResponse)
            {
                _logger.LogError("Invalid response format from OpenAI API: {Response}", responseContent);
                throw new InvalidOperationException("Invalid response format from OpenAI API");
            }

            _logger.LogInformation("Successfully generated chat response");
            return assistantResponse.Trim();
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP error while calling OpenAI API");
            throw new ApplicationException("Failed to communicate with the AI service. Please check your connection and try again.", httpEx);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Error parsing OpenAI API response");
            throw new ApplicationException("Error processing the AI service response. Please try again.", jsonEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating chat response");
            throw new ApplicationException("An unexpected error occurred. Please try again later.", ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
            _disposed = true;
        }
        await Task.CompletedTask;
    }
}
