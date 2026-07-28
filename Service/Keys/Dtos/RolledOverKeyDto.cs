namespace AiGateway.Service.Keys.Dtos;

// ApiKey is the new plaintext secret — only ever populated right after rollover, never persisted or re-derivable.
public class RolledOverKeyDto
{
    public required Guid Id { get; set; }

    public required string ApiKey { get; set; }

    public required DateTimeOffset LastRotatedAt { get; set; }
}
