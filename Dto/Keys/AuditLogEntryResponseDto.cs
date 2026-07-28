namespace AiGateway.Dto.Keys;

public class AuditLogEntryResponseDto
{
    public required string Action { get; set; }

    public required DateTimeOffset Timestamp { get; set; }

    public string? Details { get; set; }
}
