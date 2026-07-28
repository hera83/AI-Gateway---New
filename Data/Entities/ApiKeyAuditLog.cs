namespace AiGateway.Data.Entities;

public class ApiKeyAuditLog
{
    public required Guid Id { get; set; }

    public required Guid ApiKeyId { get; set; }

    public required ApiKeyAuditAction Action { get; set; }

    public required DateTimeOffset Timestamp { get; set; }

    public string? Details { get; set; }
}
