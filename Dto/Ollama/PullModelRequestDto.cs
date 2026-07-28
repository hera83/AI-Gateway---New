namespace AiGateway.Dto.Ollama;

public class PullModelRequestDto
{
    public string Model { get; set; } = string.Empty;

    public bool? Insecure { get; set; }
}
