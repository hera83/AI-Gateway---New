namespace AiGateway.Dto.Ollama;

public class PushModelRequestDto
{
    public string Model { get; set; } = string.Empty;

    public bool? Insecure { get; set; }
}
