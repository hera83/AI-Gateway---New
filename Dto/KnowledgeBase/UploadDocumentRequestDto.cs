using System.ComponentModel.DataAnnotations;

namespace AiGateway.Dto.KnowledgeBase;

public class UploadDocumentRequestDto
{
    [Required]
    public required Guid GroupId { get; set; }

    [Required]
    public required IFormFile File { get; set; }
}
