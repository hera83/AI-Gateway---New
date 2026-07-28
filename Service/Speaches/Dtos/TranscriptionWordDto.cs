using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class TranscriptionWordDto
{
    [JsonPropertyName("word")]
    public string Word { get; set; } = string.Empty;

    [JsonPropertyName("start")]
    public double Start { get; set; }

    [JsonPropertyName("end")]
    public double End { get; set; }
}
