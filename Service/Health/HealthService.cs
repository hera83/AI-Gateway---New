using AiGateway.Service.Health.Dtos;
using AiGateway.Service.Health.Interfaces;

namespace AiGateway.Service.Health;

public class HealthService : IHealthService
{
    private const long MaxAllocatedBytes = 1024L * 1024 * 1024;

    public SelfCheckResultDto CheckSelf()
    {
        var allocatedBytes = GC.GetTotalMemory(forceFullCollection: false);
        var isHealthy = allocatedBytes < MaxAllocatedBytes;

        var description = isHealthy
            ? "Application is responding and within memory thresholds."
            : $"Allocated memory ({allocatedBytes} bytes) exceeds threshold ({MaxAllocatedBytes} bytes).";

        return new SelfCheckResultDto
        {
            IsHealthy = isHealthy,
            Description = description,
            AllocatedBytes = allocatedBytes
        };
    }
}
