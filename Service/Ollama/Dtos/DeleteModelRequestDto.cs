using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class DeleteModelRequestDto
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
}
