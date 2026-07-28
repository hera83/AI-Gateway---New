using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class OllamaModelSummaryDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("modified_at")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("digest")]
    public string? Digest { get; set; }

    [JsonPropertyName("details")]
    public OllamaModelDetailsDto? Details { get; set; }
}
