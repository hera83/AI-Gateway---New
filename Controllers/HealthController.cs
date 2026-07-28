using AiGateway.Dto.Errors;
using AiGateway.Dto.Health;
using AiGateway.Service.Health.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AiGateway.Controllers;

[ApiController]
[Route("[controller]/[action]")]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
public class HealthController(IHealthService healthService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponseDto), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult Check()
    {
        var result = healthService.CheckSelf();

        var response = new HealthResponseDto
        {
            Status = result.IsHealthy ? "Healthy" : "Unhealthy",
            Checks =
            [
                new HealthCheckEntryDto
                {
                    Name = "self",
                    Status = result.IsHealthy ? "Healthy" : "Unhealthy",
                    Description = result.Description
                }
            ]
        };

        return result.IsHealthy
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
