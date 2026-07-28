using AiGateway.Service.Speaches.Dtos;

namespace AiGateway.Service.Speaches.Interfaces;

public interface ISpeachesService
{
    Task<ChatCompletionResultDto> ChatCompletionsAsync(ChatCompletionRequestDto request, CancellationToken cancellationToken);

    Task<TranscriptionResultDto> TranscribeAsync(TranscriptionRequestDto request, CancellationToken cancellationToken);

    Task<TranslationResultDto> TranslateAsync(TranslationRequestDto request, CancellationToken cancellationToken);

    Task<ListModelsResultDto> ListModelsAsync(string? task, CancellationToken cancellationToken);

    Task<ListAudioModelsResultDto> ListAudioModelsAsync(CancellationToken cancellationToken);

    Task<ListModelsResultDto> ListVoicesAsync(CancellationToken cancellationToken);

    Task<ModelDto> GetModelAsync(string modelId, CancellationToken cancellationToken);

    Task DownloadModelAsync(string modelId, CancellationToken cancellationToken);

    Task DeleteModelAsync(string modelId, CancellationToken cancellationToken);

    Task<ListModelsResultDto> GetRegistryAsync(string? task, CancellationToken cancellationToken);

    Task<RunningModelsResultDto> ListRunningModelsAsync(CancellationToken cancellationToken);

    Task<MessageResultDto> LoadModelAsync(string modelId, CancellationToken cancellationToken);

    Task<MessageResultDto> StopModelAsync(string modelId, CancellationToken cancellationToken);

    Task<AudioContentDto> SynthesizeSpeechAsync(SynthesizeRequestDto request, CancellationToken cancellationToken);

    Task<SpeechEmbeddingResultDto> CreateSpeechEmbeddingAsync(SpeechEmbeddingRequestDto request, CancellationToken cancellationToken);

    Task<List<SpeechTimestampDto>> DetectSpeechTimestampsAsync(SpeechTimestampsRequestDto request, CancellationToken cancellationToken);

    Task<DiarizationResultDto> DiarizeAsync(DiarizationRequestDto request, CancellationToken cancellationToken);
}
