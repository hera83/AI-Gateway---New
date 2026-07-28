using System.ComponentModel.DataAnnotations;

namespace AiGateway.Dto.KnowledgeBase;

public class RagChatRequestDto
{
    [Required]
    public required string Model { get; set; }

    // Full conversation, oldest first, ending with the latest user turn — same contract as
    // Ollama/Chat's Messages, so a caller already speaking that shape can reuse it here. Retrieval
    // uses only the latest 'user' message; a fresh system message with the newly retrieved context
    // replaces any system message sent here, so one can be included but it is always ignored.
    [Required]
    [MinLength(1)]
    public required List<ChatMessageDto> Messages { get; set; }

    [Range(1, 100)]
    public int? TopK { get; set; }
}
