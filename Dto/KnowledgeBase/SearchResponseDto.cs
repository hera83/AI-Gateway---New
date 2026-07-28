namespace AiGateway.Dto.KnowledgeBase;

public class SearchResponseDto
{
    public required List<ChunkMatchResponseDto> Matches { get; set; }
}
