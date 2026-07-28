using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class EmbeddingObjectDto
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = "embedding";

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("embedding")]
    public List<double> Embedding { get; set; } = [];
}
