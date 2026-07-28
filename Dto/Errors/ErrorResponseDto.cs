namespace AiGateway.Dto.Errors;

public class ErrorResponseDto
{
    public required int Status { get; set; }

    public required string Title { get; set; }

    public string? Detail { get; set; }

    public required string TraceId { get; set; }

    public IDictionary<string, string[]>? Errors { get; set; }
}
