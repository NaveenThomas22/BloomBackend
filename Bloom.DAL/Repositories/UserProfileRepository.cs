using System.Data;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserProfileRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<UserProfile?> GetByUserIdAsync(ulong userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                id, user_id AS UserId, date_of_birth AS DateOfBirth,
                cat_target_year AS CatTargetYear, cat_exam_date AS CatExamDate,
                preparation_level AS PreparationLevel, preparation_mode AS PreparationMode,
                timezone, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_profiles
            WHERE user_id = @UserId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<UserProfile>(command);
    }

    public async Task<bool> UpdateProfileAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE user_profiles SET 
                date_of_birth = @DateOfBirth,
                cat_target_year = @CatTargetYear,
                cat_exam_date = @CatExamDate,
                preparation_level = @PreparationLevel,
                preparation_mode = @PreparationMode,
                timezone = @Timezone,
                updated_at_utc = @UpdatedAtUtc
            WHERE user_id = @UserId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                profile.UserId,
                profile.DateOfBirth,
                profile.CatTargetYear,
                profile.CatExamDate,
                profile.PreparationLevel,
                profile.PreparationMode,
                profile.Timezone,
                UpdatedAtUtc = DateTime.UtcNow
            },
            cancellationToken: cancellationToken);

        var affected = await connection.ExecuteAsync(command);
        return affected > 0;
    }

    public async Task<UserPreferences?> GetPreferencesByUserIdAsync(ulong userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                id, user_id AS UserId, theme,
                daily_study_target_minutes AS DailyStudyTargetMinutes,
                preferred_wake_time AS PreferredWakeTime,
                preferred_sleep_time AS PreferredSleepTime,
                notification_enabled AS NotificationEnabled,
                created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_preferences
            WHERE user_id = @UserId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<UserPreferences>(command);
    }

    public async Task<bool> UpdatePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE user_preferences SET 
                theme = @Theme,
                daily_study_target_minutes = @DailyStudyTargetMinutes,
                preferred_wake_time = @PreferredWakeTime,
                preferred_sleep_time = @PreferredSleepTime,
                notification_enabled = @NotificationEnabled,
                updated_at_utc = @UpdatedAtUtc
            WHERE user_id = @UserId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                preferences.UserId,
                preferences.Theme,
                preferences.DailyStudyTargetMinutes,
                PreferredWakeTime = preferences.PreferredWakeTime.ToString(@"hh\:mm\:ss"),
                PreferredSleepTime = preferences.PreferredSleepTime.ToString(@"hh\:mm\:ss"),
                NotificationEnabled = preferences.NotificationEnabled ? 1 : 0,
                UpdatedAtUtc = DateTime.UtcNow
            },
            cancellationToken: cancellationToken);

        var affected = await connection.ExecuteAsync(command);
        return affected > 0;
    }
}

