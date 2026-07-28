namespace AiGateway.Service.KnowledgeBase.Dtos;

public class GroupDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
