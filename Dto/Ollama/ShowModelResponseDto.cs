using System.Text.Json;

namespace AiGateway.Dto.Ollama;

public class ShowModelResponseDto
{
    public string? License { get; set; }

    public string? Modelfile { get; set; }

    public string? Parameters { get; set; }

    public string? Template { get; set; }

    public OllamaModelDetailsDto? Details { get; set; }

    public Dictionary<string, JsonElement>? ModelInfo { get; set; }

    public List<string>? Capabilities { get; set; }
}
