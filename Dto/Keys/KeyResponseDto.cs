namespace AiGateway.Dto.Keys;

public class KeyResponseDto
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string ResponsibleName { get; set; }

    public required string ContactInfo { get; set; }

    public required bool IsActive { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastRotatedAt { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }
}
