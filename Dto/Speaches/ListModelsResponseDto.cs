namespace AiGateway.Dto.Speaches;

public class ListModelsResponseDto
{
    public List<ModelDto> Data { get; set; } = [];

    public string Object { get; set; } = "list";
}
