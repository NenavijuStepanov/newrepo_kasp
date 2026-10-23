namespace VulnerabilityTracker.Models;

/// <summary>Задача сканирования репозитория (то, что хранится в памяти).</summary>
public class ScanJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string RepositoryUrl { get; init; }

    public ScanStatus Status { get; set; } = ScanStatus.Queued;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Заполняется, только когда Status == Completed.</summary>
    public ScanResult? Result { get; set; }

    /// <summary>Текст ошибки, когда Status == Failed.</summary>
    public string? Error { get; set; }

    /// <summary>Нужен для отмены (задача со звёздочкой).</summary>
    public CancellationTokenSource Cts { get; } = new();

    /// <summary>Меняет статус и обновляет время последнего изменения.</summary>
    public void SetStatus(ScanStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }
}
