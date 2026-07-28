using AiGateway.Service.KnowledgeBase.Dtos;

namespace AiGateway.Service.KnowledgeBase.Interfaces;

public interface IKnowledgeBaseService
{
    Task<GroupDto> CreateGroupAsync(Guid apiKeyId, CreateGroupDto request, CancellationToken cancellationToken);

    Task<List<GroupDto>> ListGroupsAsync(Guid apiKeyId, CancellationToken cancellationToken);

    Task DeleteGroupAsync(Guid apiKeyId, Guid groupId, CancellationToken cancellationToken);

    Task<DocumentDto> UploadDocumentAsync(Guid apiKeyId, UploadDocumentDto request, CancellationToken cancellationToken);

    Task<List<DocumentDto>> ListDocumentsAsync(Guid apiKeyId, Guid? groupId, CancellationToken cancellationToken);

    Task DeleteDocumentAsync(Guid apiKeyId, Guid documentId, CancellationToken cancellationToken);

    Task<DocumentFileDto> DownloadDocumentAsync(Guid apiKeyId, Guid documentId, CancellationToken cancellationToken);

    Task<List<ChunkMatchDto>> SearchAsync(Guid apiKeyId, SearchQueryDto request, CancellationToken cancellationToken);

    Task<RagChatResultDto> RagChatAsync(Guid apiKeyId, RagChatQueryDto request, CancellationToken cancellationToken);
}
