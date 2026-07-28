using System.Text.Json;

namespace AiGateway.Dto.Speaches;

public class ChatCompletionChoiceDto
{
    public int Index { get; set; }

    public ChatCompletionResponseMessageDto Message { get; set; } = new();

    public string? FinishReason { get; set; }

    public JsonElement? Logprobs { get; set; }
}
