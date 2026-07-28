using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class ChatCompletionUsageDto
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("prompt_tokens_details")]
    public JsonElement? PromptTokensDetails { get; set; }

    [JsonPropertyName("completion_tokens_details")]
    public JsonElement? CompletionTokensDetails { get; set; }
}
