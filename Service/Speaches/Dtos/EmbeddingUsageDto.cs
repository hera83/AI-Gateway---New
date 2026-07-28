using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class EmbeddingUsageDto
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
