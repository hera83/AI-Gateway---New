using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class ChatCompletionResultDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<ChatCompletionChoiceDto> Choices { get; set; } = [];

    [JsonPropertyName("usage")]
    public ChatCompletionUsageDto? Usage { get; set; }

    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; set; }

    [JsonPropertyName("service_tier")]
    public string? ServiceTier { get; set; }
}
