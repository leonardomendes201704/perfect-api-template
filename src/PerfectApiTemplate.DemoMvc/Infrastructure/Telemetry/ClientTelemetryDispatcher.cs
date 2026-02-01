using Microsoft.Extensions.Logging;

namespace PerfectApiTemplate.DemoMvc.Infrastructure.Telemetry;

public interface IClientTelemetryDispatcher
{
    void Enqueue(ClientTelemetryEvent telemetryEvent);
}

public sealed class ClientTelemetryDispatcher : IClientTelemetryDispatcher
{
    private readonly ClientTelemetryQueue _queue;
    private readonly ClientTelemetryOptions _options;
    private readonly ILogger<ClientTelemetryDispatcher> _logger;

    public ClientTelemetryDispatcher(
        ClientTelemetryQueue queue,
        Microsoft.Extensions.Options.IOptions<ClientTelemetryOptions> options,
        ILogger<ClientTelemetryDispatcher> logger)
    {
        _queue = queue;
        _options = options.Value;
        _logger = logger;
    }

    public void Enqueue(ClientTelemetryEvent telemetryEvent)
    {
        if (!_options.Enabled)
        {
            return;
        }

        try
        {
            _queue.Enqueue(telemetryEvent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enqueue telemetry event");
        }
    }
}
