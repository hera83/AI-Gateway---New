using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class ShowModelRequestDto
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
}
