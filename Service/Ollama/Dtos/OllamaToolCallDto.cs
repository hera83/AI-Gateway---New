using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class OllamaToolCallDto
{
    [JsonPropertyName("function")]
    public OllamaToolCallFunctionDto Function { get; set; } = new();
}

public class OllamaToolCallFunctionDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public Dictionary<string, JsonElement> Arguments { get; set; } = [];
}
