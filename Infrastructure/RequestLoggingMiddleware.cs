using System.Diagnostics;

namespace VulnerabilityTracker.Infrastructure;

/// <summary>Логирует каждый запрос: метод, путь, код ответа и время выполнения.</summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var status = context.Response.StatusCode;

            var level = status >= 500 ? LogLevel.Error
                      : status >= 400 ? LogLevel.Warning
                      : LogLevel.Information;

            _logger.Log(level, "HTTP {Method} {Path}{Query} -> {StatusCode} in {ElapsedMs} ms",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                status,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
