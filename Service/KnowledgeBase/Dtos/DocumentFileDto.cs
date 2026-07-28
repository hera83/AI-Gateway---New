namespace AiGateway.Service.KnowledgeBase.Dtos;

public class DocumentFileDto
{
    public required string FilePath { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
}
