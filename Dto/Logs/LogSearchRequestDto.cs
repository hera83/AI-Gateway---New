using System.ComponentModel.DataAnnotations;

namespace AiGateway.Dto.Logs;

public class LogSearchRequestDto
{
    public string? Level { get; set; }

    public string? Search { get; set; }

    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 500)]
    public int PageSize { get; set; } = 50;
}
