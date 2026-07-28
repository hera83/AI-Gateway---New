namespace AiGateway.Data.Entities;

public class KnowledgeChunk
{
    public required Guid Id { get; set; }
    public required Guid ApiKeyId { get; set; }
    public required Guid DocumentId { get; set; }
    public required int ChunkIndex { get; set; }
    public required string Text { get; set; }
    public required long VectorRowId { get; set; }
}
