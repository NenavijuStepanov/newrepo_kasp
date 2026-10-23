using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace VulnerabilityTracker.Infrastructure;

/// <summary>
/// Ловит все необработанные исключения: пишет их в лог
/// и отдаёт клиенту аккуратный ответ 500 без внутренних деталей.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception on {Method} {Path}",
            context.Request.Method, context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal server error",
            Detail = "Произошла непредвиденная ошибка. Попробуйте повторить запрос позже."
        }, cancellationToken);

        return true; // исключение обработано
    }
}
