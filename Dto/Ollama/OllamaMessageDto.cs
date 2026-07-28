namespace AiGateway.Dto.Ollama;

public class OllamaMessageDto
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public List<string>? Images { get; set; }

    public List<OllamaToolCallDto>? ToolCalls { get; set; }
}
