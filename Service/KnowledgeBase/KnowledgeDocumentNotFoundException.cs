namespace AiGateway.Service.KnowledgeBase;

public class KnowledgeDocumentNotFoundException(Guid id) : Exception($"Knowledge document '{id}' was not found.")
{
    public Guid Id { get; } = id;
}
