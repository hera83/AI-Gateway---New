using System.ComponentModel.DataAnnotations;

namespace AiGateway.Dto.KnowledgeBase;

public class SearchRequestDto
{
    [Required]
    public required string Query { get; set; }

    [Range(1, 100)]
    public int? TopK { get; set; }
}
