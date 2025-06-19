using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

namespace blazor_spreadsheet_agent.Services;

public class OpenAIService
{
    private readonly ILogger<OpenAIService> _logger;
    private readonly OpenAIAPI _api;
    private const string SystemPrompt = @"You are a SQL query generator. Your task is to convert natural language questions into SQL queries.

Rules:
1. Only generate SELECT queries
2. Always limit the results to 100 rows unless specified otherwise
3. Never include DROP, DELETE, or other destructive operations
4. Use parameterized queries where possible
5. Handle date formats carefully (use ISO 8601 format: YYYY-MM-DD)
6. For string comparisons, use LOWER() for case-insensitive matching
7. Always include an ORDER BY clause for consistent results
8. If the query involves dates, always include a date range filter";

    public OpenAIService(ILogger<OpenAIService> logger, IConfiguration configuration)
    {
        _logger = logger;
        var apiKey = configuration["OpenAI:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("OpenAI API key not found in configuration");
            return;
        }
        
        _api = new OpenAIAPI(apiKey);
    }

    public async Task<string> GenerateSqlQueryAsync(string naturalLanguageQuery, string tableName, string schemaInfo)
    {
        try
        {
            if (_api == null)
            {
                _logger.LogError("OpenAI API client is not initialized. Please check your API key configuration.");
                throw new InvalidOperationException("OpenAI API client is not initialized. Please check your API key configuration.");
            }

            var prompt = BuildPrompt(naturalLanguageQuery, tableName, schemaInfo);
            
            var chatRequest = new ChatRequest
            {
                Model = Model.GPT4_Turbo,
                Messages = new List<ChatMessage>
                {
                    new(ChatMessageRole.System, SystemPrompt),
                    new(ChatMessageRole.User, prompt)
                },
                Temperature = 0.2,
                MaxTokens = 500
            };

            var response = await _api.Chat.CreateChatCompletionAsync(chatRequest);
            var sqlQuery = response.Choices[0].Message.Content;
            
            // Basic validation of the SQL query
            if (!IsValidSqlQuery(sqlQuery))
            {
                _logger.LogWarning("Generated SQL query validation failed: {sqlQuery}", sqlQuery);
                throw new InvalidOperationException("The generated SQL query is not valid.");
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

    private static bool IsValidSqlQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;

        // Basic validation - check for common SQL injection patterns
        var forbiddenPatterns = new[]
        {
            "--", "/*", "*/", ";", "DROP ", "DELETE ", "TRUNCATE ", "UPDATE ", "INSERT ",
            "EXEC ", "EXECUTE ", "DECLARE ", "XP_", "sp_
        };

        var upperQuery = query.ToUpperInvariant();
        return !forbiddenPatterns.Any(upperQuery.Contains);
    }
}
