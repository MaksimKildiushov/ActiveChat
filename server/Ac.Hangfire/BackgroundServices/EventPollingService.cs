using Ac.Application.Events;
using Ac.Data;
using Ac.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ac.Hangfire.BackgroundServices;

/// <summary>
/// Background Service для периодической проверки необработанных событий
/// Служит резервным механизмом на случай, если NOTIFY события не сработали
/// </summary>
public class EventPollingService(
    ILogger<EventPollingService> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    private readonly ILogger<EventPollingService> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private const int PollingIntervalSeconds = 10;
    private const int ProcessingTimeoutMinutes = 5;
    private const int MinEventAgeSeconds = 1; // Минимальный возраст события для обработки (чтобы избежать конфликтов с NOTIFY)

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventPollingService starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAndProcessEventsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EventPollingService, retrying in {Interval} seconds...", PollingIntervalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), stoppingToken);
            }
        }

        _logger.LogInformation("EventPollingService stopping...");
    }

    private async Task PollAndProcessEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiDb>();

        var now = DateTime.UtcNow;
        var minCreatedTime = now.AddSeconds(-MinEventAgeSeconds);

        // Ищем события, которые можно обработать:
        // 1. Pending события, созданные больше чем MinEventAgeSeconds секунд назад (чтобы избежать конфликтов с NOTIFY)
        // 2. Pending события с NextRetryAt <= now (время для повтора пришло)
        // 3. Processing события, которые застряли (обрабатываются больше 5 минут)
        var eventsToProcess = await dbContext.Events
            .Where(e => 
                (e.Status == EventStatus.Pending && 
                 e.Created < minCreatedTime &&
                 (e.NextRetryAt == null || e.NextRetryAt <= now)) ||
                (e.Status == EventStatus.Processing && 
                 e.ProcessingStartedAt != null && 
                 now - e.ProcessingStartedAt.Value > TimeSpan.FromMinutes(ProcessingTimeoutMinutes)))
            .OrderBy(e => e.Created)
            .Take(10) // Обрабатываем максимум 10 событий за раз, чтобы не перегрузить систему
            .ToListAsync(cancellationToken);

        if (eventsToProcess.Count == 0) return;

        _logger.LogInformation("Found {Count} events to process", eventsToProcess.Count);

        foreach (var eventModel in eventsToProcess)
        {
            try
            {
                // Обрабатываем событие
                var evn = new EventClass(dbContext, eventModel, _logger);
                await evn.ProcessAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventId} in EventPollingService", eventModel.Id);
                // Продолжаем обработку следующих событий
            }
        }
    }
}
