namespace AiGateway.Service.Keys.Dtos;

public class AuditLogEntryDto
{
    public required string Action { get; set; }

    public required DateTimeOffset Timestamp { get; set; }

    public string? Details { get; set; }
}
