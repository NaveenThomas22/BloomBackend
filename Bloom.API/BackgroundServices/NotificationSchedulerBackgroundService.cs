using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bloom.API.BackgroundServices;

public class NotificationSchedulerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationSchedulerBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);

    public NotificationSchedulerBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NotificationSchedulerBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationSchedulerBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var milestoneService = scope.ServiceProvider.GetRequiredService<IPersonalMilestoneService>();
                var connFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

                using var connection = await connFactory.CreateOpenConnectionAsync(stoppingToken);

             const string usersSql = @"
    SELECT
        u.id AS UserId,
        COALESCE(NULLIF(TRIM(p.timezone), ''), 'Asia/Kolkata') AS Timezone
    FROM users u
    LEFT JOIN user_profiles p ON u.id = p.user_id
    WHERE u.is_active = 1;";
                var activeUsers = (await connection.QueryAsync<(ulong UserId, string Timezone)>(
                    new CommandDefinition(usersSql, cancellationToken: stoppingToken))).ToList();

                var nowUtc = DateTime.UtcNow;

                foreach (var user in activeUsers)
                {
                    try
                    {
                        var tzInfo = TimeZoneInfo.Utc;
                        try
                        {
                            tzInfo = TimeZoneInfo.FindSystemTimeZoneById(user.Timezone);
                        }
                        catch
                        {
                            // Try standard fallback
                            if (user.Timezone.Contains("Kolkata", StringComparison.OrdinalIgnoreCase))
                            {
                                tzInfo = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                            }
                        }

                        var localTime = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tzInfo);
                        var localDate = DateOnly.FromDateTime(localTime);
                        var timeOfDay = localTime.TimeOfDay;

                        // Trigger notifications
                        await notificationService.TriggerSmartNotificationsAsync(user.UserId, localDate, timeOfDay, stoppingToken);

                        // Sync milestone progress
                        await milestoneService.SyncMilestoneProgressAsync(user.UserId, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing notifications for user {UserId}", user.UserId);
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unexpected error in NotificationSchedulerBackgroundService execution loop.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("NotificationSchedulerBackgroundService stopped.");
    }
}

