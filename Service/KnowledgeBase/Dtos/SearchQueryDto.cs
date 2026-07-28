namespace AiGateway.Service.KnowledgeBase.Dtos;

public class SearchQueryDto
{
    public required string Query { get; set; }
    public int? TopK { get; set; }
}
