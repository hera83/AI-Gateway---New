using System.Data;
using System.Text.Json;
using AiGateway.Data;
using AiGateway.Data.Entities;
using AiGateway.Service.KnowledgeBase.Dtos;
using AiGateway.Service.KnowledgeBase.Interfaces;
using AiGateway.Service.Ollama.Dtos;
using AiGateway.Service.Ollama.Interfaces;
using AiGateway.Service.TextExtraction;
using AiGateway.Service.TextExtraction.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiGateway.Service.KnowledgeBase;

public class KnowledgeBaseService(
    AppDbContext dbContext,
    IOllamaService ollamaService,
    ITextExtractionService textExtractionService,
    IOptions<KnowledgeBaseOptions> options,
    string filesRootPath,
    ILogger<KnowledgeBaseService> logger) : IKnowledgeBaseService
{
    public async Task<GroupDto> CreateGroupAsync(Guid apiKeyId, CreateGroupDto request, CancellationToken cancellationToken)
    {
        var nameExists = await dbContext.KnowledgeGroups
            .AnyAsync(group => group.ApiKeyId == apiKeyId && group.Name == request.Name, cancellationToken);
        if (nameExists)
        {
            throw new KnowledgeGroupNameConflictException(request.Name);
        }

        var group = new KnowledgeGroup
        {
            Id = Guid.NewGuid(),
            ApiKeyId = apiKeyId,
            Name = request.Name,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.KnowledgeGroups.Add(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToGroupDto(group);
    }

    public async Task<List<GroupDto>> ListGroupsAsync(Guid apiKeyId, CancellationToken cancellationToken)
    {
        var groups = await dbContext.KnowledgeGroups
            .Where(group => group.ApiKeyId == apiKeyId)
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);

        return groups.Select(ToGroupDto).ToList();
    }

    public async Task DeleteGroupAsync(Guid apiKeyId, Guid groupId, CancellationToken cancellationToken)
    {
        var group = await dbContext.KnowledgeGroups
            .FirstOrDefaultAsync(group => group.ApiKeyId == apiKeyId && group.Id == groupId, cancellationToken)
            ?? throw new KnowledgeGroupNotFoundException(groupId);

        var vectorRowIds = await (
            from chunk in dbContext.KnowledgeChunks
            join document in dbContext.KnowledgeDocuments on chunk.DocumentId equals document.Id
            where document.KnowledgeGroupId == groupId
            select chunk.VectorRowId
        ).ToListAsync(cancellationToken);

        dbContext.KnowledgeGroups.Remove(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        await DeleteVectorRowsAsync(vectorRowIds, cancellationToken);
        DeleteGroupFiles(groupId);
    }

    public async Task<DocumentDto> UploadDocumentAsync(Guid apiKeyId, UploadDocumentDto request, CancellationToken cancellationToken)
    {
        var group = await dbContext.KnowledgeGroups
            .FirstOrDefaultAsync(group => group.ApiKeyId == apiKeyId && group.Id == request.GroupId, cancellationToken)
            ?? throw new KnowledgeGroupNotFoundException(request.GroupId);

        if (request.SizeBytes > options.Value.MaxUploadSizeBytes)
        {
            throw new UploadTooLargeException(request.FileName, request.SizeBytes, options.Value.MaxUploadSizeBytes);
        }

        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            ApiKeyId = apiKeyId,
            KnowledgeGroupId = group.Id,
            FileName = request.FileName,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            Status = DocumentStatus.Processing,
            UploadedAt = DateTimeOffset.UtcNow
        };
        dbContext.KnowledgeDocuments.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Saved to disk (as-is, before any extraction) so the original upload stays downloadable via
        // DownloadDocumentAsync even if extraction below fails. request.FileStream is consumed by this
        // copy, so extraction reads back from the saved file rather than the now-exhausted stream.
        var filePath = GetDocumentFilePath(group.Id, document.Id);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await using (var fileOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await request.FileStream.CopyToAsync(fileOutput, cancellationToken);
        }

        string text;
        try
        {
            await using var fileInput = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            text = await textExtractionService.ExtractTextAsync(fileInput, request.FileName, request.ContentType, cancellationToken);
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("No extractable text was found in the file.");
            }
        }
        catch (Exception exception) when (exception is not UnsupportedFileTypeException)
        {
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = exception.Message;
            await dbContext.SaveChangesAsync(cancellationToken);
            return ToDocumentDto(document);
        }

        var chunks = ChunkText(text);
        var embedResult = await ollamaService.EmbedAsync(
            new EmbedRequestDto { Model = options.Value.EmbeddingModel, Input = chunks },
            cancellationToken);

        var chunkEntities = new List<KnowledgeChunk>(chunks.Count);
        for (var index = 0; index < chunks.Count; index++)
        {
            var vectorRowId = await InsertEmbeddingAsync(embedResult.Embeddings[index], apiKeyId, group.Id, cancellationToken);
            chunkEntities.Add(new KnowledgeChunk
            {
                Id = Guid.NewGuid(),
                ApiKeyId = apiKeyId,
                DocumentId = document.Id,
                ChunkIndex = index,
                Text = chunks[index],
                VectorRowId = vectorRowId
            });
        }

        document.ExtractedText = text;
        document.Status = DocumentStatus.Indexed;
        dbContext.KnowledgeChunks.AddRange(chunkEntities);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDocumentDto(document);
    }

    public async Task<List<DocumentDto>> ListDocumentsAsync(Guid apiKeyId, Guid? groupId, CancellationToken cancellationToken)
    {
        var query = dbContext.KnowledgeDocuments.Where(document => document.ApiKeyId == apiKeyId);
        if (groupId.HasValue)
        {
            query = query.Where(document => document.KnowledgeGroupId == groupId.Value);
        }

        // Ordered client-side after materializing — SQLite's EF Core provider can't translate ORDER BY
        // over a DateTimeOffset column (same constraint KeyService.GetAuditLogAsync works around).
        var documents = await query.ToListAsync(cancellationToken);
        return documents.OrderByDescending(document => document.UploadedAt).Select(ToDocumentDto).ToList();
    }

    public async Task DeleteDocumentAsync(Guid apiKeyId, Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.KnowledgeDocuments
            .FirstOrDefaultAsync(document => document.ApiKeyId == apiKeyId && document.Id == documentId, cancellationToken)
            ?? throw new KnowledgeDocumentNotFoundException(documentId);

        var vectorRowIds = await dbContext.KnowledgeChunks
            .Where(chunk => chunk.DocumentId == documentId)
            .Select(chunk => chunk.VectorRowId)
            .ToListAsync(cancellationToken);

        dbContext.KnowledgeDocuments.Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        await DeleteVectorRowsAsync(vectorRowIds, cancellationToken);
        DeleteDocumentFile(document.KnowledgeGroupId, document.Id);
    }

    public async Task<DocumentFileDto> DownloadDocumentAsync(Guid apiKeyId, Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.KnowledgeDocuments
            .FirstOrDefaultAsync(document => document.ApiKeyId == apiKeyId && document.Id == documentId, cancellationToken)
            ?? throw new KnowledgeDocumentNotFoundException(documentId);

        var filePath = GetDocumentFilePath(document.KnowledgeGroupId, document.Id);
        if (!File.Exists(filePath))
        {
            throw new DocumentFileNotFoundException(documentId);
        }

        return new DocumentFileDto
        {
            FilePath = filePath,
            FileName = document.FileName,
            ContentType = document.ContentType
        };
    }

    public async Task<List<ChunkMatchDto>> SearchAsync(Guid apiKeyId, SearchQueryDto request, CancellationToken cancellationToken)
    {
        var topK = request.TopK is > 0 ? request.TopK.Value : 5;
        var embedResult = await ollamaService.EmbedAsync(
            new EmbedRequestDto { Model = options.Value.EmbeddingModel, Input = [request.Query] },
            cancellationToken);

        var connection = await GetOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        // Always scoped to the caller's own api_key_id (a vec0 partition key, see
        // FixVectorMetadataColumns), never to a single group — search/RagChat deliberately span every
        // group/document the caller owns. GroupId per match in the result (see ChunkMatchDto below)
        // still tells the caller which group each hit came from.
        command.CommandText = "SELECT rowid, distance FROM vectors.vec_chunks WHERE embedding MATCH $embedding AND k = $k AND api_key_id = $apiKeyId ORDER BY distance;";
        command.Parameters.AddWithValue("$embedding", SerializeEmbedding(embedResult.Embeddings[0]));
        command.Parameters.AddWithValue("$k", topK);
        command.Parameters.AddWithValue("$apiKeyId", apiKeyId.ToString());

        var matches = new List<(long VectorRowId, double Distance)>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                matches.Add((reader.GetInt64(0), reader.GetDouble(1)));
            }
        }

        if (matches.Count == 0)
        {
            return [];
        }

        var vectorRowIds = matches.Select(match => match.VectorRowId).ToList();
        var chunkRows = await (
            from chunk in dbContext.KnowledgeChunks
            join document in dbContext.KnowledgeDocuments on chunk.DocumentId equals document.Id
            where vectorRowIds.Contains(chunk.VectorRowId)
            select new { chunk.VectorRowId, chunk.Id, chunk.DocumentId, chunk.Text, document.FileName, document.KnowledgeGroupId }
        ).ToDictionaryAsync(row => row.VectorRowId, cancellationToken);

        return matches
            .Where(match => chunkRows.ContainsKey(match.VectorRowId))
            .Select(match =>
            {
                var row = chunkRows[match.VectorRowId];
                return new ChunkMatchDto
                {
                    ChunkId = row.Id,
                    DocumentId = row.DocumentId,
                    DocumentFileName = row.FileName,
                    GroupId = row.KnowledgeGroupId,
                    Text = row.Text,
                    // Table uses distance_metric=cosine, so distance is (1 - cosine similarity); higher score = more relevant.
                    Score = 1 - match.Distance
                };
            })
            .ToList();
    }

    public async Task<RagChatResultDto> RagChatAsync(Guid apiKeyId, RagChatQueryDto request, CancellationToken cancellationToken)
    {
        var lastUserMessage = request.Messages.LastOrDefault(message => string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase))
            ?? throw new NoUserMessageException();

        var matches = await SearchAsync(apiKeyId, new SearchQueryDto
        {
            Query = lastUserMessage.Content,
            TopK = request.TopK
        }, cancellationToken);

        var systemPrompt = matches.Count > 0
            ? "Answer the user's question using only the following context. Cite sources by their [n] number." +
              Environment.NewLine + Environment.NewLine +
              string.Join(Environment.NewLine + Environment.NewLine, matches.Select((match, index) => $"[{index + 1}] {match.Text}"))
            : "No relevant context was found in the knowledge base. Answer based on general knowledge and say so.";

        // Retrieval is always based on the latest user message, not the full history, since Sources
        // can differ turn to turn and stale context from an earlier turn shouldn't leak into a later
        // answer. The fresh system message built above therefore replaces any system message the
        // caller sent in Messages; every other message (oldest first, ending with the latest user
        // turn) is forwarded as-is, mirroring Ollama/Chat's Messages contract.
        var messages = new List<OllamaMessageDto> { new() { Role = "system", Content = systemPrompt } };
        messages.AddRange(request.Messages
            .Where(message => !string.Equals(message.Role, "system", StringComparison.OrdinalIgnoreCase))
            .Select(message => new OllamaMessageDto { Role = message.Role, Content = message.Content }));

        var chatResult = await ollamaService.ChatAsync(new ChatRequestDto
        {
            Model = request.Model,
            Messages = messages
        }, cancellationToken);

        return new RagChatResultDto
        {
            Model = chatResult.Model,
            CreatedAt = chatResult.CreatedAt,
            Message = new ChatMessageDto { Role = chatResult.Message.Role, Content = chatResult.Message.Content },
            Done = chatResult.Done,
            DoneReason = chatResult.DoneReason,
            TotalDuration = chatResult.TotalDuration,
            LoadDuration = chatResult.LoadDuration,
            PromptEvalCount = chatResult.PromptEvalCount,
            PromptEvalDuration = chatResult.PromptEvalDuration,
            EvalCount = chatResult.EvalCount,
            EvalDuration = chatResult.EvalDuration,
            Sources = matches
        };
    }

    private List<string> ChunkText(string text)
    {
        var chunkSize = options.Value.ChunkSizeCharacters;
        var step = Math.Max(1, chunkSize - options.Value.ChunkOverlapCharacters);
        var chunks = new List<string>();

        var start = 0;
        while (start < text.Length)
        {
            var length = Math.Min(chunkSize, text.Length - start);
            var chunk = text.Substring(start, length).Trim();
            if (chunk.Length > 0)
            {
                chunks.Add(chunk);
            }

            if (start + length >= text.Length)
            {
                break;
            }

            start += step;
        }

        return chunks;
    }

    private async Task<long> InsertEmbeddingAsync(List<double> embedding, Guid apiKeyId, Guid groupId, CancellationToken cancellationToken)
    {
        var connection = await GetOpenConnectionAsync(cancellationToken);

        await using (var insertCommand = connection.CreateCommand())
        {
            insertCommand.CommandText = "INSERT INTO vectors.vec_chunks(embedding, api_key_id, group_id) VALUES ($embedding, $apiKeyId, $groupId);";
            insertCommand.Parameters.AddWithValue("$embedding", SerializeEmbedding(embedding));
            insertCommand.Parameters.AddWithValue("$apiKeyId", apiKeyId.ToString());
            insertCommand.Parameters.AddWithValue("$groupId", groupId.ToString());
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var rowIdCommand = connection.CreateCommand();
        rowIdCommand.CommandText = "SELECT last_insert_rowid();";
        return (long)(await rowIdCommand.ExecuteScalarAsync(cancellationToken))!;
    }

    private async Task DeleteVectorRowsAsync(List<long> vectorRowIds, CancellationToken cancellationToken)
    {
        if (vectorRowIds.Count == 0)
        {
            return;
        }

        var connection = await GetOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        // vectorRowIds are our own previously-stored rowid values (longs), never user input, so
        // inlining them is safe — sqlite-vec's vec0 module doesn't support a bound parameter array for IN (...).
        command.CommandText = $"DELETE FROM vectors.vec_chunks WHERE rowid IN ({string.Join(",", vectorRowIds)});";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqliteConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = (SqliteConnection)dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        return connection;
    }

    // Filenames are the ids themselves (not the caller-supplied original file name), so there is
    // nothing here that needs path-sanitizing and no collision risk between uploads that happen to
    // share a display name — the original name/content type are only ever used for the
    // Content-Disposition/Content-Type on download, from Data.Entities.KnowledgeDocument.
    private string GetDocumentFilePath(Guid groupId, Guid documentId) =>
        Path.Combine(GetGroupFilesDirectory(groupId), documentId.ToString());

    private string GetGroupFilesDirectory(Guid groupId) =>
        Path.Combine(filesRootPath, groupId.ToString());

    // Best-effort: the database row is already gone by the time this runs, which is what every other
    // part of the app treats as the source of truth (e.g. DownloadDocumentAsync 404s on a missing DB
    // row regardless of file state). A leftover/undeleted file under a GUID-keyed path is never reused
    // or exposed through any endpoint, so failing the whole request over cleanup would be misleading —
    // the delete already succeeded from the caller's perspective. In practice this also has to tolerate
    // App_files/ living under OneDrive-synced folders in local dev, where the sync filter driver can
    // transiently hold a lock on a just-emptied directory and make Directory.Delete throw IOException
    // even though nothing is actually wrong.
    private void DeleteDocumentFile(Guid groupId, Guid documentId)
    {
        var filePath = GetDocumentFilePath(groupId, documentId);
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Failed to delete knowledge document file {FilePath}", filePath);
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogWarning(exception, "Failed to delete knowledge document file {FilePath}", filePath);
        }
    }

    private void DeleteGroupFiles(Guid groupId)
    {
        var directory = GetGroupFilesDirectory(groupId);
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Failed to delete knowledge group files directory {Directory}", directory);
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogWarning(exception, "Failed to delete knowledge group files directory {Directory}", directory);
        }
    }

    private static string SerializeEmbedding(IEnumerable<double> embedding) => JsonSerializer.Serialize(embedding);

    private static GroupDto ToGroupDto(KnowledgeGroup group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        CreatedAt = group.CreatedAt
    };

    private static DocumentDto ToDocumentDto(KnowledgeDocument document) => new()
    {
        Id = document.Id,
        GroupId = document.KnowledgeGroupId,
        FileName = document.FileName,
        ContentType = document.ContentType,
        SizeBytes = document.SizeBytes,
        Status = document.Status.ToString(),
        ErrorMessage = document.ErrorMessage,
        UploadedAt = document.UploadedAt
    };
}
