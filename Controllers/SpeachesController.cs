using AiGateway.Dto.Errors;
using AiGateway.Service.Speaches.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiDto = AiGateway.Dto.Speaches;
using SvcDto = AiGateway.Service.Speaches.Dtos;

namespace AiGateway.Controllers;

[ApiController]
[Route("[controller]/[action]")]
[Authorize]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status502BadGateway)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
public class SpeachesController(ISpeachesService speachesService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.ChatCompletionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChatCompletions([FromBody] ApiDto.ChatCompletionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await speachesService.ChatCompletionsAsync(ToService(request), cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.TranscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Transcribe([FromForm] ApiDto.TranscribeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await speachesService.TranscribeAsync(ToService(request), cancellationToken);
        return Ok(ToApi(result));
    }

    // WebSocket upgrade, not a request/response call like the rest of this controller — the caller
    // opens wss://.../Speaches/TranscribeRealtime?model=...&language=..., streams audio in, and
    // receives Speaches' realtime transcription events back over the same connection (see
    // Service/Speaches/Docs/SpeachesService.md for the event flow). A 400 is only possible before the
    // upgrade happens; once it does, this action never returns a normal HTTP response.
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status101SwitchingProtocols)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TranscribeRealtime([FromQuery] ApiDto.RealtimeTranscribeRequestDto request, CancellationToken cancellationToken)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            return BadRequest(new ErrorResponseDto
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "WebSocket request required",
                Detail = "This endpoint only accepts WebSocket upgrade requests.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        using var clientSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await speachesService.ProxyRealtimeTranscriptionAsync(clientSocket, request.Model, request.Language, cancellationToken);
        return new EmptyResult();
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.TranslationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Translate([FromForm] ApiDto.TranslateRequestDto request, CancellationToken cancellationToken)
    {
        var result = await speachesService.TranslateAsync(ToService(request), cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiDto.ListModelsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListModels([FromQuery] ApiDto.ListModelsRequestDto request, CancellationToken cancellationToken)
    {
        var result = await speachesService.ListModelsAsync(request.Task, cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiDto.ListAudioModelsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAudioModels(CancellationToken cancellationToken)
    {
        var result = await speachesService.ListAudioModelsAsync(cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiDto.ListModelsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListVoices(CancellationToken cancellationToken)
    {
        var result = await speachesService.ListVoicesAsync(cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpGet("{modelId}")]
    [ProducesResponseType(typeof(ApiDto.ModelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModel(string modelId, CancellationToken cancellationToken)
    {
        var result = await speachesService.GetModelAsync(modelId, cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpPost("{modelId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadModel(string modelId, CancellationToken cancellationToken)
    {
        await speachesService.DownloadModelAsync(modelId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{modelId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteModel(string modelId, CancellationToken cancellationToken)
    {
        await speachesService.DeleteModelAsync(modelId, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiDto.ListModelsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRegistry([FromQuery] ApiDto.ListModelsRequestDto request, CancellationToken cancellationToken)
    {
        var result = await speachesService.GetRegistryAsync(request.Task, cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiDto.RunningModelsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRunningModels(CancellationToken cancellationToken)
    {
        var result = await speachesService.ListRunningModelsAsync(cancellationToken);
        return Ok(new ApiDto.RunningModelsResponseDto { Models = result.Models });
    }

    [HttpPost("{modelId}")]
    [ProducesResponseType(typeof(ApiDto.MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LoadModel(string modelId, CancellationToken cancellationToken)
    {
        var result = await speachesService.LoadModelAsync(modelId, cancellationToken);
        return Ok(new ApiDto.MessageResponseDto { Message = result.Message });
    }

    [HttpDelete("{modelId}")]
    [ProducesResponseType(typeof(ApiDto.MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopModel(string modelId, CancellationToken cancellationToken)
    {
        var result = await speachesService.StopModelAsync(modelId, cancellationToken);
        return Ok(new ApiDto.MessageResponseDto { Message = result.Message });
    }

    [HttpPost]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "audio/mpeg")]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SynthesizeSpeech([FromBody] ApiDto.SynthesizeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await speachesService.SynthesizeSpeechAsync(ToService(request), cancellationToken);

        // Setting a download filename adds a Content-Disposition: attachment header, which makes
        // Swagger UI (and browsers in general) offer the file as a download instead of trying —
        // unreliably, for binary audio — to render it inline with its built-in <audio> preview.
        // Callers that read the response body programmatically (fetch, HttpClient, ...) ignore
        // this header, so it doesn't change how real API consumers receive the file.
        return File(result.Content, result.ContentType, $"speech.{request.ResponseFormat}");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.SpeechEmbeddingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSpeechEmbedding([FromForm] ApiDto.SpeechEmbeddingRequestDto request, CancellationToken cancellationToken)
    {
        var result = await speachesService.CreateSpeechEmbeddingAsync(ToService(request), cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(List<ApiDto.SpeechTimestampDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DetectSpeechTimestamps([FromForm] ApiDto.SpeechTimestampsRequestDto request, CancellationToken cancellationToken)
    {
        var result = await speachesService.DetectSpeechTimestampsAsync(ToService(request), cancellationToken);
        return Ok(result.Select(ToApi).ToList());
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.DiarizationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Diarize([FromForm] ApiDto.DiarizationRequestDto request, CancellationToken cancellationToken)
    {
        var result = await speachesService.DiarizeAsync(ToService(request), cancellationToken);
        return Ok(ToApi(result));
    }

    // Chat completions

    private static SvcDto.ChatCompletionRequestDto ToService(ApiDto.ChatCompletionRequestDto request) => new()
    {
        Model = request.Model,
        Messages = request.Messages.Select(ToService).ToList(),
        Temperature = request.Temperature,
        TopP = request.TopP,
        MaxCompletionTokens = request.MaxCompletionTokens,
        N = request.N,
        Stop = request.Stop,
        PresencePenalty = request.PresencePenalty,
        FrequencyPenalty = request.FrequencyPenalty,
        Seed = request.Seed,
        User = request.User,
        Tools = request.Tools,
        ToolChoice = request.ToolChoice,
        ResponseFormat = request.ResponseFormat,
        Logprobs = request.Logprobs,
        TopLogprobs = request.TopLogprobs
    };

    private static SvcDto.ChatCompletionMessageDto ToService(ApiDto.ChatCompletionMessageDto message) => new()
    {
        Role = message.Role,
        Content = message.Content,
        Name = message.Name,
        ToolCallId = message.ToolCallId,
        ToolCalls = message.ToolCalls,
        Refusal = message.Refusal
    };

    private static ApiDto.ChatCompletionResponseDto ToApi(SvcDto.ChatCompletionResultDto result) => new()
    {
        Id = result.Id,
        Object = result.Object,
        Created = result.Created,
        Model = result.Model,
        Choices = result.Choices.Select(ToApi).ToList(),
        Usage = ToApi(result.Usage),
        SystemFingerprint = result.SystemFingerprint,
        ServiceTier = result.ServiceTier
    };

    private static ApiDto.ChatCompletionChoiceDto ToApi(SvcDto.ChatCompletionChoiceDto choice) => new()
    {
        Index = choice.Index,
        Message = ToApi(choice.Message),
        FinishReason = choice.FinishReason,
        Logprobs = choice.Logprobs
    };

    private static ApiDto.ChatCompletionResponseMessageDto ToApi(SvcDto.ChatCompletionResponseMessageDto message) => new()
    {
        Role = message.Role,
        Content = message.Content,
        Refusal = message.Refusal,
        ToolCalls = message.ToolCalls,
        Audio = message.Audio,
        Annotations = message.Annotations
    };

    private static ApiDto.ChatCompletionUsageDto? ToApi(SvcDto.ChatCompletionUsageDto? usage)
    {
        if (usage is null)
        {
            return null;
        }

        return new ApiDto.ChatCompletionUsageDto
        {
            PromptTokens = usage.PromptTokens,
            CompletionTokens = usage.CompletionTokens,
            TotalTokens = usage.TotalTokens,
            PromptTokensDetails = usage.PromptTokensDetails,
            CompletionTokensDetails = usage.CompletionTokensDetails
        };
    }

    // Transcribe / translate

    private static SvcDto.TranscriptionRequestDto ToService(ApiDto.TranscribeRequestDto request) => new()
    {
        Model = request.Model,
        File = request.File.OpenReadStream(),
        FileName = request.File.FileName,
        ContentType = request.File.ContentType,
        Language = request.Language,
        Prompt = request.Prompt,
        Temperature = request.Temperature,
        TimestampGranularities = request.TimestampGranularities,
        Hotwords = request.Hotwords,
        WithoutTimestamps = request.WithoutTimestamps
    };

    private static ApiDto.TranscriptionResponseDto ToApi(SvcDto.TranscriptionResultDto result) => new()
    {
        Text = result.Text,
        Language = result.Language,
        Duration = result.Duration,
        Segments = result.Segments?.Select(ToApi).ToList(),
        Words = result.Words?.Select(ToApi).ToList()
    };

    private static ApiDto.TranscriptionSegmentDto ToApi(SvcDto.TranscriptionSegmentDto segment) => new()
    {
        Id = segment.Id,
        Start = segment.Start,
        End = segment.End,
        Text = segment.Text,
        Tokens = segment.Tokens,
        Temperature = segment.Temperature,
        AvgLogprob = segment.AvgLogprob,
        CompressionRatio = segment.CompressionRatio,
        NoSpeechProb = segment.NoSpeechProb,
        Seek = segment.Seek
    };

    private static ApiDto.TranscriptionWordDto ToApi(SvcDto.TranscriptionWordDto word) => new()
    {
        Word = word.Word,
        Start = word.Start,
        End = word.End
    };

    private static SvcDto.TranslationRequestDto ToService(ApiDto.TranslateRequestDto request) => new()
    {
        Model = request.Model,
        File = request.File.OpenReadStream(),
        FileName = request.File.FileName,
        ContentType = request.File.ContentType,
        Prompt = request.Prompt,
        Temperature = request.Temperature
    };

    private static ApiDto.TranslationResponseDto ToApi(SvcDto.TranslationResultDto result) => new()
    {
        Text = result.Text,
        Language = result.Language,
        Duration = result.Duration,
        Segments = result.Segments?.Select(ToApi).ToList()
    };

    // Models / registry / ps

    private static ApiDto.ModelDto ToApi(SvcDto.ModelDto model) => new()
    {
        Id = model.Id,
        Created = model.Created,
        Object = model.Object,
        OwnedBy = model.OwnedBy,
        Language = model.Language,
        Task = model.Task
    };

    private static ApiDto.ListModelsResponseDto ToApi(SvcDto.ListModelsResultDto result) => new()
    {
        Data = result.Data.Select(ToApi).ToList(),
        Object = result.Object
    };

    private static ApiDto.ListAudioModelsResponseDto ToApi(SvcDto.ListAudioModelsResultDto result) => new()
    {
        Models = result.Models.Select(ToApi).ToList(),
        Object = result.Object
    };

    // Speech synthesis / embedding / timestamps / diarization

    private static SvcDto.SynthesizeRequestDto ToService(ApiDto.SynthesizeRequestDto request) => new()
    {
        Model = request.Model,
        Input = request.Input,
        Voice = request.Voice,
        ResponseFormat = request.ResponseFormat,
        Speed = request.Speed,
        SampleRate = request.SampleRate
    };

    private static SvcDto.SpeechEmbeddingRequestDto ToService(ApiDto.SpeechEmbeddingRequestDto request) => new()
    {
        Model = request.Model,
        File = request.File.OpenReadStream(),
        FileName = request.File.FileName,
        ContentType = request.File.ContentType
    };

    private static ApiDto.SpeechEmbeddingResponseDto ToApi(SvcDto.SpeechEmbeddingResultDto result) => new()
    {
        Object = result.Object,
        Data = result.Data.Select(ToApi).ToList(),
        Model = result.Model,
        Usage = new ApiDto.EmbeddingUsageDto
        {
            PromptTokens = result.Usage.PromptTokens,
            TotalTokens = result.Usage.TotalTokens
        }
    };

    private static ApiDto.EmbeddingObjectDto ToApi(SvcDto.EmbeddingObjectDto embedding) => new()
    {
        Object = embedding.Object,
        Index = embedding.Index,
        Embedding = embedding.Embedding
    };

    private static SvcDto.SpeechTimestampsRequestDto ToService(ApiDto.SpeechTimestampsRequestDto request) => new()
    {
        File = request.File.OpenReadStream(),
        FileName = request.File.FileName,
        ContentType = request.File.ContentType,
        Model = request.Model,
        Threshold = request.Threshold,
        NegThreshold = request.NegThreshold,
        MinSpeechDurationMs = request.MinSpeechDurationMs,
        MaxSpeechDurationS = request.MaxSpeechDurationS,
        MinSilenceDurationMs = request.MinSilenceDurationMs,
        SpeechPadMs = request.SpeechPadMs
    };

    private static ApiDto.SpeechTimestampDto ToApi(SvcDto.SpeechTimestampDto timestamp) => new()
    {
        Start = timestamp.Start,
        End = timestamp.End
    };

    private static SvcDto.DiarizationRequestDto ToService(ApiDto.DiarizationRequestDto request) => new()
    {
        File = request.File.OpenReadStream(),
        FileName = request.File.FileName,
        ContentType = request.File.ContentType,
        KnownSpeakerNames = request.KnownSpeakerNames,
        KnownSpeakerReferences = request.KnownSpeakerReferences
    };

    private static ApiDto.DiarizationResponseDto ToApi(SvcDto.DiarizationResultDto result) => new()
    {
        Duration = result.Duration,
        Segments = result.Segments.Select(ToApi).ToList()
    };

    private static ApiDto.DiarizationSegmentDto ToApi(SvcDto.DiarizationSegmentDto segment) => new()
    {
        Start = segment.Start,
        End = segment.End,
        Speaker = segment.Speaker
    };
}
