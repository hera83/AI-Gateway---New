using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class ChatCompletionChoiceDto
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public ChatCompletionResponseMessageDto Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("logprobs")]
    public JsonElement? Logprobs { get; set; }
}
