using System.Text.Json.Serialization;

namespace AiGateway.Service.Ollama.Dtos;

public class VersionResultDto
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}
