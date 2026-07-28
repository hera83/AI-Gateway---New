namespace AiGateway.Service.Logs.Dtos;

public class LogEntryDto
{
    public required long Id { get; set; }

    public required DateTimeOffset Timestamp { get; set; }

    public required string Level { get; set; }

    public string? Message { get; set; }

    public string? Exception { get; set; }

    public string? Properties { get; set; }
}
