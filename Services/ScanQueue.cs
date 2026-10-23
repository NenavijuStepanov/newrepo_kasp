using System.Threading.Channels;
using VulnerabilityTracker.Models;

namespace VulnerabilityTracker.Services;

/// <summary>
/// Очередь задач на сканирование. Контроллер кладёт задачу сюда,
/// фоновый воркер (ScanWorker) забирает по порядку.
/// </summary>
public class ScanQueue
{
    private readonly Channel<ScanJob> _channel =
        Channel.CreateUnbounded<ScanJob>(new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(ScanJob job) => _channel.Writer.TryWrite(job);

    public IAsyncEnumerable<ScanJob> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
