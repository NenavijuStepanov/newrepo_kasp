using Microsoft.Extensions.Options;
using VulnerabilityTracker.Models;

namespace VulnerabilityTracker.Services;

public interface IScanner
{
    Task<ScanResult> ScanAsync(string repositoryUrl, CancellationToken ct);
}

/// <summary>
/// Имитация сканера: "работает" заданное время и возвращает случайный результат.
/// Если в URL есть слово "fail" - бросает ошибку (чтобы можно было проверить статус Failed).
/// </summary>
public class FakeScanner : IScanner
{
    private readonly ScanOptions _options;

    public FakeScanner(IOptions<ScanOptions> options) => _options = options.Value;

    public async Task<ScanResult> ScanAsync(string repositoryUrl, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(_options.SimulatedDurationSeconds), ct);

        if (repositoryUrl.Contains("fail", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Не удалось выполнить сканирование репозитория.");

        var random = new Random();
        var severities = Enum.GetValues<Severity>();
        var findings = Enumerable.Range(1, random.Next(0, 5))
            .Select(i => new Finding
            {
                Title = $"Potential issue #{i}",
                Severity = severities[random.Next(severities.Length)],
                File = $"src/File{random.Next(1, 20)}.cs"
            })
            .ToList();

        return new ScanResult { FilesScanned = random.Next(50, 500), Findings = findings };
    }
}
