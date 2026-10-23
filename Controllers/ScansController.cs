using Microsoft.AspNetCore.Mvc;
using VulnerabilityTracker.Models;
using VulnerabilityTracker.Services;

namespace VulnerabilityTracker.Controllers;

[ApiController]
[Route("api/scans")]
public class ScansController : ControllerBase
{
    private readonly IScanService _scans;

    public ScansController(IScanService scans) => _scans = scans;

    /// <summary>Запустить сканирование репозитория в фоне. Сразу возвращает ID задачи.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(StartScanResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<StartScanResponse> Start([FromBody] StartScanRequest request)
    {
        var scanId = _scans.StartScan(request.RepositoryUrl.Trim());

        // 202 Accepted: задача принята, но ещё не выполнена
        return AcceptedAtAction(
            nameof(GetStatus),
            new { id = scanId },
            new StartScanResponse(scanId));
    }

    /// <summary>Получить статус задачи сканирования (и результат, если она завершена).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ScanStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<ScanStatusResponse> GetStatus(Guid id)
    {
        var status = _scans.GetStatus(id);
        return status is null ? NotFoundScan(id) : status;
    }

    /// <summary>Отменить сканирование (в очереди или уже запущенное).</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(CancelScanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<CancelScanResponse> Cancel(Guid id)
    {
        var result = _scans.Cancel(id);
        return result is null ? NotFoundScan(id) : result;
    }

    private ObjectResult NotFoundScan(Guid id) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Scan not found",
            detail: $"Задача сканирования с ID {id} не найдена.");
}
