using System.Text.Json;

namespace AiGateway.Dto.Ollama;

public class OllamaToolCallDto
{
    public OllamaToolCallFunctionDto Function { get; set; } = new();
}

public class OllamaToolCallFunctionDto
{
    public string Name { get; set; } = string.Empty;

    public Dictionary<string, JsonElement> Arguments { get; set; } = [];
}
