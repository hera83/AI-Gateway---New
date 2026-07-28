namespace AiGateway.Dto.KnowledgeBase;

public class ChunkMatchResponseDto
{
    public required Guid ChunkId { get; set; }
    public required Guid DocumentId { get; set; }
    public required string DocumentFileName { get; set; }
    public required Guid GroupId { get; set; }
    public required string Text { get; set; }
    public required double Score { get; set; }
}
