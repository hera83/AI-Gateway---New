namespace AiGateway.Dto.Speaches;

public class DiarizationRequestDto
{
    public IFormFile File { get; set; } = null!;

    public List<string>? KnownSpeakerNames { get; set; }

    public List<string>? KnownSpeakerReferences { get; set; }
}
