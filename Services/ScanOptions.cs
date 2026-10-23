namespace VulnerabilityTracker.Services;

/// <summary>Настройки сканирования (секция "Scan" в appsettings.json).</summary>
public class ScanOptions
{
    /// <summary>Сколько сканирований может идти одновременно. Остальные ждут в очереди.</summary>
    public int MaxConcurrentScans { get; set; } = 2;

    /// <summary>Сколько секунд "длится" имитация сканирования.</summary>
    public int SimulatedDurationSeconds { get; set; } = 10;
}
