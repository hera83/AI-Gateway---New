namespace AiGateway.Service.Speaches.Dtos;

public class DiarizationRequestDto
{
    public Stream File { get; set; } = Stream.Null;

    public string FileName { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public List<string>? KnownSpeakerNames { get; set; }

    public List<string>? KnownSpeakerReferences { get; set; }
}
