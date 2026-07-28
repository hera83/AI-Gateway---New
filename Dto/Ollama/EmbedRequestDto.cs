namespace AiGateway.Dto.Ollama;

public class EmbedRequestDto
{
    public string Model { get; set; } = string.Empty;

    public List<string> Input { get; set; } = [];

    public OllamaOptionsDto? Options { get; set; }

    public string? KeepAlive { get; set; }
}
