using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class TranslationResultDto
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("duration")]
    public double? Duration { get; set; }

    [JsonPropertyName("segments")]
    public List<TranscriptionSegmentDto>? Segments { get; set; }
}
