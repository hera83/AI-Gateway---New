namespace AiGateway.Dto.Ollama;

public class OllamaModelDetailsDto
{
    public string? ParentModel { get; set; }

    public string? Format { get; set; }

    public string? Family { get; set; }

    public List<string>? Families { get; set; }

    public string? ParameterSize { get; set; }

    public string? QuantizationLevel { get; set; }
}
