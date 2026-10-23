using System.ComponentModel.DataAnnotations;

namespace VulnerabilityTracker.Models;

// ---------- Уязвимости ----------

public class CreateVulnerabilityRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required, EnumDataType(typeof(Severity))]
    public Severity? Severity { get; set; }
}

public record VulnerabilityResponse(
    Guid Id,
    string Name,
    Severity Severity,
    DateTime CreatedAt);

// ---------- Сканирование ----------

public class StartScanRequest
{
    [Required, Url]
    public string RepositoryUrl { get; set; } = string.Empty;
}

public record StartScanResponse(Guid ScanId);

public record ScanStatusResponse(
    Guid Id,
    ScanStatus Status,
    DateTime UpdatedAt,
    ScanResult? Result,   // только если завершена
    string? Error);       // только если ошибка

// ---------- Отмена (задача со звёздочкой) ----------

public record CancelScanResponse(
    Guid Id,
    bool Success,
    DateTime CancelledAt);
