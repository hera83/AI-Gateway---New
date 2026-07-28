using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class ListAudioModelsResultDto
{
    [JsonPropertyName("models")]
    public List<ModelDto> Models { get; set; } = [];

    [JsonPropertyName("object")]
    public string Object { get; set; } = "list";
}
