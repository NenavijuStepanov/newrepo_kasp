# Vulnerability Tracker

Небольшой REST API на ASP.NET Core (C#) для учёта уязвимостей и имитации фонового сканирования репозиториев.

- Данные хранятся **только в памяти**: после перезапуска сервиса всё пропадает (база данных не используется по условию задания).
- Сканирование - **имитация**: задача "работает" несколько секунд и возвращает случайный результат.
- Реализованы дополнительные задачи: **ограничение числа параллельных сканирований** (с очередью) и **отмена сканирования**.

## Требования

- [.NET SDK](https://dotnet.microsoft.com/download) 10.0

## Запуск

```bash
cd VulnerabilityTracker
dotnet run
```

В консоли появится адрес, например `http://localhost:5123`. Swagger UI доступен по адресу:

```
http://localhost:5123/swagger
```

Порт берётся из `Properties/launchSettings.json`. Чтобы запустить на другом порту:

```bash
dotnet run --urls http://localhost:5200
```

## Тесты

```bash
dotnet test
```

Что покрыто:

- хранилище уязвимостей (добавление, получение, неизвестный ID);
- `ScanService` (запуск, статус, отмена в разных состояниях);
- `ScanWorker` (успех, ошибка, лимит параллельности, отмена задачи в очереди и выполняющейся задачи);
- HTTP API целиком (коды 201/202/400/404, сценарий "запустить и отменить").

## Настройки

Секция `Scan` в `appsettings.json`:

```json
"Scan": {
  "MaxConcurrentScans": 2,
  "SimulatedDurationSeconds": 10
}
```

| Параметр | Описание | По умолчанию |
|---|---|---|
| `MaxConcurrentScans` | Сколько сканирований выполняется одновременно. Остальные ждут в очереди (статус `Queued`) | 2 |
| `SimulatedDurationSeconds` | Длительность имитации сканирования, сек | 10 |

## API

| Метод | Путь | Описание | Ответы |
|---|---|---|---|
| POST | `/api/vulnerabilities` | Добавить уязвимость | 201, 400 |
| GET | `/api/vulnerabilities/{id}` | Получить уязвимость по ID | 200, 404 |
| POST | `/api/scans` | Запустить сканирование репозитория (в фоне) | 202, 400 |
| GET | `/api/scans/{id}` | Статус сканирования (и результат, если завершено) | 200, 404 |
| POST | `/api/scans/{id}/cancel` | Отменить сканирование | 200, 404 |

### Уровни критичности (`severity`)

`Low`, `Medium`, `High`, `Critical`

### Статусы сканирования (`status`)

| Статус | Значение |
|---|---|
| `Queued` | Ожидает в очереди |
| `Running` | Выполняется |
| `Completed` | Завершено успешно, есть `result` |
| `Failed` | Завершено с ошибкой, текст в `error` |
| `Cancelled` | Отменено |

> Для проверки статуса `Failed` запустите сканирование с URL, содержащим слово `fail`, например `https://gitlab.com/user/fail-repo`.

## Примеры запросов (curl)

```bash
# Добавить уязвимость
curl -X POST http://localhost:5123/api/vulnerabilities \
  -H "Content-Type: application/json" \
  -d '{"name": "SQL Injection", "severity": "High"}'

# Получить уязвимость
curl http://localhost:5123/api/vulnerabilities/{id}

# Запустить сканирование (вернёт scanId сразу)
curl -X POST http://localhost:5123/api/scans \
  -H "Content-Type: application/json" \
  -d '{"repositoryUrl": "https://gitlab.com/user/repo"}'

# Узнать статус
curl http://localhost:5123/api/scans/{scanId}

# Отменить сканирование
curl -X POST http://localhost:5123/api/scans/{scanId}/cancel
```

Готовые запросы также лежат в файле `requests.http` (Visual Studio, VS Code с расширением REST Client, Rider).

### Пример ответов

`POST /api/scans` -> `202 Accepted`:

```json
{ "scanId": "3f2c0e5a-6b0d-4c7e-9a31-2f4f6a1b9c10" }
```

`GET /api/scans/{id}` для завершённой задачи:

```json
{
  "id": "3f2c0e5a-6b0d-4c7e-9a31-2f4f6a1b9c10",
  "status": "Completed",
  "updatedAt": "2026-10-23T12:00:10Z",
  "result": {
    "filesScanned": 214,
    "findings": [
      { "title": "Potential issue #1", "severity": "High", "file": "src/File7.cs" }
    ]
  },
  "error": null
}
```

`POST /api/scans/{id}/cancel`:

```json
{ "id": "3f2c0e5a-6b0d-4c7e-9a31-2f4f6a1b9c10", "success": true, "cancelledAt": "2026-10-23T12:00:03Z" }
```

Если отмена невозможна (задача уже завершена, провалена или отменена), возвращается `200` и `"success": false`.

Ошибки возвращаются в формате [ProblemDetails](https://datatracker.ietf.org/doc/html/rfc7807) (например, 404 для неизвестного ID, 400 для неверных данных, 500 для непредвиденных ошибок без внутренних деталей).

## Структура проекта

```
VulnerabilityTracker/
├── Controllers/
│   ├── VulnerabilitiesController.cs   # эндпоинты уязвимостей
│   └── ScansController.cs             # эндпоинты сканирования
├── Models/                            # сущности, перечисления и DTO запросов/ответов
├── Services/
│   ├── VulnerabilityStore.cs          # хранилище уязвимостей в памяти
│   ├── ScanJobStore.cs                # хранилище задач сканирования в памяти
│   ├── ScanQueue.cs                   # очередь задач (Channel)
│   ├── ScanWorker.cs                  # фоновый сервис: лимит параллельности, обработка, отмена
│   ├── ScanService.cs                 # запуск, статус и отмена (используется контроллерами)
│   ├── Scanner.cs                     # имитация сканера
│   └── ScanOptions.cs                 # настройки
├── Infrastructure/
│   ├── RequestLoggingMiddleware.cs    # логирование запросов
│   └── GlobalExceptionHandler.cs      # обработка необработанных исключений
└── Program.cs
VulnerabilityTracker.Tests/            # xUnit-тесты
```

## Как это работает

1. `POST /api/scans` создаёт задачу со статусом `Queued`, кладёт её в очередь (`Channel`) и сразу возвращает ID.
2. `ScanWorker` (`BackgroundService`) по порядку забирает задачи из очереди. Семафор (`SemaphoreSlim`) не даёт запустить больше `MaxConcurrentScans` сканирований одновременно.
3. Отмена использует `CancellationToken`: задача, ожидающая в очереди, пропускается, а выполняющаяся - прерывается. Переходы статусов защищены блокировкой, чтобы отмена и завершение не перезаписывали друг друга.

## Допущения и ограничения

- Хранилища в памяти: данные теряются при перезапуске, история задач не очищается.
- Сканер - заглушка (`FakeScanner`); реальный сканер можно подключить, реализовав интерфейс `IScanner`.
- Аутентификации нет.
- Swagger включён всегда для удобства проверки.

## Автор

Степанов К.Ю.
