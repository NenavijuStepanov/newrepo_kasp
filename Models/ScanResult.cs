namespace VulnerabilityTracker.Models;

/// <summary>Результат сканирования (в задании - имитация).</summary>
public class ScanResult
{
    public int FilesScanned { get; init; }
    public List<Finding> Findings { get; init; } = new();
}

/// <summary>Одна найденная проблема.</summary>
public class Finding
{
    public required string Title { get; init; }
    public required Severity Severity { get; init; }
    public string? File { get; init; }
}
