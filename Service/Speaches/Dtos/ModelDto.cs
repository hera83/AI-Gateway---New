using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class ModelDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("object")]
    public string Object { get; set; } = "model";

    [JsonPropertyName("owned_by")]
    public string OwnedBy { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public List<string>? Language { get; set; }

    [JsonPropertyName("task")]
    public string Task { get; set; } = string.Empty;
}
