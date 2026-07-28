using System.Text.Json;

namespace AiGateway.Dto.Speaches;

public class ChatCompletionMessageDto
{
    public string Role { get; set; } = string.Empty;

    public JsonElement? Content { get; set; }

    public string? Name { get; set; }

    public string? ToolCallId { get; set; }

    public JsonElement? ToolCalls { get; set; }

    public string? Refusal { get; set; }
}
