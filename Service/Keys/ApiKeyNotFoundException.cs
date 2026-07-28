namespace AiGateway.Service.Keys;

public class ApiKeyNotFoundException(Guid id) : Exception($"API key '{id}' was not found.")
{
    public Guid Id { get; } = id;
}
