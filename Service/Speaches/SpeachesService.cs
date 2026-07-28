using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AiGateway.Service.Speaches.Dtos;
using AiGateway.Service.Speaches.Interfaces;

namespace AiGateway.Service.Speaches;

public class SpeachesService(HttpClient httpClient) : ISpeachesService
{
    public async Task<ChatCompletionResultDto> ChatCompletionsAsync(ChatCompletionRequestDto request, CancellationToken cancellationToken)
    {
        request.Stream = false;
        return await PostJsonAsync<ChatCompletionRequestDto, ChatCompletionResultDto>("v1/chat/completions", request, cancellationToken);
    }

    public async Task<TranscriptionResultDto> TranscribeAsync(TranscriptionRequestDto request, CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string>
        {
            ["model"] = request.Model,
            ["response_format"] = "verbose_json",
            ["stream"] = "false"
        };

        if (request.Language is not null)
        {
            fields["language"] = request.Language;
        }

        if (request.Prompt is not null)
        {
            fields["prompt"] = request.Prompt;
        }

        if (request.Temperature is not null)
        {
            fields["temperature"] = request.Temperature.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (request.Hotwords is not null)
        {
            fields["hotwords"] = request.Hotwords;
        }

        if (request.WithoutTimestamps is not null)
        {
            fields["without_timestamps"] = request.WithoutTimestamps.Value ? "true" : "false";
        }

        using var content = BuildMultipartContent(fields, request.File, request.FileName, request.ContentType);

        if (request.TimestampGranularities is not null)
        {
            foreach (var granularity in request.TimestampGranularities)
            {
                content.Add(new StringContent(granularity), "timestamp_granularities");
            }
        }

        return await PostMultipartAsync<TranscriptionResultDto>("v1/audio/transcriptions", content, cancellationToken);
    }

    public async Task<TranslationResultDto> TranslateAsync(TranslationRequestDto request, CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string>
        {
            ["model"] = request.Model,
            ["response_format"] = "verbose_json"
        };

        if (request.Prompt is not null)
        {
            fields["prompt"] = request.Prompt;
        }

        if (request.Temperature is not null)
        {
            fields["temperature"] = request.Temperature.Value.ToString(CultureInfo.InvariantCulture);
        }

        using var content = BuildMultipartContent(fields, request.File, request.FileName, request.ContentType);
        return await PostMultipartAsync<TranslationResultDto>("v1/audio/translations", content, cancellationToken);
    }

    public async Task<ListModelsResultDto> ListModelsAsync(string? task, CancellationToken cancellationToken)
    {
        var uri = task is null ? "v1/models" : $"v1/models?task={Uri.EscapeDataString(task)}";
        var result = await httpClient.GetFromJsonAsync<ListModelsResultDto>(uri, cancellationToken);
        return result ?? new ListModelsResultDto();
    }

    public async Task<ListAudioModelsResultDto> ListAudioModelsAsync(CancellationToken cancellationToken)
    {
        var result = await httpClient.GetFromJsonAsync<ListAudioModelsResultDto>("v1/audio/models", cancellationToken);
        return result ?? new ListAudioModelsResultDto();
    }

    public async Task<ListModelsResultDto> ListVoicesAsync(CancellationToken cancellationToken)
    {
        var result = await httpClient.GetFromJsonAsync<ListModelsResultDto>("v1/audio/voices", cancellationToken);
        return result ?? new ListModelsResultDto();
    }

    public async Task<ModelDto> GetModelAsync(string modelId, CancellationToken cancellationToken)
    {
        var result = await httpClient.GetFromJsonAsync<ModelDto>($"v1/models/{modelId}", cancellationToken);
        return result ?? new ModelDto();
    }

    public async Task DownloadModelAsync(string modelId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync($"v1/models/{modelId}", null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteModelAsync(string modelId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync($"v1/models/{modelId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<ListModelsResultDto> GetRegistryAsync(string? task, CancellationToken cancellationToken)
    {
        var uri = task is null ? "v1/registry" : $"v1/registry?task={Uri.EscapeDataString(task)}";
        var result = await httpClient.GetFromJsonAsync<ListModelsResultDto>(uri, cancellationToken);
        return result ?? new ListModelsResultDto();
    }

    public async Task<RunningModelsResultDto> ListRunningModelsAsync(CancellationToken cancellationToken)
    {
        var result = await httpClient.GetFromJsonAsync<RunningModelsResultDto>("api/ps", cancellationToken);
        return result ?? new RunningModelsResultDto();
    }

    public async Task<MessageResultDto> LoadModelAsync(string modelId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync($"api/ps/{modelId}", null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonOrDefaultAsync<MessageResultDto>(response, cancellationToken);
    }

    public async Task<MessageResultDto> StopModelAsync(string modelId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync($"api/ps/{modelId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonOrDefaultAsync<MessageResultDto>(response, cancellationToken);
    }

    public async Task<AudioContentDto> SynthesizeSpeechAsync(SynthesizeRequestDto request, CancellationToken cancellationToken)
    {
        request.StreamFormat = "audio";
        using var response = await httpClient.PostAsJsonAsync("v1/audio/speech", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        return new AudioContentDto { Content = bytes, ContentType = NormalizeAudioContentType(contentType) };
    }

    // Speaches returns "audio/mp3" for mp3 output, which isn't the IANA-registered media type
    // (audio/mpeg) — browsers' <audio> elements, including Swagger UI's built-in player, silently
    // refuse to play a source whose Content-Type they don't recognize.
    private static string NormalizeAudioContentType(string contentType) => contentType.Equals("audio/mp3", StringComparison.OrdinalIgnoreCase)
        ? "audio/mpeg"
        : contentType;

    public async Task<SpeechEmbeddingResultDto> CreateSpeechEmbeddingAsync(SpeechEmbeddingRequestDto request, CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string> { ["model"] = request.Model };
        using var content = BuildMultipartContent(fields, request.File, request.FileName, request.ContentType);
        return await PostMultipartAsync<SpeechEmbeddingResultDto>("v1/audio/speech/embedding", content, cancellationToken);
    }

    public async Task<List<SpeechTimestampDto>> DetectSpeechTimestampsAsync(SpeechTimestampsRequestDto request, CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string>();

        if (request.Model is not null)
        {
            fields["model"] = request.Model;
        }

        if (request.Threshold is not null)
        {
            fields["threshold"] = request.Threshold.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (request.NegThreshold is not null)
        {
            fields["neg_threshold"] = request.NegThreshold.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (request.MinSpeechDurationMs is not null)
        {
            fields["min_speech_duration_ms"] = request.MinSpeechDurationMs.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (request.MaxSpeechDurationS is not null)
        {
            fields["max_speech_duration_s"] = request.MaxSpeechDurationS.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (request.MinSilenceDurationMs is not null)
        {
            fields["min_silence_duration_ms"] = request.MinSilenceDurationMs.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (request.SpeechPadMs is not null)
        {
            fields["speech_pad_ms"] = request.SpeechPadMs.Value.ToString(CultureInfo.InvariantCulture);
        }

        using var content = BuildMultipartContent(fields, request.File, request.FileName, request.ContentType);
        using var response = await httpClient.PostAsync("v1/audio/speech/timestamps", content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<List<SpeechTimestampDto>>(cancellationToken);
        return result ?? [];
    }

    public async Task<DiarizationResultDto> DiarizeAsync(DiarizationRequestDto request, CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string> { ["response_format"] = "json" };
        using var content = BuildMultipartContent(fields, request.File, request.FileName, request.ContentType);

        if (request.KnownSpeakerNames is not null)
        {
            foreach (var name in request.KnownSpeakerNames)
            {
                content.Add(new StringContent(name), "known_speaker_names");
            }
        }

        if (request.KnownSpeakerReferences is not null)
        {
            foreach (var reference in request.KnownSpeakerReferences)
            {
                content.Add(new StringContent(reference), "known_speaker_references");
            }
        }

        return await PostMultipartAsync<DiarizationResultDto>("v1/audio/diarization", content, cancellationToken);
    }

    private static MultipartFormDataContent BuildMultipartContent(IDictionary<string, string> fields, Stream file, string fileName, string? contentType)
    {
        var content = new MultipartFormDataContent();
        foreach (var (key, value) in fields)
        {
            content.Add(new StringContent(value), key);
        }

        var fileContent = new StreamContent(file);
        if (!string.IsNullOrEmpty(contentType))
        {
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        content.Add(fileContent, "file", fileName);
        return content;
    }

    private async Task<TResult> PostJsonAsync<TRequest, TResult>(string requestUri, TRequest request, CancellationToken cancellationToken)
        where TResult : new()
    {
        using var response = await httpClient.PostAsJsonAsync(requestUri, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<TResult>(cancellationToken);
        return result ?? new TResult();
    }

    private async Task<TResult> PostMultipartAsync<TResult>(string requestUri, MultipartFormDataContent content, CancellationToken cancellationToken)
        where TResult : new()
    {
        using var response = await httpClient.PostAsync(requestUri, content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<TResult>(cancellationToken);
        return result ?? new TResult();
    }

    private static async Task<TResult> ReadJsonOrDefaultAsync<TResult>(HttpResponseMessage response, CancellationToken cancellationToken)
        where TResult : new()
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return new TResult();
        }

        return JsonSerializer.Deserialize<TResult>(body) ?? new TResult();
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Speaches request to '{response.RequestMessage?.RequestUri}' failed with status {(int)response.StatusCode} ({response.StatusCode}): {body}",
            null,
            response.StatusCode);
    }
}
