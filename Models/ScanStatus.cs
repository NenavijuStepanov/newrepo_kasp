namespace VulnerabilityTracker.Models;

/// <summary>Состояние задачи сканирования.</summary>
public enum ScanStatus
{
    Queued,     // ждёт своей очереди (для задачи со звёздочкой)
    Running,    // выполняется
    Completed,  // успешно завершена
    Failed,     // завершилась ошибкой
    Cancelled   // отменена (для задачи со звёздочкой)
}
