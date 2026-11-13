using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public NotificationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IReadOnlyList<NotificationCategory>> GetCategoriesAsync(CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, category_code AS CategoryCode, category_name AS CategoryName,
                   description, icon, color, default_enabled AS DefaultEnabled,
                   created_at_utc AS CreatedAtUtc
            FROM notification_categories
            ORDER BY id ASC;";

        var result = await connection.QueryAsync<NotificationCategory>(
            new CommandDefinition(sql, cancellationToken: ct));

        return result.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<UserNotificationPreference>> GetPreferencesAsync(ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, category_code AS CategoryCode, is_enabled AS IsEnabled,
                   preferred_time_utc AS PreferredTimeUtc, quiet_hours_start_utc AS QuietHoursStartUtc,
                   quiet_hours_end_utc AS QuietHoursEndUtc, channels, max_daily_alerts AS MaxDailyAlerts,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_notification_preferences
            WHERE user_id = @userId;";

        var result = await connection.QueryAsync<UserNotificationPreference>(
            new CommandDefinition(sql, new { userId }, cancellationToken: ct));

        return result.ToList().AsReadOnly();
    }

    public async Task<UserNotificationPreference?> GetPreferenceAsync(ulong userId, string categoryCode, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, category_code AS CategoryCode, is_enabled AS IsEnabled,
                   preferred_time_utc AS PreferredTimeUtc, quiet_hours_start_utc AS QuietHoursStartUtc,
                   quiet_hours_end_utc AS QuietHoursEndUtc, channels, max_daily_alerts AS MaxDailyAlerts,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_notification_preferences
            WHERE user_id = @userId AND category_code = @categoryCode;";

        return await connection.QuerySingleOrDefaultAsync<UserNotificationPreference>(
            new CommandDefinition(sql, new { userId, categoryCode }, cancellationToken: ct));
    }

    public async Task<bool> UpsertPreferenceAsync(UserNotificationPreference preference, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO user_notification_preferences (user_id, category_code, is_enabled, preferred_time_utc,
                                                      quiet_hours_start_utc, quiet_hours_end_utc, channels,
                                                      max_daily_alerts, created_at_utc, updated_at_utc)
            VALUES (@UserId, @CategoryCode, @IsEnabled, @PreferredTimeUtc,
                    @QuietHoursStartUtc, @QuietHoursEndUtc, @Channels,
                    @MaxDailyAlerts, UTC_TIMESTAMP(), UTC_TIMESTAMP())
            ON DUPLICATE KEY UPDATE
                is_enabled = VALUES(is_enabled),
                preferred_time_utc = VALUES(preferred_time_utc),
                quiet_hours_start_utc = VALUES(quiet_hours_start_utc),
                quiet_hours_end_utc = VALUES(quiet_hours_end_utc),
                channels = VALUES(channels),
                max_daily_alerts = VALUES(max_daily_alerts),
                updated_at_utc = UTC_TIMESTAMP();";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                preference.UserId,
                preference.CategoryCode,
                preference.IsEnabled,
                preference.PreferredTimeUtc,
                preference.QuietHoursStartUtc,
                preference.QuietHoursEndUtc,
                preference.Channels,
                preference.MaxDailyAlerts
            }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<IReadOnlyList<UserNotification>> GetNotificationsAsync(ulong userId, int limit = 50, int offset = 0, string? status = null, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        var sql = @"
            SELECT id, user_id AS UserId, category_code AS CategoryCode, title, message,
                   deep_link AS DeepLink, status, read_at_utc AS ReadAtUtc,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_notifications
            WHERE user_id = @userId " +
            (!string.IsNullOrWhiteSpace(status) ? "AND status = @status " : "") +
            "ORDER BY created_at_utc DESC LIMIT @limit OFFSET @offset;";

        var result = await connection.QueryAsync<UserNotification>(
            new CommandDefinition(sql, new { userId, status, limit, offset }, cancellationToken: ct));

        return result.ToList().AsReadOnly();
    }

    public async Task<int> GetUnreadCountAsync(ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT COUNT(*) FROM user_notifications
            WHERE user_id = @userId AND status = 'Unread';";

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { userId }, cancellationToken: ct));
    }

    public async Task<UserNotification?> GetNotificationByIdAsync(ulong notificationId, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, category_code AS CategoryCode, title, message,
                   deep_link AS DeepLink, status, read_at_utc AS ReadAtUtc,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_notifications
            WHERE id = @notificationId AND user_id = @userId;";

        return await connection.QuerySingleOrDefaultAsync<UserNotification>(
            new CommandDefinition(sql, new { notificationId, userId }, cancellationToken: ct));
    }

    public async Task<ulong> CreateNotificationAsync(UserNotification notification, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO user_notifications (user_id, category_code, title, message, deep_link, status, read_at_utc, created_at_utc, updated_at_utc)
            VALUES (@UserId, @CategoryCode, @Title, @Message, @DeepLink, @Status, @ReadAtUtc, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        return await connection.ExecuteScalarAsync<ulong>(
            new CommandDefinition(sql, new
            {
                notification.UserId,
                notification.CategoryCode,
                notification.Title,
                notification.Message,
                notification.DeepLink,
                notification.Status,
                notification.ReadAtUtc
            }, cancellationToken: ct));
    }

    public async Task<bool> MarkAsReadAsync(ulong notificationId, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE user_notifications
            SET status = 'Read',
                read_at_utc = UTC_TIMESTAMP(),
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @notificationId AND user_id = @userId;";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { notificationId, userId }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<int> MarkAllAsReadAsync(ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE user_notifications
            SET status = 'Read',
                read_at_utc = UTC_TIMESTAMP(),
                updated_at_utc = UTC_TIMESTAMP()
            WHERE user_id = @userId AND status = 'Unread';";

        return await connection.ExecuteAsync(
            new CommandDefinition(sql, new { userId }, cancellationToken: ct));
    }

    public async Task<bool> DismissNotificationAsync(ulong notificationId, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE user_notifications
            SET status = 'Dismissed',
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @notificationId AND user_id = @userId;";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { notificationId, userId }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<bool> UpsertSubscriptionAsync(NotificationSubscription subscription, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO notification_subscriptions (user_id, endpoint, p256dh, auth, user_agent, is_active, created_at_utc, updated_at_utc)
            VALUES (@UserId, @Endpoint, @P256dh, @Auth, @UserAgent, @IsActive, UTC_TIMESTAMP(), UTC_TIMESTAMP())
            ON DUPLICATE KEY UPDATE
                p256dh = VALUES(p256dh),
                auth = VALUES(auth),
                user_agent = VALUES(user_agent),
                is_active = VALUES(is_active),
                updated_at_utc = UTC_TIMESTAMP();";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                subscription.UserId,
                subscription.Endpoint,
                subscription.P256dh,
                subscription.Auth,
                subscription.UserAgent,
                subscription.IsActive
            }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<bool> DeactivateSubscriptionAsync(ulong userId, string endpoint, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE notification_subscriptions
            SET is_active = 0,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE user_id = @userId AND endpoint = @endpoint;";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { userId, endpoint }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<IReadOnlyList<NotificationSubscription>> GetActiveSubscriptionsAsync(ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, endpoint, p256dh, auth, user_agent AS UserAgent,
                   is_active AS IsActive, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM notification_subscriptions
            WHERE user_id = @userId AND is_active = 1;";

        var result = await connection.QueryAsync<NotificationSubscription>(
            new CommandDefinition(sql, new { userId }, cancellationToken: ct));

        return result.ToList().AsReadOnly();
    }

    public async Task<ulong> RecordDeliveryAttemptAsync(NotificationDeliveryAttempt attempt, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO notification_delivery_attempts (notification_id, channel, status, error_message, attempted_at_utc)
            VALUES (@NotificationId, @Channel, @Status, @ErrorMessage, UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        return await connection.ExecuteScalarAsync<ulong>(
            new CommandDefinition(sql, new
            {
                attempt.NotificationId,
                attempt.Channel,
                attempt.Status,
                attempt.ErrorMessage
            }, cancellationToken: ct));
    }

    public async Task<int> GetDailyAlertCountAsync(ulong userId, string categoryCode, DateOnly dateUtc, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT COUNT(*) 
            FROM user_notifications
            WHERE user_id = @userId AND category_code = @categoryCode
              AND created_at_utc >= @startDate AND created_at_utc < @endDate;";

        var startDate = dateUtc.ToDateTime(TimeOnly.MinValue);
        var endDate = dateUtc.AddDays(1).ToDateTime(TimeOnly.MinValue);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                userId,
                categoryCode,
                startDate,
                endDate
            }, cancellationToken: ct));
    }

    public async Task<SmartNotificationTriggersData> GetSmartTriggerMetricsAsync(ulong userId, DateOnly localDate, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string dueRevisionsSql = @"
            SELECT COUNT(*) FROM revision_schedules
            WHERE user_id = @userId AND is_active = 1 AND scheduled_date <= @date;";

        const string morningCheckSql = @"
            SELECT COUNT(*) FROM daily_reflections
            WHERE user_id = @userId AND reflection_date = @date AND morning_completed_at_utc IS NOT NULL;";

        const string eveningCheckSql = @"
            SELECT COUNT(*) FROM daily_reflections
            WHERE user_id = @userId AND reflection_date = @date AND evening_completed_at_utc IS NOT NULL;";

        const string habitCheckSql = @"
            SELECT COUNT(*) FROM user_habits h
            WHERE h.user_id = @userId AND h.is_active = 1 AND h.deleted_at_utc IS NULL
              AND NOT EXISTS (
                  SELECT 1 FROM habit_completions c
                  WHERE c.user_id = @userId AND c.habit_id = h.id AND c.completion_date = @date
              );";

        var dateParam = localDate.ToDateTime(TimeOnly.MinValue);

        var dueCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(dueRevisionsSql, new { userId, date = dateParam }, cancellationToken: ct));

        var morningCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(morningCheckSql, new { userId, date = dateParam }, cancellationToken: ct));

        var eveningCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(eveningCheckSql, new { userId, date = dateParam }, cancellationToken: ct));

        var pendingHabits = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(habitCheckSql, new { userId, date = dateParam }, cancellationToken: ct));

        return new SmartNotificationTriggersData
        {
            DueRevisionsCount = dueCount,
            MorningReflectionLogged = morningCount > 0,
            EveningReflectionLogged = eveningCount > 0,
            PendingHabitsCount = pendingHabits
        };
    }
}
