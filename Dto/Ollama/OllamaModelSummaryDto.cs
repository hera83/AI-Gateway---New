namespace AiGateway.Dto.Ollama;

public class OllamaModelSummaryDto
{
    public string Name { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public DateTimeOffset? ModifiedAt { get; set; }

    public long Size { get; set; }

    public string? Digest { get; set; }

    public OllamaModelDetailsDto? Details { get; set; }
}
