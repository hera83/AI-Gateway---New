namespace AiGateway.Dto.Speaches;

public class SynthesizeRequestDto
{
    public string Model { get; set; } = string.Empty;

    public string Input { get; set; } = string.Empty;

    public string Voice { get; set; } = string.Empty;

    public string ResponseFormat { get; set; } = "mp3";

    public double? Speed { get; set; }

    public int? SampleRate { get; set; }
}
