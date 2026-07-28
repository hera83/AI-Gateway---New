using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class EmbedRequestDto
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public List<string> Input { get; set; } = [];

    [JsonPropertyName("options")]
    public OllamaOptionsDto? Options { get; set; }

    [JsonPropertyName("keep_alive")]
    public string? KeepAlive { get; set; }
}
