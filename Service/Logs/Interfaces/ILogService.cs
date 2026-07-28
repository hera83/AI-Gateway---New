using AiGateway.Service.Logs.Dtos;

namespace AiGateway.Service.Logs.Interfaces;

public interface ILogService
{
    Task<LogSearchResultDto> SearchAsync(LogSearchDto query, CancellationToken cancellationToken);
}
