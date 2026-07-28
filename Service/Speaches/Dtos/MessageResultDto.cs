using System.Text.Json.Serialization;

namespace AiGateway.Service.Speaches.Dtos;

public class MessageResultDto
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
