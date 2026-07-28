namespace AiGateway.Dto.Ollama;

public class CopyModelRequestDto
{
    public string Source { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;
}
