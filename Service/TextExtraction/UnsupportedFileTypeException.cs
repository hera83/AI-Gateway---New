namespace AiGateway.Service.TextExtraction;

public class UnsupportedFileTypeException(string fileName)
    : Exception($"File '{fileName}' has an unsupported file type. Supported types: .txt, .md, .pdf, .docx.")
{
    public string FileName { get; } = fileName;
}
