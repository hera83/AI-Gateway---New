namespace AiGateway.Dto.Health;

public class HealthResponseDto
{
    public string Status { get; set; } = string.Empty;

    public IEnumerable<HealthCheckEntryDto> Checks { get; set; } = [];
}
