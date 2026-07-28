using AiGateway.Service.Ollama.Dtos;

namespace AiGateway.Service.Ollama.Interfaces;

public interface IOllamaService
{
    Task<GenerateResultDto> GenerateAsync(GenerateRequestDto request, CancellationToken cancellationToken);

    Task<ChatResultDto> ChatAsync(ChatRequestDto request, CancellationToken cancellationToken);

    Task<EmbedResultDto> EmbedAsync(EmbedRequestDto request, CancellationToken cancellationToken);

    Task<ListModelsResultDto> ListModelsAsync(CancellationToken cancellationToken);

    Task<ListRunningModelsResultDto> ListRunningModelsAsync(CancellationToken cancellationToken);

    Task<ShowModelResultDto> ShowModelAsync(ShowModelRequestDto request, CancellationToken cancellationToken);

    Task CopyModelAsync(CopyModelRequestDto request, CancellationToken cancellationToken);

    Task DeleteModelAsync(DeleteModelRequestDto request, CancellationToken cancellationToken);

    Task<PullModelResultDto> PullModelAsync(PullModelRequestDto request, CancellationToken cancellationToken);

    Task<PushModelResultDto> PushModelAsync(PushModelRequestDto request, CancellationToken cancellationToken);

    Task<CreateModelResultDto> CreateModelAsync(CreateModelRequestDto request, CancellationToken cancellationToken);

    Task<VersionResultDto> GetVersionAsync(CancellationToken cancellationToken);
}
