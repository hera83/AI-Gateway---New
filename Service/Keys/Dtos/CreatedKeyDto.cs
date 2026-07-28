namespace AiGateway.Service.Keys.Dtos;

// ApiKey is the plaintext secret — only ever populated right after generation, never persisted or re-derivable.
public class CreatedKeyDto : KeyDto
{
    public required string ApiKey { get; set; }
}
