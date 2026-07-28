using System.ComponentModel.DataAnnotations;

namespace AiGateway.Dto.KnowledgeBase;

public class ChatMessageDto
{
    [Required]
    public required string Role { get; set; }

    [Required]
    public required string Content { get; set; }
}
