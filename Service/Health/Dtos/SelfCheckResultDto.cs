namespace AiGateway.Service.Health.Dtos;

public class SelfCheckResultDto
{
    public bool IsHealthy { get; set; }

    public string Description { get; set; } = string.Empty;

    public long AllocatedBytes { get; set; }
}
