using System.Net.Http.Json;
using AiGateway.Service.Ollama.Dtos;
using AiGateway.Service.Ollama.Interfaces;

namespace AiGateway.Service.Ollama;

public class OllamaService(HttpClient httpClient) : IOllamaService
{
    public async Task<GenerateResultDto> GenerateAsync(GenerateRequestDto request, CancellationToken cancellationToken)
    {
        request.Stream = false;
        return await PostAsync<GenerateRequestDto, GenerateResultDto>("api/generate", request, cancellationToken);
    }

    public async Task<ChatResultDto> ChatAsync(ChatRequestDto request, CancellationToken cancellationToken)
    {
        request.Stream = false;
        return await PostAsync<ChatRequestDto, ChatResultDto>("api/chat", request, cancellationToken);
    }

    public async Task<EmbedResultDto> EmbedAsync(EmbedRequestDto request, CancellationToken cancellationToken)
    {
        return await PostAsync<EmbedRequestDto, EmbedResultDto>("api/embed", request, cancellationToken);
    }

    public async Task<ListModelsResultDto> ListModelsAsync(CancellationToken cancellationToken)
    {
        var result = await httpClient.GetFromJsonAsync<ListModelsResultDto>("api/tags", cancellationToken);
        return result ?? new ListModelsResultDto();
    }

    public async Task<ListRunningModelsResultDto> ListRunningModelsAsync(CancellationToken cancellationToken)
    {
        var result = await httpClient.GetFromJsonAsync<ListRunningModelsResultDto>("api/ps", cancellationToken);
        return result ?? new ListRunningModelsResultDto();
    }

    public async Task<ShowModelResultDto> ShowModelAsync(ShowModelRequestDto request, CancellationToken cancellationToken)
    {
        return await PostAsync<ShowModelRequestDto, ShowModelResultDto>("api/show", request, cancellationToken);
    }

    public async Task CopyModelAsync(CopyModelRequestDto request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("api/copy", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteModelAsync(DeleteModelRequestDto request, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, "api/delete")
        {
            Content = JsonContent.Create(request)
        };
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<PullModelResultDto> PullModelAsync(PullModelRequestDto request, CancellationToken cancellationToken)
    {
        request.Stream = false;
        return await PostAsync<PullModelRequestDto, PullModelResultDto>("api/pull", request, cancellationToken);
    }

    public async Task<PushModelResultDto> PushModelAsync(PushModelRequestDto request, CancellationToken cancellationToken)
    {
        request.Stream = false;
        return await PostAsync<PushModelRequestDto, PushModelResultDto>("api/push", request, cancellationToken);
    }

    public async Task<CreateModelResultDto> CreateModelAsync(CreateModelRequestDto request, CancellationToken cancellationToken)
    {
        request.Stream = false;
        return await PostAsync<CreateModelRequestDto, CreateModelResultDto>("api/create", request, cancellationToken);
    }

    public async Task<VersionResultDto> GetVersionAsync(CancellationToken cancellationToken)
    {
        var result = await httpClient.GetFromJsonAsync<VersionResultDto>("api/version", cancellationToken);
        return result ?? new VersionResultDto();
    }

    private async Task<TResult> PostAsync<TRequest, TResult>(string requestUri, TRequest request, CancellationToken cancellationToken)
        where TResult : new()
    {
        using var response = await httpClient.PostAsJsonAsync(requestUri, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<TResult>(cancellationToken);
        return result ?? new TResult();
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Ollama request to '{response.RequestMessage?.RequestUri}' failed with status {(int)response.StatusCode} ({response.StatusCode}): {body}",
            null,
            response.StatusCode);
    }
}
