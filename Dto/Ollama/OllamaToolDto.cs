using System.Text.Json;

namespace AiGateway.Dto.Ollama;

public class OllamaToolDto
{
    public string Type { get; set; } = "function";

    public OllamaToolFunctionDto Function { get; set; } = new();
}

public class OllamaToolFunctionDto
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public JsonElement Parameters { get; set; }
}
