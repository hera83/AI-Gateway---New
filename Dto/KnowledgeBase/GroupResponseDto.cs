namespace AiGateway.Dto.KnowledgeBase;

public class GroupResponseDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
