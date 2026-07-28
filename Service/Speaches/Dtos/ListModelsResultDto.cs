using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class ListModelsResultDto
{
    [JsonPropertyName("data")]
    public List<ModelDto> Data { get; set; } = [];

    [JsonPropertyName("object")]
    public string Object { get; set; } = "list";
}
