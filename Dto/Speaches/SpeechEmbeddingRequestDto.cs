namespace AiGateway.Dto.Speaches;

public class SpeechEmbeddingRequestDto
{
    public string Model { get; set; } = string.Empty;

    public IFormFile File { get; set; } = null!;
}
