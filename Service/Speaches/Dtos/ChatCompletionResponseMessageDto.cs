using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class ChatCompletionResponseMessageDto
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("refusal")]
    public string? Refusal { get; set; }

    [JsonPropertyName("tool_calls")]
    public JsonElement? ToolCalls { get; set; }

    [JsonPropertyName("audio")]
    public JsonElement? Audio { get; set; }

    [JsonPropertyName("annotations")]
    public JsonElement? Annotations { get; set; }
}
