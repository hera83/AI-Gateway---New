namespace AiGateway.Dto.KnowledgeBase;

public class RagChatResponseDto
{
    public required string Model { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public required ChatMessageDto Message { get; set; }
    public bool Done { get; set; }
    public string? DoneReason { get; set; }
    public long? TotalDuration { get; set; }
    public long? LoadDuration { get; set; }
    public int? PromptEvalCount { get; set; }
    public long? PromptEvalDuration { get; set; }
    public int? EvalCount { get; set; }
    public long? EvalDuration { get; set; }
    public required List<ChunkMatchResponseDto> Sources { get; set; }
}
