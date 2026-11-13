using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class ReflectionRepository : IReflectionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ReflectionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IReadOnlyList<ReflectionMood>> GetActiveMoodsAsync(CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, mood_code AS MoodCode, mood_name AS MoodName, emoji, color, description,
                   display_order AS DisplayOrder, is_active AS IsActive, created_at_utc AS CreatedAtUtc
            FROM reflection_moods
            WHERE is_active = 1
            ORDER BY display_order ASC, mood_name ASC;";

        var results = await connection.QueryAsync<ReflectionMood>(
            new CommandDefinition(sql, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<ReflectionMood?> GetMoodByIdAsync(ulong id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, mood_code AS MoodCode, mood_name AS MoodName, emoji, color, description,
                   display_order AS DisplayOrder, is_active AS IsActive, created_at_utc AS CreatedAtUtc
            FROM reflection_moods
            WHERE id = @id;";

        return await connection.QuerySingleOrDefaultAsync<ReflectionMood>(
            new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }

    public async Task<DailyReflection?> GetByDateAsync(ulong userId, DateOnly reflectionDate, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT r.id, r.user_id AS UserId, r.reflection_date AS ReflectionDate,
                   r.morning_mood_id AS MorningMoodId, r.morning_mood_intensity AS MorningMoodIntensity,
                   r.morning_energy_level AS MorningEnergyLevel, r.today_intention AS TodayIntention,
                   r.main_priority AS MainPriority, r.task_one AS TaskOne, r.task_two AS TaskTwo,
                   r.task_three AS TaskThree, r.study_target_minutes AS StudyTargetMinutes,
                   r.personal_commitment AS PersonalCommitment, r.morning_notes AS MorningNotes,
                   r.morning_completed_at_utc AS MorningCompletedAtUtc,
                   r.evening_mood_id AS EveningMoodId, r.evening_mood_intensity AS EveningMoodIntensity,
                   r.how_was_today AS HowWasToday, r.what_accomplished AS WhatAccomplished,
                   r.what_went_well AS WhatWentWell, r.could_have_been_better AS CouldHaveBeenBetter,
                   r.what_learned_today AS WhatLearnedToday, r.what_challenged_me AS WhatChallengedMe,
                   r.satisfaction_rating AS SatisfactionRating, r.improve_tomorrow AS ImproveTomorrow,
                   r.gratitude_entry AS GratitudeEntry, r.evening_completed_at_utc AS EveningCompletedAtUtc,
                   r.created_at_utc AS CreatedAtUtc, r.updated_at_utc AS UpdatedAtUtc,
                   mm.mood_name AS MorningMoodName, mm.emoji AS MorningMoodEmoji, mm.color AS MorningMoodColor,
                   em.mood_name AS EveningMoodName, em.emoji AS EveningMoodEmoji, em.color AS EveningMoodColor
            FROM daily_reflections r
            LEFT JOIN reflection_moods mm ON r.morning_mood_id = mm.id
            LEFT JOIN reflection_moods em ON r.evening_mood_id = em.id
            WHERE r.user_id = @userId AND r.reflection_date = @reflectionDate;";

        return await connection.QuerySingleOrDefaultAsync<DailyReflection>(
            new CommandDefinition(sql, new
            {
                userId,
                reflectionDate = reflectionDate.ToDateTime(TimeOnly.MinValue)
            }, cancellationToken: ct));
    }

    public async Task<ulong> UpsertMorningReflectionAsync(DailyReflection reflection, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO daily_reflections (
                user_id, reflection_date, morning_mood_id, morning_mood_intensity,
                morning_energy_level, today_intention, main_priority, task_one,
                task_two, task_three, study_target_minutes, personal_commitment,
                morning_notes, morning_completed_at_utc, created_at_utc, updated_at_utc
            )
            VALUES (
                @UserId, @ReflectionDate, @MorningMoodId, @MorningMoodIntensity,
                @MorningEnergyLevel, @TodayIntention, @MainPriority, @TaskOne,
                @TaskTwo, @TaskThree, @StudyTargetMinutes, @PersonalCommitment,
                @MorningNotes, @MorningCompletedAtUtc, UTC_TIMESTAMP(), UTC_TIMESTAMP()
            )
            ON DUPLICATE KEY UPDATE
                morning_mood_id = VALUES(morning_mood_id),
                morning_mood_intensity = VALUES(morning_mood_intensity),
                morning_energy_level = VALUES(morning_energy_level),
                today_intention = VALUES(today_intention),
                main_priority = VALUES(main_priority),
                task_one = VALUES(task_one),
                task_two = VALUES(task_two),
                task_three = VALUES(task_three),
                study_target_minutes = VALUES(study_target_minutes),
                personal_commitment = VALUES(personal_commitment),
                morning_notes = VALUES(morning_notes),
                morning_completed_at_utc = VALUES(morning_completed_at_utc),
                updated_at_utc = UTC_TIMESTAMP();

            SELECT id FROM daily_reflections WHERE user_id = @UserId AND reflection_date = @ReflectionDate;";

        return await connection.ExecuteScalarAsync<ulong>(
            new CommandDefinition(sql, new
            {
                reflection.UserId,
                ReflectionDate = reflection.ReflectionDate.ToDateTime(TimeOnly.MinValue),
                reflection.MorningMoodId,
                reflection.MorningMoodIntensity,
                reflection.MorningEnergyLevel,
                reflection.TodayIntention,
                reflection.MainPriority,
                reflection.TaskOne,
                reflection.TaskTwo,
                reflection.TaskThree,
                reflection.StudyTargetMinutes,
                reflection.PersonalCommitment,
                reflection.MorningNotes,
                reflection.MorningCompletedAtUtc
            }, cancellationToken: ct));
    }

    public async Task<ulong> UpsertEveningReflectionAsync(DailyReflection reflection, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO daily_reflections (
                user_id, reflection_date, evening_mood_id, evening_mood_intensity,
                how_was_today, what_accomplished, what_went_well, could_have_been_better,
                what_learned_today, what_challenged_me, satisfaction_rating,
                improve_tomorrow, gratitude_entry, evening_completed_at_utc,
                created_at_utc, updated_at_utc
            )
            VALUES (
                @UserId, @ReflectionDate, @EveningMoodId, @EveningMoodIntensity,
                @HowWasToday, @WhatAccomplished, @WhatWentWell, @CouldHaveBeenBetter,
                @WhatLearnedToday, @WhatChallengedMe, @SatisfactionRating,
                @ImproveTomorrow, @GratitudeEntry, @EveningCompletedAtUtc,
                UTC_TIMESTAMP(), UTC_TIMESTAMP()
            )
            ON DUPLICATE KEY UPDATE
                evening_mood_id = VALUES(evening_mood_id),
                evening_mood_intensity = VALUES(evening_mood_intensity),
                how_was_today = VALUES(how_was_today),
                what_accomplished = VALUES(what_accomplished),
                what_went_well = VALUES(what_went_well),
                could_have_been_better = VALUES(could_have_been_better),
                what_learned_today = VALUES(what_learned_today),
                what_challenged_me = VALUES(what_challenged_me),
                satisfaction_rating = VALUES(satisfaction_rating),
                improve_tomorrow = VALUES(improve_tomorrow),
                gratitude_entry = VALUES(gratitude_entry),
                evening_completed_at_utc = VALUES(evening_completed_at_utc),
                updated_at_utc = UTC_TIMESTAMP();

            SELECT id FROM daily_reflections WHERE user_id = @UserId AND reflection_date = @ReflectionDate;";

        return await connection.ExecuteScalarAsync<ulong>(
            new CommandDefinition(sql, new
            {
                reflection.UserId,
                ReflectionDate = reflection.ReflectionDate.ToDateTime(TimeOnly.MinValue),
                reflection.EveningMoodId,
                reflection.EveningMoodIntensity,
                reflection.HowWasToday,
                reflection.WhatAccomplished,
                reflection.WhatWentWell,
                reflection.CouldHaveBeenBetter,
                reflection.WhatLearnedToday,
                reflection.WhatChallengedMe,
                reflection.SatisfactionRating,
                reflection.ImproveTomorrow,
                reflection.GratitudeEntry,
                reflection.EveningCompletedAtUtc
            }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<DailyReflection>> GetRangeAsync(
        ulong userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT r.id, r.user_id AS UserId, r.reflection_date AS ReflectionDate,
                   r.morning_mood_id AS MorningMoodId, r.morning_mood_intensity AS MorningMoodIntensity,
                   r.morning_energy_level AS MorningEnergyLevel, r.today_intention AS TodayIntention,
                   r.main_priority AS MainPriority, r.task_one AS TaskOne, r.task_two AS TaskTwo,
                   r.task_three AS TaskThree, r.study_target_minutes AS StudyTargetMinutes,
                   r.personal_commitment AS PersonalCommitment, r.morning_notes AS MorningNotes,
                   r.morning_completed_at_utc AS MorningCompletedAtUtc,
                   r.evening_mood_id AS EveningMoodId, r.evening_mood_intensity AS EveningMoodIntensity,
                   r.how_was_today AS HowWasToday, r.what_accomplished AS WhatAccomplished,
                   r.what_went_well AS WhatWentWell, r.could_have_been_better AS CouldHaveBeenBetter,
                   r.what_learned_today AS WhatLearnedToday, r.what_challenged_me AS WhatChallengedMe,
                   r.satisfaction_rating AS SatisfactionRating, r.improve_tomorrow AS ImproveTomorrow,
                   r.gratitude_entry AS GratitudeEntry, r.evening_completed_at_utc AS EveningCompletedAtUtc,
                   r.created_at_utc AS CreatedAtUtc, r.updated_at_utc AS UpdatedAtUtc,
                   mm.mood_name AS MorningMoodName, mm.emoji AS MorningMoodEmoji, mm.color AS MorningMoodColor,
                   em.mood_name AS EveningMoodName, em.emoji AS EveningMoodEmoji, em.color AS EveningMoodColor
            FROM daily_reflections r
            LEFT JOIN reflection_moods mm ON r.morning_mood_id = mm.id
            LEFT JOIN reflection_moods em ON r.evening_mood_id = em.id
            WHERE r.user_id = @userId
              AND r.reflection_date >= @startDate
              AND r.reflection_date <= @endDate
            ORDER BY r.reflection_date ASC;";

        var results = await connection.QueryAsync<DailyReflection>(
            new CommandDefinition(sql, new
            {
                userId,
                startDate = startDate.ToDateTime(TimeOnly.MinValue),
                endDate = endDate.ToDateTime(TimeOnly.MinValue)
            }, cancellationToken: ct));

        return results.AsList();
    }
}

