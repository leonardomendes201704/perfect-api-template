using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PerfectApiTemplate.Application.Abstractions.Auth;
using PerfectApiTemplate.Infrastructure.Persistence;

namespace PerfectApiTemplate.Tests.Integration.Fixtures;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                ["AdminUser:Email"] = "admin@admin.com.br",
                ["AdminUser:Password"] = "Naotemsenha0!",
                ["AdminUser:FullName"] = "System Administrator"
            };
            config.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IExternalAuthService>();
            services.AddSingleton<IExternalAuthService, FakeExternalAuthService>();
            services.PostConfigure<PerfectApiTemplate.Infrastructure.Auth.AdminUserOptions>(options =>
            {
                options.Email = "admin@admin.com.br";
                options.Password = "Naotemsenha0!";
                options.FullName = "System Administrator";
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();

            var adminSeeder = scope.ServiceProvider.GetRequiredService<PerfectApiTemplate.Infrastructure.Auth.AdminUserSeeder>();
            adminSeeder.SeedAsync().GetAwaiter().GetResult();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

internal sealed class FakeExternalAuthService : IExternalAuthService
{
    public Task<ExternalUserInfo> GetUserInfoAsync(string provider, string accessToken, CancellationToken cancellationToken = default)
    {
        var info = new ExternalUserInfo(provider, "external-id-1", "external@example.com", "External User");
        return Task.FromResult(info);
    }
}
