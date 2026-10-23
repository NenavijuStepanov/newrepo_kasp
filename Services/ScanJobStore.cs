using System.Collections.Concurrent;
using VulnerabilityTracker.Models;

namespace VulnerabilityTracker.Services;

public interface IScanJobStore
{
    void Add(ScanJob job);
    bool TryGet(Guid id, out ScanJob job);
}

/// <summary>Хранилище задач сканирования в памяти.</summary>
public class InMemoryScanJobStore : IScanJobStore
{
    private readonly ConcurrentDictionary<Guid, ScanJob> _jobs = new();

    public void Add(ScanJob job) => _jobs[job.Id] = job;

    public bool TryGet(Guid id, out ScanJob job) => _jobs.TryGetValue(id, out job!);
}
