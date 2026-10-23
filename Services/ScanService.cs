using VulnerabilityTracker.Models;

namespace VulnerabilityTracker.Services;

public interface IScanService
{
    Guid StartScan(string repositoryUrl);
    ScanStatusResponse? GetStatus(Guid id);   // null = задача не найдена
    CancelScanResponse? Cancel(Guid id);      // null = задача не найдена
}

/// <summary>Логика, которую вызывают контроллеры: запуск, статус, отмена.</summary>
public class ScanService : IScanService
{
    private readonly IScanJobStore _store;
    private readonly ScanQueue _queue;
    private readonly ILogger<ScanService> _logger;

    public ScanService(IScanJobStore store, ScanQueue queue, ILogger<ScanService> logger)
    {
        _store = store;
        _queue = queue;
        _logger = logger;
    }

    public Guid StartScan(string repositoryUrl)
    {
        var job = new ScanJob { RepositoryUrl = repositoryUrl };
        _store.Add(job);
        _queue.Enqueue(job);

        _logger.LogInformation("Scan {ScanId} queued for {Repo}", job.Id, repositoryUrl);
        return job.Id;
    }

    public ScanStatusResponse? GetStatus(Guid id)
    {
        if (!_store.TryGet(id, out var job)) return null;

        // lock, чтобы не прочитать задачу в момент, когда воркер её меняет
        lock (job)
        {
            return new ScanStatusResponse(job.Id, job.Status, job.UpdatedAt, job.Result, job.Error);
        }
    }

    public CancelScanResponse? Cancel(Guid id)
    {
        if (!_store.TryGet(id, out var job)) return null;

        lock (job)
        {
            // Отменить можно только ожидающую или выполняющуюся задачу
            if (job.Status is not (ScanStatus.Queued or ScanStatus.Running))
                return new CancelScanResponse(id, false, DateTime.UtcNow);

            job.SetStatus(ScanStatus.Cancelled);
            job.Cts.Cancel(); // прерывает и ожидание в очереди, и идущее сканирование
        }

        _logger.LogInformation("Scan {ScanId} cancelled", id);
        return new CancelScanResponse(id, true, DateTime.UtcNow);
    }
}
