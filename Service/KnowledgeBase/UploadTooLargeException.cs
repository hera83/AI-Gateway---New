namespace AiGateway.Service.KnowledgeBase;

public class UploadTooLargeException(string fileName, long sizeBytes, long maxSizeBytes)
    : Exception($"File '{fileName}' ({sizeBytes} bytes) exceeds the maximum allowed upload size of {maxSizeBytes} bytes.")
{
    public string FileName { get; } = fileName;
    public long SizeBytes { get; } = sizeBytes;
    public long MaxSizeBytes { get; } = maxSizeBytes;
}
