using AiGateway.Authentication;
using AiGateway.Dto.Errors;
using AiGateway.Dto.Logs;
using AiGateway.Service.Logs.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SvcDto = AiGateway.Service.Logs.Dtos;

namespace AiGateway.Controllers;

[ApiController]
[Route("[controller]/[action]")]
[Authorize(Roles = ApiKeyAuthenticationHandler.AdministratorRole)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
public class LogsController(ILogService logService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(LogSearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] LogSearchRequestDto request, CancellationToken cancellationToken)
    {
        var result = await logService.SearchAsync(ToService(request), cancellationToken);

        return Ok(new LogSearchResponseDto
        {
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            Entries = result.Entries.Select(ToApi).ToList()
        });
    }

    private static SvcDto.LogSearchDto ToService(LogSearchRequestDto request) => new()
    {
        Level = request.Level,
        Search = request.Search,
        From = request.From,
        To = request.To,
        Page = request.Page,
        PageSize = request.PageSize
    };

    private static LogEntryResponseDto ToApi(SvcDto.LogEntryDto entry) => new()
    {
        Id = entry.Id,
        Timestamp = entry.Timestamp,
        Level = entry.Level,
        Message = entry.Message,
        Exception = entry.Exception,
        Properties = entry.Properties
    };
}
