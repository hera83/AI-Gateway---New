namespace AiGateway.Dto.KnowledgeBase;

public class DocumentResponseDto
{
    public required Guid Id { get; set; }
    public required Guid GroupId { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }
    public required string Status { get; set; }
    public string? ErrorMessage { get; set; }
    public required DateTimeOffset UploadedAt { get; set; }
}
