namespace AiGateway.Dto.Speaches;

public class RealtimeTranscribeRequestDto
{
    public string Model { get; set; } = string.Empty;

    public string? Language { get; set; }
}
