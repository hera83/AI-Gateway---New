namespace AiGateway.Dto.Speaches;

public class DiarizationResponseDto
{
    public double Duration { get; set; }

    public List<DiarizationSegmentDto> Segments { get; set; } = [];
}
