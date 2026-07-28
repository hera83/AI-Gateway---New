using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class ListRunningModelsResultDto
{
    [JsonPropertyName("models")]
    public List<OllamaRunningModelDto> Models { get; set; } = [];
}
