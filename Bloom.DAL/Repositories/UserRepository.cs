using System.Data;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<User?> GetByIdAsync(ulong id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                u.id, u.full_name AS FullName, u.email, u.normalized_email AS NormalizedEmail,
                u.password_hash AS PasswordHash, u.profile_picture_url AS ProfilePictureUrl,
                u.is_active AS IsActive, u.email_verified AS EmailVerified, u.role AS Role,
                u.created_at_utc AS CreatedAtUtc, u.updated_at_utc AS UpdatedAtUtc,
                u.last_login_at_utc AS LastLoginAtUtc,
                p.id, p.user_id AS UserId, p.date_of_birth AS DateOfBirth,
                p.cat_target_year AS CatTargetYear, p.cat_exam_date AS CatExamDate,
                p.preparation_level AS PreparationLevel, p.preparation_mode AS PreparationMode,
                p.timezone, p.created_at_utc AS CreatedAtUtc, p.updated_at_utc AS UpdatedAtUtc,
                pref.id, pref.user_id AS UserId, pref.theme,
                pref.daily_study_target_minutes AS DailyStudyTargetMinutes,
                pref.preferred_wake_time AS PreferredWakeTime,
                pref.preferred_sleep_time AS PreferredSleepTime,
                pref.notification_enabled AS NotificationEnabled,
                pref.created_at_utc AS CreatedAtUtc, pref.updated_at_utc AS UpdatedAtUtc
            FROM users u
            LEFT JOIN user_profiles p ON u.id = p.user_id
            LEFT JOIN user_preferences pref ON u.id = pref.user_id
            WHERE u.id = @Id AND u.is_active = 1;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);

        var users = await connection.QueryAsync<User, UserProfile, UserPreferences, User>(
            command,
            (user, profile, preferences) =>
            {
                user.Profile = profile;
                user.Preferences = preferences;
                return user;
            },
            splitOn: "id,id");

        return users.FirstOrDefault();
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return await GetByNormalizedEmailAsync(normalized, cancellationToken);
    }

    public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                id, full_name AS FullName, email, normalized_email AS NormalizedEmail,
                password_hash AS PasswordHash, profile_picture_url AS ProfilePictureUrl,
                is_active AS IsActive, email_verified AS EmailVerified, role AS Role,
                created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc,
                last_login_at_utc AS LastLoginAtUtc
            FROM users
            WHERE normalized_email = @NormalizedEmail;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { NormalizedEmail = normalizedEmail }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<User>(command);
    }

    public async Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM users WHERE normalized_email = @NormalizedEmail;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { NormalizedEmail = normalizedEmail }, cancellationToken: cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(command);
        return count > 0;
    }

    public async Task<ulong> CreateAsync(
        User user, 
        UserProfile profile, 
        UserPreferences preferences, 
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            const string insertUserSql = @"
                INSERT INTO users (
                    full_name, email, normalized_email, password_hash,
                    profile_picture_url, is_active, email_verified, role,
                    created_at_utc, updated_at_utc
                ) VALUES (
                    @FullName, @Email, @NormalizedEmail, @PasswordHash,
                    @ProfilePictureUrl, @IsActive, @EmailVerified, @Role,
                    @CreatedAtUtc, @UpdatedAtUtc
                );
                SELECT LAST_INSERT_ID();";

            var userCommand = new CommandDefinition(
                insertUserSql,
                new
                {
                    user.FullName,
                    user.Email,
                    user.NormalizedEmail,
                    user.PasswordHash,
                    user.ProfilePictureUrl,
                    IsActive = user.IsActive ? 1 : 0,
                    EmailVerified = user.EmailVerified ? 1 : 0,
                    Role = string.IsNullOrWhiteSpace(user.Role) ? "Aspirant" : user.Role,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            var userId = await connection.ExecuteScalarAsync<ulong>(userCommand);
            user.Id = userId;
            profile.UserId = userId;
            preferences.UserId = userId;

            const string insertProfileSql = @"
                INSERT INTO user_profiles (
                    user_id, date_of_birth, cat_target_year, cat_exam_date,
                    preparation_level, preparation_mode, timezone,
                    created_at_utc, updated_at_utc
                ) VALUES (
                    @UserId, @DateOfBirth, @CatTargetYear, @CatExamDate,
                    @PreparationLevel, @PreparationMode, @Timezone,
                    @CreatedAtUtc, @UpdatedAtUtc
                );";

            var profileCommand = new CommandDefinition(
                insertProfileSql,
                new
                {
                    UserId = userId,
                    profile.DateOfBirth,
                    profile.CatTargetYear,
                    profile.CatExamDate,
                    profile.PreparationLevel,
                    profile.PreparationMode,
                    profile.Timezone,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(profileCommand);

            const string insertPrefSql = @"
                INSERT INTO user_preferences (
                    user_id, theme, daily_study_target_minutes,
                    preferred_wake_time, preferred_sleep_time, notification_enabled,
                    created_at_utc, updated_at_utc
                ) VALUES (
                    @UserId, @Theme, @DailyStudyTargetMinutes,
                    @PreferredWakeTime, @PreferredSleepTime, @NotificationEnabled,
                    @CreatedAtUtc, @UpdatedAtUtc
                );";

            var prefCommand = new CommandDefinition(
                insertPrefSql,
                new
                {
                    UserId = userId,
                    preferences.Theme,
                    preferences.DailyStudyTargetMinutes,
                    PreferredWakeTime = preferences.PreferredWakeTime.ToString(@"hh\:mm\:ss"),
                    PreferredSleepTime = preferences.PreferredSleepTime.ToString(@"hh\:mm\:ss"),
                    NotificationEnabled = preferences.NotificationEnabled ? 1 : 0,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(prefCommand);

            transaction.Commit();
            return userId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE users SET 
                full_name = @FullName,
                profile_picture_url = @ProfilePictureUrl,
                updated_at_utc = @UpdatedAtUtc
            WHERE id = @Id;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                user.Id,
                user.FullName,
                user.ProfilePictureUrl,
                UpdatedAtUtc = DateTime.UtcNow
            },
            cancellationToken: cancellationToken);

        var affected = await connection.ExecuteAsync(command);
        return affected > 0;
    }

    public async Task<bool> UpdateLastLoginAsync(ulong userId, DateTime lastLoginUtc, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE users SET last_login_at_utc = @LastLoginUtc WHERE id = @UserId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { UserId = userId, LastLoginUtc = lastLoginUtc }, cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(command);
        return affected > 0;
    }
}

