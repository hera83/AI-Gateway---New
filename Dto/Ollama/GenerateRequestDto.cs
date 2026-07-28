using System.Text.Json;

namespace AiGateway.Dto.Ollama;

public class GenerateRequestDto
{
    public string Model { get; set; } = string.Empty;

    public string? Prompt { get; set; }

    public string? Suffix { get; set; }

    public List<string>? Images { get; set; }

    public JsonElement? Format { get; set; }

    public OllamaOptionsDto? Options { get; set; }

    public string? System { get; set; }

    public string? Template { get; set; }

    public List<long>? Context { get; set; }

    public bool? Raw { get; set; }

    public string? KeepAlive { get; set; }
}
