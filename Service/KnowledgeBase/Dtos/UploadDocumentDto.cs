namespace AiGateway.Service.KnowledgeBase.Dtos;

public class UploadDocumentDto
{
    public required Guid GroupId { get; set; }
    public required Stream FileStream { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }
}
