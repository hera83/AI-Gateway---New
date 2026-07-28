namespace AiGateway.Data.Entities;

public class KnowledgeDocument
{
    public required Guid Id { get; set; }
    public required Guid ApiKeyId { get; set; }
    public required Guid KnowledgeGroupId { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }
    public string? ExtractedText { get; set; }
    public required DocumentStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public required DateTimeOffset UploadedAt { get; set; }
}
