namespace PerfectApiTemplate.DemoMvc.ApiClients;

public sealed class HealthApiClient : ApiClientBase
{
    public HealthApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, Infrastructure.ApiUrlProvider urlProvider)
        : base(httpClient, httpContextAccessor, urlProvider)
    {
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        var result = await GetAsync<string>("/health", cancellationToken);
        return result.IsSuccess;
    }
}

