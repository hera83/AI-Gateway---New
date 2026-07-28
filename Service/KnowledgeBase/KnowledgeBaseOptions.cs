namespace AiGateway.Service.KnowledgeBase;

public class KnowledgeBaseOptions
{
    public required string EmbeddingModel { get; set; }
    public required int EmbeddingDimensions { get; set; }
    public required int ChunkSizeCharacters { get; set; }
    public required int ChunkOverlapCharacters { get; set; }
    public required long MaxUploadSizeBytes { get; set; }
}
