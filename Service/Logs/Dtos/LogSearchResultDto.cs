namespace AiGateway.Service.Logs.Dtos;

public class LogSearchResultDto
{
    public required int TotalCount { get; set; }

    public required int Page { get; set; }

    public required int PageSize { get; set; }

    public required IReadOnlyList<LogEntryDto> Entries { get; set; }
}
