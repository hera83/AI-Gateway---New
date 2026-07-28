namespace AiGateway.Service.TextExtraction.Interfaces;

public interface ITextExtractionService
{
    Task<string> ExtractTextAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken);
}
