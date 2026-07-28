namespace AiGateway.Dto.Logs;

public class LogEntryResponseDto
{
    public required long Id { get; set; }

    public required DateTimeOffset Timestamp { get; set; }

    public required string Level { get; set; }

    public string? Message { get; set; }

    public string? Exception { get; set; }

    public string? Properties { get; set; }
}
