using System.ComponentModel.DataAnnotations;

namespace AiGateway.Dto.Keys;

public class UpdateKeyRequestDto
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(200)]
    public required string ResponsibleName { get; set; }

    [Required]
    [MaxLength(300)]
    public required string ContactInfo { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }
}
