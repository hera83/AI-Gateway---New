namespace AiGateway.Service.KnowledgeBase;

public class KnowledgeGroupNameConflictException(string name)
    : Exception($"A knowledge group named '{name}' already exists for this API key.")
{
    public string Name { get; } = name;
}
