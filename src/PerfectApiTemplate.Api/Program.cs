using System.Diagnostics;
using System.Text;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PerfectApiTemplate.Api.Middleware;
using PerfectApiTemplate.Application;
using PerfectApiTemplate.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --------------------------
// Logging (Serilog)
// --------------------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// --------------------------
// MVC
// --------------------------
builder.Services.AddControllers();

// --------------------------
// DI (Application + Infrastructure)
// --------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --------------------------
// JWT Auth (dev-friendly defaults; fail-fast outside dev)
// --------------------------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];
var jwtSigningKey = jwtSection["SigningKey"];

if (builder.Environment.IsDevelopment())
{
    jwtIssuer ??= "dev-issuer";
    jwtAudience ??= "dev-audience";

    if (string.IsNullOrWhiteSpace(jwtSigningKey))
    {
        jwtSigningKey = "dev-signing-key-change-me";
        Log.Warning("Jwt:SigningKey was not set. Using a development default signing key. DO NOT use this in production.");
    }
}
else
{
    if (string.IsNullOrWhiteSpace(jwtIssuer) ||
        string.IsNullOrWhiteSpace(jwtAudience) ||
        string.IsNullOrWhiteSpace(jwtSigningKey))
    {
        throw new InvalidOperationException(
            "JWT configuration is missing. Please set Jwt:Issuer, Jwt:Audience, and Jwt:SigningKey for non-Development environments.");
    }
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey!))
        };
    });

builder.Services.AddAuthorization();

// --------------------------
// Health Checks
// --------------------------
builder.Services.AddHealthChecks();

// --------------------------
// ProblemDetails + CorrelationId
// --------------------------
builder.Services.AddProblemDetails(options =>
{
    options.IncludeExceptionDetails = (context, _) => builder.Environment.IsDevelopment();
    options.OnBeforeWriteDetails = (context, details) =>
    {
        if (context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var value) && value is string correlationId)
        {
            details.Extensions["correlationId"] = correlationId;
        }
    };
});

// --------------------------
// Swagger (JWT)
// Ensures UI is available at: /swagger  (index is /swagger/index.html)
// --------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Perfect API Template", Version = "v1" });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    };

    options.AddSecurityDefinition("Bearer", bearerScheme);

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document, "Bearer"), new List<string>() }
    });
});

var app = builder.Build();

// --------------------------
// Middleware pipeline
// --------------------------
app.UseSerilogRequestLogging();

// CorrelationId must run BEFORE ProblemDetails so errors include correlationId
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseProblemDetails();

// Swagger JSON endpoint: /swagger/v1/swagger.json
app.UseSwagger(c => { c.RouteTemplate = "swagger/{documentName}/swagger.json"; });

// Swagger UI endpoint: /swagger (index: /swagger/index.html)
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "swagger";
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Perfect API Template v1");
});

// --------------------------
// Auto-open browser on startup (Development only)
// Opens: /swagger/index.html
// --------------------------
if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Swagger:OpenOnStart", true))
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        try
        {
            // Resolve actual bound addresses (e.g., http://localhost:5000)
            var server = app.Services.GetRequiredService<IServer>();
            var addressesFeature = server.Features.Get<IServerAddressesFeature>();
            var baseAddress = addressesFeature?.Addresses?.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                // Fallback if addresses not available
                baseAddress = "http://localhost:5000";
            }

            // Replace wildcard host with localhost for browser
            baseAddress = baseAddress
                .Replace("0.0.0.0", "localhost")
                .Replace("[::]", "localhost")
                .TrimEnd('/');

            var swaggerUrl = $"{baseAddress}/swagger/index.html";

            Process.Start(new ProcessStartInfo
            {
                FileName = swaggerUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // Non-fatal: ignore if the environment cannot launch a browser.
        }
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
