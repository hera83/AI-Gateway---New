using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class ChatCompletionMessageDto
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public JsonElement? Content { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; set; }

    [JsonPropertyName("tool_calls")]
    public JsonElement? ToolCalls { get; set; }

    [JsonPropertyName("refusal")]
    public string? Refusal { get; set; }
}
