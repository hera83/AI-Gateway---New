namespace AiGateway.Dto.Keys;

public class RolloverKeyResponseDto
{
    public required Guid Id { get; set; }

    // Vises kun i dette svar — kan ikke hentes frem igen, kun rulles igen.
    public required string ApiKey { get; set; }

    public required DateTimeOffset LastRotatedAt { get; set; }
}
