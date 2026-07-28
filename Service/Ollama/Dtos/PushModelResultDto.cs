using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class PushModelResultDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
