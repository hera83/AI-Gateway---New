using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class GenerateRequestDto
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("suffix")]
    public string? Suffix { get; set; }

    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }

    [JsonPropertyName("format")]
    public JsonElement? Format { get; set; }

    [JsonPropertyName("options")]
    public OllamaOptionsDto? Options { get; set; }

    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("template")]
    public string? Template { get; set; }

    [JsonPropertyName("context")]
    public List<long>? Context { get; set; }

    [JsonPropertyName("raw")]
    public bool? Raw { get; set; }

    [JsonPropertyName("keep_alive")]
    public string? KeepAlive { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}
