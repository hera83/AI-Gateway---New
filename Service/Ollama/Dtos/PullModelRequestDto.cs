using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class PullModelRequestDto
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("insecure")]
    public bool? Insecure { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}
