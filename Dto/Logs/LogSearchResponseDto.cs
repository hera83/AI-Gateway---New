namespace AiGateway.Dto.Logs;

public class LogSearchResponseDto
{
    public required int TotalCount { get; set; }

    public required int Page { get; set; }

    public required int PageSize { get; set; }

    public required List<LogEntryResponseDto> Entries { get; set; }
}
