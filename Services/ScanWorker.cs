using Microsoft.Extensions.Options;
using VulnerabilityTracker.Models;

namespace VulnerabilityTracker.Services;

/// <summary>
/// Фоновый сервис: берёт задачи из очереди и запускает сканирование,
/// но не более MaxConcurrentScans одновременно. Остальные остаются в статусе Queued.
/// </summary>
public class ScanWorker : BackgroundService
{
    private readonly ScanQueue _queue;
    private readonly IScanner _scanner;
    private readonly ScanOptions _options;
    private readonly ILogger<ScanWorker> _logger;

    public ScanWorker(ScanQueue queue, IScanner scanner,
        IOptions<ScanOptions> options, ILogger<ScanWorker> logger)
    {
        _queue = queue;
        _scanner = scanner;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var limit = Math.Max(1, _options.MaxConcurrentScans);
        var semaphore = new SemaphoreSlim(limit, limit);

        try
        {
            await foreach (var job in _queue.ReadAllAsync(stoppingToken))
            {
                // Ждём свободный "слот". Если задачу отменили, пока она ждала, - пропускаем её.
                using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(
                    stoppingToken, job.Cts.Token);
                try
                {
                    await semaphore.WaitAsync(waitCts.Token);
                }
                catch (OperationCanceledException)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    continue; // отменена в очереди
                }

                _ = Task.Run(async () =>
                {
                    try { await ProcessAsync(job, stoppingToken); }
                    finally { semaphore.Release(); }
                }, CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальная остановка приложения
        }
    }

    private async Task ProcessAsync(ScanJob job, CancellationToken stoppingToken)
    {
        // Между извлечением из очереди и стартом задачу могли отменить
        lock (job)
        {
            if (job.Status != ScanStatus.Queued) return;
            job.SetStatus(ScanStatus.Running);
        }

        _logger.LogInformation("Scan {ScanId} started", job.Id);

        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken, job.Cts.Token);

        try
        {
            var result = await _scanner.ScanAsync(job.RepositoryUrl, runCts.Token);

            lock (job)
            {
                // Если за это время задачу отменили - не перезаписываем статус
                if (job.Status != ScanStatus.Running) return;
                job.Result = result;
                job.SetStatus(ScanStatus.Completed);
            }
            _logger.LogInformation("Scan {ScanId} completed", job.Id);
        }
        catch (OperationCanceledException)
        {
            lock (job)
            {
                if (job.Status == ScanStatus.Running) job.SetStatus(ScanStatus.Cancelled);
            }
            _logger.LogInformation("Scan {ScanId} interrupted", job.Id);
        }
        catch (Exception ex)
        {
            lock (job)
            {
                if (job.Status != ScanStatus.Running) return;
                job.Error = ex.Message;
                job.SetStatus(ScanStatus.Failed);
            }
            _logger.LogError(ex, "Scan {ScanId} failed", job.Id);
        }
    }
}
