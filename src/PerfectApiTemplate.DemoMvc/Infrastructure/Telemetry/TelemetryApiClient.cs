using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace PerfectApiTemplate.DemoMvc.Infrastructure.Telemetry;

public sealed class TelemetryApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ApiUrlProvider _urlProvider;
    private readonly ClientTelemetryOptions _options;

    public TelemetryApiClient(HttpClient httpClient, ApiUrlProvider urlProvider, IOptions<ClientTelemetryOptions> options)
    {
        _httpClient = httpClient;
        _urlProvider = urlProvider;
        _options = options.Value;
    }

    public async Task<bool> SendAsync(ClientTelemetryEvent telemetryEvent, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return true;
        }

        var url = new Uri($"{_urlProvider.GetBaseUrl().TrimEnd('/')}/{_options.EndpointPath.TrimStart('/')}");
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(telemetryEvent)
        };

        if (!string.IsNullOrWhiteSpace(_options.InternalKey))
        {
            request.Headers.TryAddWithoutValidation(_options.InternalKeyHeader, _options.InternalKey);
        }

        if (!string.IsNullOrWhiteSpace(telemetryEvent.CorrelationId))
        {
            request.Headers.TryAddWithoutValidation(ClientCorrelationContext.CorrelationHeader, telemetryEvent.CorrelationId);
        }

        var apiRequestId = string.IsNullOrWhiteSpace(telemetryEvent.ApiRequestId)
            ? Guid.NewGuid().ToString("N")
            : telemetryEvent.ApiRequestId;
        request.Headers.TryAddWithoutValidation("X-Request-Id", apiRequestId);

        if (!string.IsNullOrWhiteSpace(telemetryEvent.RequestId))
        {
            request.Headers.TryAddWithoutValidation(ClientCorrelationContext.ClientRequestHeader, telemetryEvent.RequestId);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
