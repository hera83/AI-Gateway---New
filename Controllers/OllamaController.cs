using System.Text.Json;
using AiGateway.Dto.Errors;
using AiGateway.Service.Ollama.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiDto = AiGateway.Dto.Ollama;
using SvcDto = AiGateway.Service.Ollama.Dtos;

namespace AiGateway.Controllers;

[ApiController]
[Route("[controller]/[action]")]
[Authorize]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status502BadGateway)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
public class OllamaController(IOllamaService ollamaService) : ControllerBase
{
    // When request.Stream is true, the response body is newline-delimited JSON (one
    // GenerateResponseDto per line, matching Ollama's own streaming format) instead of the single
    // object ProducesResponseType documents for the non-streaming (default) case.
    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.GenerateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Generate([FromBody] ApiDto.GenerateRequestDto request, CancellationToken cancellationToken)
    {
        if (!request.Stream)
        {
            var result = await ollamaService.GenerateAsync(ToService(request), cancellationToken);
            return Ok(ToApi(result));
        }

        Response.ContentType = "application/x-ndjson";
        await foreach (var chunk in ollamaService.GenerateStreamAsync(ToService(request), cancellationToken))
        {
            await Response.WriteAsync(JsonSerializer.Serialize(ToApi(chunk)), cancellationToken);
            await Response.WriteAsync("\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        return new EmptyResult();
    }

    // See the streaming note on Generate above — same newline-delimited-JSON behavior applies here.
    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Chat([FromBody] ApiDto.ChatRequestDto request, CancellationToken cancellationToken)
    {
        if (!request.Stream)
        {
            var result = await ollamaService.ChatAsync(ToService(request), cancellationToken);
            return Ok(ToApi(result));
        }

        Response.ContentType = "application/x-ndjson";
        await foreach (var chunk in ollamaService.ChatStreamAsync(ToService(request), cancellationToken))
        {
            await Response.WriteAsync(JsonSerializer.Serialize(ToApi(chunk)), cancellationToken);
            await Response.WriteAsync("\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        return new EmptyResult();
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.EmbedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Embed([FromBody] ApiDto.EmbedRequestDto request, CancellationToken cancellationToken)
    {
        var result = await ollamaService.EmbedAsync(ToService(request), cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiDto.ListModelsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListModels(CancellationToken cancellationToken)
    {
        var result = await ollamaService.ListModelsAsync(cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiDto.ListRunningModelsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRunningModels(CancellationToken cancellationToken)
    {
        var result = await ollamaService.ListRunningModelsAsync(cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.ShowModelResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ShowModel([FromBody] ApiDto.ShowModelRequestDto request, CancellationToken cancellationToken)
    {
        var result = await ollamaService.ShowModelAsync(ToService(request), cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CopyModel([FromBody] ApiDto.CopyModelRequestDto request, CancellationToken cancellationToken)
    {
        await ollamaService.CopyModelAsync(ToService(request), cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteModel([FromBody] ApiDto.DeleteModelRequestDto request, CancellationToken cancellationToken)
    {
        await ollamaService.DeleteModelAsync(ToService(request), cancellationToken);
        return NoContent();
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.PullModelResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PullModel([FromBody] ApiDto.PullModelRequestDto request, CancellationToken cancellationToken)
    {
        var result = await ollamaService.PullModelAsync(ToService(request), cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.PushModelResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PushModel([FromBody] ApiDto.PushModelRequestDto request, CancellationToken cancellationToken)
    {
        var result = await ollamaService.PushModelAsync(ToService(request), cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.CreateModelResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateModel([FromBody] ApiDto.CreateModelRequestDto request, CancellationToken cancellationToken)
    {
        var result = await ollamaService.CreateModelAsync(ToService(request), cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiDto.VersionResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersion(CancellationToken cancellationToken)
    {
        var result = await ollamaService.GetVersionAsync(cancellationToken);
        return Ok(new ApiDto.VersionResponseDto { Version = result.Version });
    }

    // Shared sub-object mapping

    private static SvcDto.OllamaOptionsDto? ToService(ApiDto.OllamaOptionsDto? options)
    {
        if (options is null)
        {
            return null;
        }

        return new SvcDto.OllamaOptionsDto
        {
            NumKeep = options.NumKeep,
            Seed = options.Seed,
            NumPredict = options.NumPredict,
            TopK = options.TopK,
            TopP = options.TopP,
            MinP = options.MinP,
            TypicalP = options.TypicalP,
            RepeatLastN = options.RepeatLastN,
            Temperature = options.Temperature,
            RepeatPenalty = options.RepeatPenalty,
            PresencePenalty = options.PresencePenalty,
            FrequencyPenalty = options.FrequencyPenalty,
            Mirostat = options.Mirostat,
            MirostatTau = options.MirostatTau,
            MirostatEta = options.MirostatEta,
            PenalizeNewline = options.PenalizeNewline,
            Stop = options.Stop,
            Numa = options.Numa,
            NumCtx = options.NumCtx,
            NumBatch = options.NumBatch,
            NumGpu = options.NumGpu,
            MainGpu = options.MainGpu,
            LowVram = options.LowVram,
            VocabOnly = options.VocabOnly,
            UseMmap = options.UseMmap,
            UseMlock = options.UseMlock,
            NumThread = options.NumThread
        };
    }

    private static SvcDto.OllamaMessageDto ToService(ApiDto.OllamaMessageDto message) => new()
    {
        Role = message.Role,
        Content = message.Content,
        Images = message.Images,
        ToolCalls = message.ToolCalls?.Select(ToService).ToList()
    };

    private static SvcDto.OllamaToolCallDto ToService(ApiDto.OllamaToolCallDto toolCall) => new()
    {
        Function = new SvcDto.OllamaToolCallFunctionDto
        {
            Name = toolCall.Function.Name,
            Arguments = toolCall.Function.Arguments
        }
    };

    private static SvcDto.OllamaToolDto ToService(ApiDto.OllamaToolDto tool) => new()
    {
        Type = tool.Type,
        Function = new SvcDto.OllamaToolFunctionDto
        {
            Name = tool.Function.Name,
            Description = tool.Function.Description,
            Parameters = tool.Function.Parameters
        }
    };

    private static ApiDto.OllamaMessageDto ToApi(SvcDto.OllamaMessageDto message) => new()
    {
        Role = message.Role,
        Content = message.Content,
        Images = message.Images,
        ToolCalls = message.ToolCalls?.Select(ToApi).ToList()
    };

    private static ApiDto.OllamaToolCallDto ToApi(SvcDto.OllamaToolCallDto toolCall) => new()
    {
        Function = new ApiDto.OllamaToolCallFunctionDto
        {
            Name = toolCall.Function.Name,
            Arguments = toolCall.Function.Arguments
        }
    };

    private static ApiDto.OllamaModelDetailsDto? ToApi(SvcDto.OllamaModelDetailsDto? details)
    {
        if (details is null)
        {
            return null;
        }

        return new ApiDto.OllamaModelDetailsDto
        {
            ParentModel = details.ParentModel,
            Format = details.Format,
            Family = details.Family,
            Families = details.Families,
            ParameterSize = details.ParameterSize,
            QuantizationLevel = details.QuantizationLevel
        };
    }

    private static ApiDto.OllamaModelSummaryDto ToApi(SvcDto.OllamaModelSummaryDto model) => new()
    {
        Name = model.Name,
        Model = model.Model,
        ModifiedAt = model.ModifiedAt,
        Size = model.Size,
        Digest = model.Digest,
        Details = ToApi(model.Details)
    };

    private static ApiDto.OllamaRunningModelDto ToApi(SvcDto.OllamaRunningModelDto model) => new()
    {
        Name = model.Name,
        Model = model.Model,
        Size = model.Size,
        Digest = model.Digest,
        Details = ToApi(model.Details),
        ExpiresAt = model.ExpiresAt,
        SizeVram = model.SizeVram
    };

    // Generate

    private static SvcDto.GenerateRequestDto ToService(ApiDto.GenerateRequestDto request) => new()
    {
        Model = request.Model,
        Prompt = request.Prompt,
        Suffix = request.Suffix,
        Images = request.Images,
        Format = request.Format,
        Options = ToService(request.Options),
        System = request.System,
        Template = request.Template,
        Context = request.Context,
        Raw = request.Raw,
        KeepAlive = request.KeepAlive
    };

    private static ApiDto.GenerateResponseDto ToApi(SvcDto.GenerateResultDto result) => new()
    {
        Model = result.Model,
        CreatedAt = result.CreatedAt,
        Response = result.Response,
        Done = result.Done,
        DoneReason = result.DoneReason,
        Context = result.Context,
        TotalDuration = result.TotalDuration,
        LoadDuration = result.LoadDuration,
        PromptEvalCount = result.PromptEvalCount,
        PromptEvalDuration = result.PromptEvalDuration,
        EvalCount = result.EvalCount,
        EvalDuration = result.EvalDuration
    };

    // Chat

    private static SvcDto.ChatRequestDto ToService(ApiDto.ChatRequestDto request) => new()
    {
        Model = request.Model,
        Messages = request.Messages.Select(ToService).ToList(),
        Tools = request.Tools?.Select(ToService).ToList(),
        Format = request.Format,
        Options = ToService(request.Options),
        KeepAlive = request.KeepAlive
    };

    private static ApiDto.ChatResponseDto ToApi(SvcDto.ChatResultDto result) => new()
    {
        Model = result.Model,
        CreatedAt = result.CreatedAt,
        Message = ToApi(result.Message),
        Done = result.Done,
        DoneReason = result.DoneReason,
        TotalDuration = result.TotalDuration,
        LoadDuration = result.LoadDuration,
        PromptEvalCount = result.PromptEvalCount,
        PromptEvalDuration = result.PromptEvalDuration,
        EvalCount = result.EvalCount,
        EvalDuration = result.EvalDuration
    };

    // Embed

    private static SvcDto.EmbedRequestDto ToService(ApiDto.EmbedRequestDto request) => new()
    {
        Model = request.Model,
        Input = request.Input,
        Options = ToService(request.Options),
        KeepAlive = request.KeepAlive
    };

    private static ApiDto.EmbedResponseDto ToApi(SvcDto.EmbedResultDto result) => new()
    {
        Model = result.Model,
        Embeddings = result.Embeddings,
        TotalDuration = result.TotalDuration,
        LoadDuration = result.LoadDuration,
        PromptEvalCount = result.PromptEvalCount
    };

    // Tags / ps

    private static ApiDto.ListModelsResponseDto ToApi(SvcDto.ListModelsResultDto result) => new()
    {
        Models = result.Models.Select(ToApi).ToList()
    };

    private static ApiDto.ListRunningModelsResponseDto ToApi(SvcDto.ListRunningModelsResultDto result) => new()
    {
        Models = result.Models.Select(ToApi).ToList()
    };

    // Show

    private static SvcDto.ShowModelRequestDto ToService(ApiDto.ShowModelRequestDto request) => new()
    {
        Model = request.Model
    };

    private static ApiDto.ShowModelResponseDto ToApi(SvcDto.ShowModelResultDto result) => new()
    {
        License = result.License,
        Modelfile = result.Modelfile,
        Parameters = result.Parameters,
        Template = result.Template,
        Details = ToApi(result.Details),
        ModelInfo = result.ModelInfo,
        Capabilities = result.Capabilities
    };

    // Copy / delete

    private static SvcDto.CopyModelRequestDto ToService(ApiDto.CopyModelRequestDto request) => new()
    {
        Source = request.Source,
        Destination = request.Destination
    };

    private static SvcDto.DeleteModelRequestDto ToService(ApiDto.DeleteModelRequestDto request) => new()
    {
        Model = request.Model
    };

    // Pull / push

    private static SvcDto.PullModelRequestDto ToService(ApiDto.PullModelRequestDto request) => new()
    {
        Model = request.Model,
        Insecure = request.Insecure
    };

    private static ApiDto.PullModelResponseDto ToApi(SvcDto.PullModelResultDto result) => new()
    {
        Status = result.Status,
        Error = result.Error
    };

    private static SvcDto.PushModelRequestDto ToService(ApiDto.PushModelRequestDto request) => new()
    {
        Model = request.Model,
        Insecure = request.Insecure
    };

    private static ApiDto.PushModelResponseDto ToApi(SvcDto.PushModelResultDto result) => new()
    {
        Status = result.Status,
        Error = result.Error
    };

    // Create

    private static SvcDto.CreateModelRequestDto ToService(ApiDto.CreateModelRequestDto request) => new()
    {
        Model = request.Model,
        From = request.From,
        Files = request.Files,
        Adapters = request.Adapters,
        Template = request.Template,
        License = request.License,
        System = request.System,
        Parameters = request.Parameters,
        Messages = request.Messages?.Select(ToService).ToList(),
        Quantize = request.Quantize
    };

    private static ApiDto.CreateModelResponseDto ToApi(SvcDto.CreateModelResultDto result) => new()
    {
        Status = result.Status,
        Error = result.Error
    };
}
