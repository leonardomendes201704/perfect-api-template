using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerfectApiTemplate.Application.Abstractions;
using PerfectApiTemplate.Infrastructure.Persistence;
using PerfectApiTemplate.Infrastructure.Services;
using PerfectApiTemplate.Infrastructure.Messaging;
using PerfectApiTemplate.Application.Abstractions.Notifications;
using PerfectApiTemplate.Application.Abstractions.Auth;
using PerfectApiTemplate.Infrastructure.Auth;

namespace PerfectApiTemplate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserProviderRepository, UserProviderRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddHttpClient<IExternalAuthService, ExternalAuthService>();
        services.Configure<ExternalAuthOptions>(options =>
            configuration.GetSection("ExternalAuth").Bind(options));
        services.Configure<AdminUserOptions>(options =>
            configuration.GetSection("AdminUser").Bind(options));
        services.AddScoped<AdminUserSeeder>();
        services.AddScoped<INotificationPublisher, NoOpNotificationPublisher>();
        services.AddHostedService<OutboxProcessor>();
        services.Configure<OutboxProcessorOptions>(options =>
        {
            var section = configuration.GetSection("Outbox");
            options.BatchSize = section.GetValue("BatchSize", options.BatchSize);
            options.PollingSeconds = section.GetValue("PollingSeconds", options.PollingSeconds);
        });
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}

