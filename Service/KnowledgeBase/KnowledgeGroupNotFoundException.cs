namespace AiGateway.Service.KnowledgeBase;

public class KnowledgeGroupNotFoundException(Guid id) : Exception($"Knowledge group '{id}' was not found.")
{
    public Guid Id { get; } = id;
}
