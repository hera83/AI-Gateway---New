using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class ListModelsResultDto
{
    [JsonPropertyName("models")]
    public List<OllamaModelSummaryDto> Models { get; set; } = [];
}
