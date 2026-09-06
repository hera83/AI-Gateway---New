using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AiGateway.Service.Speaches.Dtos;
using AiGateway.Service.Speaches.Interfaces;

namespace AiGateway.Service.Speaches;

public class SpeachesService(HttpClient httpClient, ILogger<SpeachesService> logger) : ISpeachesService
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

    public async Task ProxyRealtimeTranscriptionAsync(WebSocket clientSocket, string model, string? language, CancellationToken cancellationToken)
    {
        // httpClient.BaseAddress is http(s):// (see Program.cs) — Speaches' realtime endpoint is
        // the same host/port, just upgraded to a WebSocket, so ws(s):// is the only thing that changes.
        var upstreamUriBuilder = new UriBuilder(httpClient.BaseAddress!)
        {
            Scheme = httpClient.BaseAddress!.Scheme == Uri.UriSchemeHttps ? "wss" : "ws",
            Path = "/v1/realtime",
            Query = language is null
                ? $"intent=transcription&model={Uri.EscapeDataString(model)}"
                : $"intent=transcription&model={Uri.EscapeDataString(model)}&language={Uri.EscapeDataString(language)}"
        };

        using var upstreamSocket = new ClientWebSocket();
        await upstreamSocket.ConnectAsync(upstreamUriBuilder.Uri, cancellationToken);

        // Each direction is driven only by the caller's own cancellation token (request abort) —
        // never by the other direction failing. An earlier version of this method cancelled a shared
        // "stop the other side too" token when one direction faulted, but WebSocket.ReceiveAsync
        // aborts its *entire* socket as a side effect of being cancelled (that's just how .NET's
        // WebSocket implementation works) — so cancelling clientSocket's still-pending receive from
        // here tore the client connection down instantly, before TryNotifyClientOfFailureAsync below
        // ever got a chance to run. Don't reintroduce that.
        var clientToUpstream = RelayAsync(clientSocket, upstreamSocket, cancellationToken);
        var upstreamToClient = RelayAsync(upstreamSocket, clientSocket, cancellationToken);

        var first = await Task.WhenAny(clientToUpstream, upstreamToClient);
        if (first.IsFaulted)
        {
            var ex = first.Exception!.GetBaseException();
            logger.LogError(ex, "Speaches realtime transcription relay failed for model '{Model}'.", model);
            await TryNotifyClientOfFailureAsync(clientSocket, ex);
        }

        // Give the other direction's task a chance to finish naturally (e.g. the client acking the
        // close frame TryNotifyClientOfFailureAsync just sent) without letting it block this method
        // forever if the peer never responds — its own exception, if any, is only observed here so
        // it doesn't surface later as an unobserved task exception; the failure that mattered (if
        // any) was already handled above.
        var second = first == clientToUpstream ? upstreamToClient : clientToUpstream;
        try
        {
            await second.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
        catch
        {
            // Timeout or fault on the trailing direction — nothing further to do with it here.
        }
    }

    // Best-effort: tells the client *why* the connection is ending instead of just dropping it, using
    // an "error" event shaped like OpenAI's Realtime API (which this protocol mirrors) so existing
    // Realtime-protocol clients can handle it without special-casing this gateway. Uses
    // CloseOutputAsync rather than CloseAsync, and CancellationToken.None throughout, deliberately:
    // the other relay direction may still have a receive pending on this same clientSocket, and
    // CloseOutputAsync sends the close frame without waiting for (or conflicting with) that, while
    // the request's own cancellation token may have already tripped by the time we get here.
    private static async Task TryNotifyClientOfFailureAsync(WebSocket clientSocket, Exception ex)
    {
        if (clientSocket.State != WebSocketState.Open)
        {
            return;
        }

        try
        {
            var errorEvent = JsonSerializer.Serialize(new
            {
                type = "error",
                error = new
                {
                    type = "server_error",
                    message = $"The realtime transcription connection to Speaches failed: {ex.Message}"
                }
            });
            await clientSocket.SendAsync(Encoding.UTF8.GetBytes(errorEvent), WebSocketMessageType.Text, true, CancellationToken.None);
            await clientSocket.CloseOutputAsync(WebSocketCloseStatus.InternalServerError, "Upstream relay failed.", CancellationToken.None);
        }
        catch
        {
            // The client socket may already be unusable — this is best-effort only.
        }
    }

    // Speaches' realtime protocol (mirroring OpenAI's Realtime API) is a sequence of JSON text-frame
    // events — audio is base64 inside e.g. "input_audio_buffer.append", not a raw binary frame — so
    // this gateway never needs to parse a single event: it only has to shuttle whole messages
    // verbatim in both directions until one side closes.
    private static async Task RelayAsync(WebSocket source, WebSocket destination, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        while (source.State == WebSocketState.Open)
        {
            using var messageStream = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await source.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (destination.State == WebSocketState.Open)
                    {
                        await destination.CloseOutputAsync(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, cancellationToken);
                    }

                    return;
                }

                messageStream.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            if (destination.State == WebSocketState.Open)
            {
                await destination.SendAsync(messageStream.ToArray(), result.MessageType, true, cancellationToken);
            }
        }
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
        return await PostMultipartAsync<List<SpeechTimestampDto>>("v1/audio/speech/timestamps", content, cancellationToken);
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
        logger.LogInformation("Sending request to Speaches at '{RequestUri}'.", requestUri);
        var stopwatch = Stopwatch.StartNew();
        using var response = await httpClient.PostAsJsonAsync(requestUri, request, cancellationToken);
        logger.LogInformation(
            "Received response from Speaches for '{RequestUri}' after {ElapsedMilliseconds} ms (status {StatusCode}).",
            requestUri, stopwatch.ElapsedMilliseconds, (int)response.StatusCode);
        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<TResult>(cancellationToken);
        return result ?? new TResult();
    }

    // Long-running (uploads can take minutes for large audio files, see CLAUDE.md's 75-minute
    // HttpClient.Timeout) — logged with elapsed time on both ends so a stuck-looking transcription
    // can be told apart from "still uploading/decoding, GPU just hasn't started yet" without
    // guessing, since there is otherwise no progress signal between the request starting and ending.
    private async Task<TResult> PostMultipartAsync<TResult>(string requestUri, MultipartFormDataContent content, CancellationToken cancellationToken)
        where TResult : new()
    {
        logger.LogInformation(
            "Sending multipart request to Speaches at '{RequestUri}' ({ContentLength} bytes).",
            requestUri, content.Headers.ContentLength?.ToString(CultureInfo.InvariantCulture) ?? "unknown");
        var stopwatch = Stopwatch.StartNew();
        using var response = await httpClient.PostAsync(requestUri, content, cancellationToken);
        logger.LogInformation(
            "Received response from Speaches for '{RequestUri}' after {ElapsedMilliseconds} ms (status {StatusCode}).",
            requestUri, stopwatch.ElapsedMilliseconds, (int)response.StatusCode);
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
