using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PerfectApiTemplate.Infrastructure.Persistence.Logging.Entities;
using PerfectApiTemplate.Infrastructure.Persistence.Logging.Queues;

namespace PerfectApiTemplate.Infrastructure.Persistence.Logging.Workers;

public sealed class ErrorLogWorker : BackgroundService
{
    private readonly LogQueue<ErrorLog> _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ErrorLogWorker> _logger;

    public ErrorLogWorker(LogQueue<ErrorLog> queue, IServiceScopeFactory scopeFactory, ILogger<ErrorLogWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var log in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LogsDbContext>();
                db.ErrorLogs.Add(log);
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist error log");
            }
        }
    }
}
