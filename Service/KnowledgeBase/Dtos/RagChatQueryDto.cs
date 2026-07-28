namespace AiGateway.Service.KnowledgeBase.Dtos;

public class RagChatQueryDto
{
    public required string Model { get; set; }
    public required List<ChatMessageDto> Messages { get; set; }
    public int? TopK { get; set; }
}
