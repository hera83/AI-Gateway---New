using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class SpeechTimestampDto
{
    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("end")]
    public int End { get; set; }
}
