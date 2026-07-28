using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class OllamaMessageDto
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<OllamaToolCallDto>? ToolCalls { get; set; }
}
