namespace AiGateway.Dto.Speaches;

public class EmbeddingObjectDto
{
    public string Object { get; set; } = "embedding";

    public int Index { get; set; }

    public List<double> Embedding { get; set; } = [];
}
