using AiGateway.Service.TextExtraction.Interfaces;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace AiGateway.Service.TextExtraction;

public class TextExtractionService : ITextExtractionService
{
    public async Task<string> ExtractTextAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".txt" or ".md" => await ExtractPlainTextAsync(fileStream, cancellationToken),
            ".pdf" => ExtractPdfText(fileStream),
            ".docx" => ExtractDocxText(fileStream),
            _ => throw new UnsupportedFileTypeException(fileName)
        };
    }

    private static async Task<string> ExtractPlainTextAsync(Stream fileStream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(fileStream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static string ExtractPdfText(Stream fileStream)
    {
        using var document = PdfDocument.Open(fileStream);
        return string.Join(Environment.NewLine, document.GetPages().Select(page => page.Text));
    }

    private static string ExtractDocxText(Stream fileStream)
    {
        using var document = WordprocessingDocument.Open(fileStream, false);
        var body = document.MainDocumentPart?.Document.Body;
        if (body is null)
        {
            return string.Empty;
        }

        return string.Join(Environment.NewLine, body.Elements<Paragraph>().Select(paragraph => paragraph.InnerText));
    }
}
