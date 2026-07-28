namespace AiGateway.Service.Logs.Dtos;

public class LogSearchDto
{
    public string? Level { get; set; }

    public string? Search { get; set; }

    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }

    public required int Page { get; set; }

    public required int PageSize { get; set; }
}
