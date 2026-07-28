using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class SpeechEmbeddingResultDto
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = "list";

    [JsonPropertyName("data")]
    public List<EmbeddingObjectDto> Data { get; set; } = [];

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("usage")]
    public EmbeddingUsageDto Usage { get; set; } = new();
}
