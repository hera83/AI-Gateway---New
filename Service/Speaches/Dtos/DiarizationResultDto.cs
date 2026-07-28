using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class DiarizationResultDto
{
    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    [JsonPropertyName("segments")]
    public List<DiarizationSegmentDto> Segments { get; set; } = [];
}
