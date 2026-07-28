using System.Text.Json;

namespace AiGateway.Dto.Ollama;

public class ChatRequestDto
{
    public string Model { get; set; } = string.Empty;

    public List<OllamaMessageDto> Messages { get; set; } = [];

    public List<OllamaToolDto>? Tools { get; set; }

    public JsonElement? Format { get; set; }

    public OllamaOptionsDto? Options { get; set; }

    public string? KeepAlive { get; set; }
}
