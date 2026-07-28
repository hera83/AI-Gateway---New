namespace AiGateway.Service.Keys.Dtos;

public class CreateKeyDto
{
    public required string Name { get; set; }

    public required string ResponsibleName { get; set; }

    public required string ContactInfo { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }
}
