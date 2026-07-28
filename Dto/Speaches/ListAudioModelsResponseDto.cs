namespace AiGateway.Dto.Speaches;

public class ListAudioModelsResponseDto
{
    public List<ModelDto> Models { get; set; } = [];

    public string Object { get; set; } = "list";
}
