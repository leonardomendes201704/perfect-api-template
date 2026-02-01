using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PerfectApiTemplate.DemoMvc.Infrastructure;

namespace PerfectApiTemplate.DemoMvc.ApiClients;

public abstract class ApiClientBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApiUrlProvider _urlProvider;

    protected ApiClientBase(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ApiUrlProvider urlProvider)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _urlProvider = urlProvider;
    }

    protected async Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(path));
        AddAuthHeader(request);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<T>(response, cancellationToken);
    }

    protected async Task<ApiResult<T>> PostAsync<T>(string path, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(path));
        AddAuthHeader(request);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<T>(response, cancellationToken);
    }

    protected async Task<ApiResult<T>> PutAsync<T>(string path, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, BuildUri(path));
        AddAuthHeader(request);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<T>(response, cancellationToken);
    }

    protected async Task<ApiResult<T>> DeleteAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, BuildUri(path));
        AddAuthHeader(request);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<T>(response, cancellationToken);
    }

    protected async Task<ApiResult<string>> PostAsync(string path, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(path));
        AddAuthHeader(request);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<string>(response, cancellationToken);
    }

    private Uri BuildUri(string path)
    {
        var baseUrl = _urlProvider.GetBaseUrl().TrimEnd('/');
        return new Uri($"{baseUrl}/{path.TrimStart('/')}");
    }

    private void AddAuthHeader(HttpRequestMessage request)
    {
        var token = _httpContextAccessor.HttpContext?.Session.GetStringValue(SessionKeys.JwtToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static async Task<ApiResult<T>> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        var content = response.Content is null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            if (typeof(T) == typeof(string))
            {
                return ApiResult<T>.Success((T)(object)(content ?? string.Empty), statusCode);
            }

            var data = string.IsNullOrWhiteSpace(content)
                ? default
                : JsonSerializer.Deserialize<T>(content, JsonOptions);
            return ApiResult<T>.Success(data!, statusCode);
        }

        var error = ExtractProblemDetails(content) ?? $"{response.ReasonPhrase} ({statusCode})";
        return ApiResult<T>.Failure(error, statusCode);
    }

    private static string? ExtractProblemDetails(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            var problem = JsonSerializer.Deserialize<ProblemDetails>(content, JsonOptions);
            if (problem is not null)
            {
                return string.IsNullOrWhiteSpace(problem.Detail) ? problem.Title : problem.Detail;
            }
        }
        catch
        {
        }

        return content;
    }
}

