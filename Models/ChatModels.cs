using System.Text.Json.Serialization;

namespace blazor_spreadsheet_agent.Models
{
    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";
        
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "gpt-4-turbo-preview";
        
        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = new();
        
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
        
        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; } = 500;
    }

    public class ChatResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatChoice> Choices { get; set; } = new();
        
        [JsonPropertyName("usage")]
        public ChatUsage? Usage { get; set; }
    }

    public class ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
        
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class ChatUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }
        
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
        
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
