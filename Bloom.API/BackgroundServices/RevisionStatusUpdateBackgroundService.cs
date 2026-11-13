using System.Data;
using System.Data.Common;
using Bloom.Core.Interfaces;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bloom.API.BackgroundServices;

public class RevisionStatusUpdateBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RevisionStatusUpdateBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public RevisionStatusUpdateBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<RevisionStatusUpdateBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RevisionStatusUpdateBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var scheduleService = scope.ServiceProvider.GetRequiredService<Bloom.BLL.Interfaces.IRevisionScheduleService>();
                
                // Reconcile any completed topics that might be missing initial revision schedules (self-healing durable recovery)
                var reconciledCount = await scheduleService.ReconcileMissingInitialSchedulesAsync(null, stoppingToken);
                if (reconciledCount > 0)
                {
                    _logger.LogInformation("Reconciled {Count} missing revision schedules across users.", reconciledCount);
                }

                var connFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
                using var connection = connFactory.CreateConnection();
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                // 1. Identify distinct active users and their timezones
                const string activeUsersSql = @"
                    SELECT DISTINCT s.user_id AS UserId, COALESCE(p.timezone, 'Asia/Kolkata') AS Timezone
                    FROM revision_schedules s
                    LEFT JOIN user_profiles p ON s.user_id = p.user_id
                    WHERE s.is_deleted = 0 AND s.status IN ('Upcoming', 'DueToday');
                ";

                var activeUsers = (await connection.QueryAsync<UserTimezoneRow>(
                    new CommandDefinition(activeUsersSql, cancellationToken: stoppingToken))).ToList();

                if (activeUsers.Count > 0)
                {
                    // 2. Group users by local calendar date calculated in their authoritative timezone
                    var nowUtc = DateTime.UtcNow;
                    var usersByToday = new Dictionary<DateOnly, List<ulong>>();

                    foreach (var u in activeUsers)
                    {
                        TimeZoneInfo tz;
                        try
                        {
                            tz = TimeZoneInfo.FindSystemTimeZoneById(u.Timezone);
                        }
                        catch
                        {
                            try { tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
                            catch { tz = TimeZoneInfo.Utc; }
                        }

                        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz));
                        if (!usersByToday.TryGetValue(localToday, out var list))
                        {
                            list = new List<ulong>();
                            usersByToday[localToday] = list;
                        }
                        list.Add(u.UserId);
                    }

                    // 3. Update status batch per distinct local calendar date
                    int totalUpdated = 0;
                    foreach (var (todayDate, userIds) in usersByToday)
                    {
                        const string updateSql = @"
                            UPDATE revision_schedules
                            SET status = 'DueToday', updated_at_utc = UTC_TIMESTAMP()
                            WHERE is_deleted = 0 AND user_id IN @UserIds AND scheduled_date = @Today AND status = 'Upcoming';

                            UPDATE revision_schedules
                            SET status = 'Overdue', updated_at_utc = UTC_TIMESTAMP()
                            WHERE is_deleted = 0 AND user_id IN @UserIds AND scheduled_date < @Today AND status IN ('Upcoming', 'DueToday');
                        ";

                        var affected = await connection.ExecuteAsync(new CommandDefinition(
                            updateSql,
                            new { UserIds = userIds, Today = todayDate.ToString("yyyy-MM-dd") },
                            cancellationToken: stoppingToken));

                        totalUpdated += affected;
                    }

                    if (totalUpdated > 0)
                    {
                        _logger.LogInformation("Revision status background update updated {Count} schedules across {UserCount} active users.",
                            totalUpdated, activeUsers.Count);
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Error occurred during background revision status update: {Message}", ex.Message);
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("RevisionStatusUpdateBackgroundService stopped.");
    }

    private class UserTimezoneRow
    {
        public ulong UserId { get; set; }
        public string Timezone { get; set; } = "Asia/Kolkata";
    }
}
