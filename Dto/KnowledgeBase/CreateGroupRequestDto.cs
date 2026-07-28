using System.ComponentModel.DataAnnotations;

namespace AiGateway.Dto.KnowledgeBase;

public class CreateGroupRequestDto
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }
}
