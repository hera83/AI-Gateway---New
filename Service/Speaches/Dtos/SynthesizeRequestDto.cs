using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class SynthesizeRequestDto
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    [JsonPropertyName("voice")]
    public string Voice { get; set; } = string.Empty;

    [JsonPropertyName("response_format")]
    public string ResponseFormat { get; set; } = "mp3";

    [JsonPropertyName("speed")]
    public double? Speed { get; set; }

    [JsonPropertyName("sample_rate")]
    public int? SampleRate { get; set; }

    [JsonPropertyName("stream_format")]
    public string StreamFormat { get; set; } = "audio";
}
