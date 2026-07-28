using AiGateway.Service.Health.Dtos;

namespace AiGateway.Service.Health.Interfaces;

public interface IHealthService
{
    SelfCheckResultDto CheckSelf();
}
