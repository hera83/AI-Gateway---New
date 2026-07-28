namespace AiGateway.Dto.Keys;

public class AuditLogResponseDto
{
    public required List<AuditLogEntryResponseDto> Entries { get; set; }
}
