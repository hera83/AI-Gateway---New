using System.Text.Json;

namespace AiGateway.Dto.Speaches;

public class ChatCompletionUsageDto
{
    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }

    public JsonElement? PromptTokensDetails { get; set; }

    public JsonElement? CompletionTokensDetails { get; set; }
}
