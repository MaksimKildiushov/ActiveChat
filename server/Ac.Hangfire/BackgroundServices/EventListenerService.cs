using Ac.Application.Events;
using Ac.Data;
using Ac.Domain.Enums;
using Ac.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ac.Hangfire.BackgroundServices;

/// <summary>
/// Background Service для прослушивания PostgreSQL NOTIFY событий
/// и постановки их в очередь Hangfire
/// </summary>
public class EventListenerService(
    IConfiguration configuration,
    ILogger<EventListenerService> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<EventListenerService> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private NpgsqlConnection? _connection;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventListenerService starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndListenAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EventListenerService, reconnecting in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("EventListenerService stopping...");
    }

    private async Task ConnectAndListenAsync(CancellationToken stoppingToken)
    {
        var connectionString = _configuration.GetConnectionString("Default");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Default' not found");
        }

        _connection = new NpgsqlConnection(connectionString);
        await _connection.OpenAsync(stoppingToken);

        _logger.LogInformation("Connected to PostgreSQL, listening for events...");

        // Подписываемся на уведомления
        _connection.Notification += async (sender, e) =>
        {
            try
            {
                await HandleNotificationAsync(e.Payload, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling notification: {Payload}", e.Payload);
            }
        };

        // Начинаем слушать канал 'events'
        await using var cmd = new NpgsqlCommand("LISTEN events", _connection);
        await cmd.ExecuteNonQueryAsync(stoppingToken);

        _logger.LogInformation("Listening on channel 'events'");

        // Ожидаем уведомлений
        while (!stoppingToken.IsCancellationRequested && _connection.State == System.Data.ConnectionState.Open)
        {
            try
            {
                await _connection.WaitAsync(stoppingToken);
            }
            catch (PostgresException ex) when (ex.SqlState == "57P03") // connection_failure
            {
                _logger.LogWarning("Connection lost, will reconnect...");
                break; // Выходим из цикла, чтобы переподключиться
            }
            catch (OperationCanceledException)
            {
                break; // Нормальное завершение
            }
        }
    }

    private async Task HandleNotificationAsync(string eventArgsPayload, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received NOTIFY events: {Payload}", eventArgsPayload);

        // Парсим eventId из payload
        if (!int.TryParse(eventArgsPayload, out var eventId))
        {
            _logger.LogWarning("Invalid event ID in notification: {Payload}", eventArgsPayload);
            return;
        }

        // Проверяем, что событие существует и еще не обработано
        // Используем scope для получения DbContext через DI
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiDb>();

        // Загружаем событие с блокировкой (SELECT FOR UPDATE)
        EventEntity? eventModel = await dbContext.Events
            .FromSqlRaw("SELECT * FROM \"Events\" WHERE \"Id\" = {0} FOR UPDATE NOWAIT", eventId)
            .FirstOrDefaultAsync();

        if (eventModel == null)
        {
            _logger.LogWarning("Event {EventId} not found in database", eventId);
            return;
        }

        if (eventModel.Status != EventStatus.Pending)
        {
            _logger.LogInformation("Event {EventId} already processed (Status: {Status})", eventId, eventModel.Status);
            return;
        }

        var evn = new EventClass(dbContext, eventModel, _logger);
        await evn.ProcessAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EventListenerService stopping...");

        if (_connection != null)
        {
            try
            {
                await _connection.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing connection");
            }
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _connection?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
