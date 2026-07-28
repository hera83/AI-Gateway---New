namespace AiGateway.Data.Entities;

public class KnowledgeGroup
{
    public required Guid Id { get; set; }
    public required Guid ApiKeyId { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
