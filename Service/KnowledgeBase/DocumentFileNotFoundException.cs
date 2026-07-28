namespace AiGateway.Service.KnowledgeBase;

public class DocumentFileNotFoundException(Guid id) : Exception($"The stored file for knowledge document '{id}' was not found.")
{
    public Guid Id { get; } = id;
}
