namespace AiGateway.Dto.Keys;

public class CreateKeyResponseDto : KeyResponseDto
{
    // Vises kun i dette svar — kan ikke hentes frem igen, kun rulles via POST /keys/{id}/rollover.
    public required string ApiKey { get; set; }
}
