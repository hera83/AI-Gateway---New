using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class ChatRequestDto
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OllamaMessageDto> Messages { get; set; } = [];

    [JsonPropertyName("tools")]
    public List<OllamaToolDto>? Tools { get; set; }

    [JsonPropertyName("format")]
    public JsonElement? Format { get; set; }

    [JsonPropertyName("options")]
    public OllamaOptionsDto? Options { get; set; }

    [JsonPropertyName("keep_alive")]
    public string? KeepAlive { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}
