using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using PerfectApiTemplate.Application.Abstractions.Logging;
using PerfectApiTemplate.Application.Common.Logging;
using Serilog.Context;

namespace PerfectApiTemplate.Api.Middleware;

public sealed class ErrorLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IErrorLogWriter _writer;
    private readonly LoggingOptions _options;
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogger<ErrorLoggingMiddleware> _logger;

    public ErrorLoggingMiddleware(
        RequestDelegate next,
        IErrorLogWriter writer,
        IOptions<LoggingOptions> options,
        ICorrelationContext correlationContext,
        ILogger<ErrorLoggingMiddleware> logger)
    {
        _next = next;
        _writer = writer;
        _options = options.Value;
        _correlationContext = correlationContext;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            using (LogContext.PushProperty("CorrelationId", _correlationContext.CorrelationId ?? string.Empty))
            using (LogContext.PushProperty("RequestId", _correlationContext.RequestId ?? string.Empty))
            {
                await _next(context);
            }
        }
        catch (Exception ex)
        {
            if (_options.Errors.Enabled)
            {
                try
                {
                    await LogExceptionAsync(context, ex);
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "Failed to enqueue error log");
                }
            }

            throw;
        }
    }

    private async Task LogExceptionAsync(HttpContext context, Exception ex)
    {
        string? requestBody = null;
        bool requestTruncated = false;
        long? requestOriginalLength = context.Request.ContentLength;
        if (IsSafeContentType(context.Request.ContentType, _options.Requests.ExcludedContentTypes))
        {
            (requestBody, requestTruncated, requestOriginalLength) =
                await ReadRequestBodyAsync(context.Request, _options.Errors.MaxBodyBytes);
            requestBody = LogMasking.SanitizeJson(requestBody, _options.Mask.JsonKeys, _options.Mask.JsonPaths, _options.Mask.Enabled);
        }

        var headers = LogMasking.SanitizeHeaders(
            context.Request.Headers.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())),
            _options.Requests.AllowedHeaders.Length > 0 ? _options.Requests.AllowedHeaders : _options.Mask.HeaderAllowList,
            _options.Mask.HeaderDenyList);

        var entry = new ErrorLogEntry(
            DateTime.UtcNow,
            ex.GetType().FullName ?? ex.GetType().Name,
            ex.Message,
            ex.StackTrace,
            FlattenInnerExceptions(ex),
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
            headers,
            requestBody,
            requestTruncated,
            requestOriginalLength,
            context.Response?.StatusCode,
            _correlationContext.UserId,
            _correlationContext.TenantId,
            _correlationContext.CorrelationId,
            _correlationContext.RequestId,
            _correlationContext.TraceId,
            _correlationContext.SpanId,
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            Environment.MachineName,
            typeof(Program).Assembly.GetName().Version?.ToString());

        _writer.Enqueue(entry);
    }

    private static string FlattenInnerExceptions(Exception ex)
    {
        var messages = new List<string>();
        var current = ex.InnerException;
        while (current is not null)
        {
            messages.Add($"{current.GetType().Name}: {current.Message}");
            current = current.InnerException;
        }

        return string.Join(" | ", messages);
    }

    private static bool IsSafeContentType(string? contentType, string[] excluded)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        if (excluded.Any(x => contentType.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)
               || contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<(string? body, bool truncated, long? originalLength)> ReadRequestBodyAsync(HttpRequest request, int maxBytes)
    {
        request.EnableBuffering();
        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var buffer = new char[maxBytes + 1];
        var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
        request.Body.Position = 0;

        if (read <= maxBytes)
        {
            return (new string(buffer, 0, read), false, read);
        }

        var truncated = new string(buffer, 0, maxBytes);
        return (truncated, true, read);
    }
}
