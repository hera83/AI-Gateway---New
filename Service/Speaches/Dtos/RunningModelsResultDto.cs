using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class RunningModelsResultDto
{
    [JsonPropertyName("models")]
    public List<string> Models { get; set; } = [];
}
