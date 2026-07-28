using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class CopyModelRequestDto
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("destination")]
    public string Destination { get; set; } = string.Empty;
}
