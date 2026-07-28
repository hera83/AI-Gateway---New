using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class DiarizationSegmentDto
{
    [JsonPropertyName("start")]
    public double Start { get; set; }

    [JsonPropertyName("end")]
    public double End { get; set; }

    [JsonPropertyName("speaker")]
    public string Speaker { get; set; } = string.Empty;
}
