using System.Text.Json.Serialization;
using VulnerabilityTracker.Infrastructure;
using VulnerabilityTracker.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Контроллеры + enum как строки ("High", а не 2) ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---------- Swagger ----------
// Нужен пакет: dotnet add package Swashbuckle.AspNetCore
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------- Обработка ошибок ----------
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---------- Наши сервисы ----------
builder.Services.Configure<ScanOptions>(builder.Configuration.GetSection("Scan"));

builder.Services.AddSingleton<IVulnerabilityStore, InMemoryVulnerabilityStore>();
builder.Services.AddSingleton<IScanJobStore, InMemoryScanJobStore>();
builder.Services.AddSingleton<ScanQueue>();
builder.Services.AddSingleton<IScanner, FakeScanner>();
builder.Services.AddSingleton<IScanService, ScanService>();
builder.Services.AddHostedService<ScanWorker>();

var app = builder.Build();

// Порядок важен: логирование - первым, чтобы видеть итоговый код ответа,
// в том числе 500 после работы обработчика ошибок.
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages(); // 404/405 и т.п. тоже вернутся в формате ProblemDetails

// Swagger включён всегда, чтобы проверяющий мог сразу открыть /swagger
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

// Нужно, чтобы тесты (WebApplicationFactory) видели класс Program
public partial class Program { }
