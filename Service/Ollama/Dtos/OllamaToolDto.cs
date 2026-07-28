using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class OllamaToolDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OllamaToolFunctionDto Function { get; set; } = new();
}

public class OllamaToolFunctionDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    public JsonElement Parameters { get; set; }
}
