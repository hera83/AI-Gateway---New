using System.Security.Claims;
using AiGateway.Dto.Errors;
using AiGateway.Service.KnowledgeBase;
using AiGateway.Service.KnowledgeBase.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiDto = AiGateway.Dto.KnowledgeBase;
using SvcDto = AiGateway.Service.KnowledgeBase.Dtos;

namespace AiGateway.Controllers;

[ApiController]
[Route("[controller]/[action]")]
[Authorize]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
public class KnowledgeBaseController(IKnowledgeBaseService knowledgeBaseService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.GroupResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateGroup([FromBody] ApiDto.CreateGroupRequestDto request, CancellationToken cancellationToken)
    {
        var result = await knowledgeBaseService.CreateGroupAsync(GetCallerApiKeyId(), ToService(request), cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiDto.ListGroupsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListGroups(CancellationToken cancellationToken)
    {
        var result = await knowledgeBaseService.ListGroupsAsync(GetCallerApiKeyId(), cancellationToken);
        return Ok(new ApiDto.ListGroupsResponseDto { Groups = result.Select(ToApi).ToList() });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken cancellationToken)
    {
        await knowledgeBaseService.DeleteGroupAsync(GetCallerApiKeyId(), id, cancellationToken);
        return NoContent();
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.DocumentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> UploadDocument([FromForm] ApiDto.UploadDocumentRequestDto request, CancellationToken cancellationToken)
    {
        var result = await knowledgeBaseService.UploadDocumentAsync(GetCallerApiKeyId(), ToService(request), cancellationToken);
        return Ok(ToApi(result));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiDto.ListDocumentsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListDocuments([FromQuery] Guid? groupId, CancellationToken cancellationToken)
    {
        var result = await knowledgeBaseService.ListDocumentsAsync(GetCallerApiKeyId(), groupId, cancellationToken);
        return Ok(new ApiDto.ListDocumentsResponseDto { Documents = result.Select(ToApi).ToList() });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken)
    {
        await knowledgeBaseService.DeleteDocumentAsync(GetCallerApiKeyId(), id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PhysicalFileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(Guid id, CancellationToken cancellationToken)
    {
        var result = await knowledgeBaseService.DownloadDocumentAsync(GetCallerApiKeyId(), id, cancellationToken);
        return PhysicalFile(result.FilePath, result.ContentType, result.FileName);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.SearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromBody] ApiDto.SearchRequestDto request, CancellationToken cancellationToken)
    {
        var result = await knowledgeBaseService.SearchAsync(GetCallerApiKeyId(), ToService(request), cancellationToken);
        return Ok(new ApiDto.SearchResponseDto { Matches = result.Select(ToApi).ToList() });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiDto.RagChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RagChat([FromBody] ApiDto.RagChatRequestDto request, CancellationToken cancellationToken)
    {
        var result = await knowledgeBaseService.RagChatAsync(GetCallerApiKeyId(), ToService(request), cancellationToken);
        return Ok(ToApi(result));
    }

    // The master key authenticates with subject "master" (see ApiKeyAuthenticationHandler), which has no
    // corresponding ApiKey row — it can't own Knowledge Base data, so every action resolves the caller's
    // own key id through here rather than trusting a route/body parameter.
    private Guid GetCallerApiKeyId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(subject, out var apiKeyId) ? apiKeyId : throw new MasterKeyNotSupportedException();
    }

    private static SvcDto.CreateGroupDto ToService(ApiDto.CreateGroupRequestDto request) => new()
    {
        Name = request.Name
    };

    private static ApiDto.GroupResponseDto ToApi(SvcDto.GroupDto group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        CreatedAt = group.CreatedAt
    };

    private static SvcDto.UploadDocumentDto ToService(ApiDto.UploadDocumentRequestDto request) => new()
    {
        GroupId = request.GroupId,
        FileStream = request.File.OpenReadStream(),
        FileName = request.File.FileName,
        ContentType = request.File.ContentType,
        SizeBytes = request.File.Length
    };

    private static ApiDto.DocumentResponseDto ToApi(SvcDto.DocumentDto document) => new()
    {
        Id = document.Id,
        GroupId = document.GroupId,
        FileName = document.FileName,
        ContentType = document.ContentType,
        SizeBytes = document.SizeBytes,
        Status = document.Status,
        ErrorMessage = document.ErrorMessage,
        UploadedAt = document.UploadedAt
    };

    private static SvcDto.SearchQueryDto ToService(ApiDto.SearchRequestDto request) => new()
    {
        Query = request.Query,
        TopK = request.TopK
    };

    private static ApiDto.ChunkMatchResponseDto ToApi(SvcDto.ChunkMatchDto match) => new()
    {
        ChunkId = match.ChunkId,
        DocumentId = match.DocumentId,
        DocumentFileName = match.DocumentFileName,
        GroupId = match.GroupId,
        Text = match.Text,
        Score = match.Score
    };

    private static SvcDto.RagChatQueryDto ToService(ApiDto.RagChatRequestDto request) => new()
    {
        Model = request.Model,
        Messages = request.Messages.Select(ToService).ToList(),
        TopK = request.TopK
    };

    private static SvcDto.ChatMessageDto ToService(ApiDto.ChatMessageDto message) => new()
    {
        Role = message.Role,
        Content = message.Content
    };

    private static ApiDto.ChatMessageDto ToApi(SvcDto.ChatMessageDto message) => new()
    {
        Role = message.Role,
        Content = message.Content
    };

    private static ApiDto.RagChatResponseDto ToApi(SvcDto.RagChatResultDto result) => new()
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
        EvalDuration = result.EvalDuration,
        Sources = result.Sources.Select(ToApi).ToList()
    };
}
