using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Threading;
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
                max_tokens = 500,
                stream = true
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                CancellationToken.None);
                
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(CancellationToken.None);
            using var reader = new StreamReader(stream);

            var buffer = new StringBuilder();
            while (!CancellationToken.None.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(CancellationToken.None);
                if (line == null) break;

                if (line.StartsWith("data: "))
                {
                    var eventData = line[6..]; // Remove 'data: ' prefix
                    if (eventData == "[DONE]")
                    {
                        break;
                    }

                    using var jsonDoc = JsonDocument.Parse(eventData);
                    var choice = jsonDoc.RootElement.GetProperty("choices")[0];
                    if (choice.TryGetProperty("delta", out var delta) && 
                        delta.TryGetProperty("content", out var content))
                    {
                        var contentValue = content.GetString();
                        if (!string.IsNullOrEmpty(contentValue))
                        {
                            buffer.Append(contentValue);
                        }
                    }
                }
            }
            
            // Basic validation of the SQL query
            var sqlQuery = buffer.ToString();
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                _logger.LogWarning("Generated SQL query is null or empty");
                throw new InvalidOperationException("Generated SQL query is empty");
            }

            return sqlQuery;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error calling OpenAI API");
            throw new ApplicationException("Failed to communicate with the AI service. Please check your connection and try again.", httpEx);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Error parsing OpenAI API response");
            throw new ApplicationException("Error processing the AI service response. Please try again.", jsonEx);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Streaming was canceled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating chat response");
            throw new ApplicationException("An unexpected error occurred. Please try again later.", ex);
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

    public IAsyncEnumerable<string> StreamChatResponseAsync(string userMessage, List<ChatMessage>? conversationHistory = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("Message cannot be empty", nameof(userMessage));

        return StreamChatResponseInternalAsync(userMessage, conversationHistory, cancellationToken);
    }

    private async IAsyncEnumerable<string> StreamChatResponseInternalAsync(string userMessage, List<ChatMessage>? conversationHistory, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
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

        var requestBody = new
        {
            model = "gpt-4-turbo-preview",
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = 0.7,
            max_tokens = 1000,
            stream = true
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
            
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var buffer = new StringBuilder();
        var eventData = new StringBuilder();
        var inDataSection = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break; // End of stream

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
            {
                if (inDataSection && eventData.Length > 0)
                {
                    // Process the complete event
                    var eventStr = eventData.ToString();
                    eventData.Clear();
                    inDataSection = false;

                    if (eventStr == "[DONE]")
                    {
                        _logger.LogDebug("Received [DONE] event");
                        break;
                    }

                    string? contentValue = null;
                    bool hasError = false;
                    
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(eventStr);
                        if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) && 
                            choices.GetArrayLength() > 0)
                        {
                            var choice = choices[0];
                            if (choice.TryGetProperty("delta", out var delta) && 
                                delta.TryGetProperty("content", out var content) &&
                                content.ValueKind != JsonValueKind.Null)
                            {
                                contentValue = content.GetString();
                            }
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Error parsing JSON from OpenAI stream: {EventData}", eventStr);
                        hasError = true;
                    }
                    
                    if (!hasError && !string.IsNullOrEmpty(contentValue))
                    {
                        buffer.Append(contentValue);
                        _logger.LogDebug("Yielding content chunk: {Chunk}", contentValue);
                        yield return contentValue;
                    }
                }
                continue;
            }

            // Check for data section
            if (line.StartsWith("data: "))
            {
                inDataSection = true;
                eventData.Append(line[6..].Trim()); // Remove 'data: ' prefix and trim
            }
            else if (inDataSection)
            {
                // If we're in a data section but the line doesn't start with 'data: ',
                // it's a continuation of the JSON data (shouldn't happen with current API but being defensive)
                eventData.Append(line.Trim());
            }
        }
        
        _logger.LogInformation("Successfully generated chat response");
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
