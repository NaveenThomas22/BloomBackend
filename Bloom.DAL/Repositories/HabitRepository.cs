using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class HabitRepository : IHabitRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public HabitRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IReadOnlyList<HabitCategory>> GetCategoriesAsync(ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, category_code AS CategoryCode, category_name AS CategoryName,
                   description, icon, color, display_order AS DisplayOrder, is_active AS IsActive,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM habit_categories
            WHERE (user_id IS NULL OR user_id = @userId) AND is_active = 1
            ORDER BY display_order ASC, id ASC;";

        var result = await connection.QueryAsync<HabitCategory>(
            new CommandDefinition(sql, new { userId }, cancellationToken: ct));
        return result.ToList().AsReadOnly();
    }

    public async Task<HabitCategory?> GetCategoryByIdAsync(ulong categoryId, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, category_code AS CategoryCode, category_name AS CategoryName,
                   description, icon, color, display_order AS DisplayOrder, is_active AS IsActive,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM habit_categories
            WHERE id = @categoryId AND (user_id IS NULL OR user_id = @userId);";

        return await connection.QuerySingleOrDefaultAsync<HabitCategory>(
            new CommandDefinition(sql, new { categoryId, userId }, cancellationToken: ct));
    }

    public async Task<ulong> CreateCategoryAsync(HabitCategory category, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO habit_categories (user_id, category_code, category_name, description, icon, color, display_order, is_active, created_at_utc, updated_at_utc)
            VALUES (@UserId, @CategoryCode, @CategoryName, @Description, @Icon, @Color, @DisplayOrder, @IsActive, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        return await connection.ExecuteScalarAsync<ulong>(
            new CommandDefinition(sql, new
            {
                category.UserId,
                category.CategoryCode,
                category.CategoryName,
                category.Description,
                category.Icon,
                category.Color,
                category.DisplayOrder,
                category.IsActive
            }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<UserHabit>> GetHabitsAsync(ulong userId, bool activeOnly = true, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        var sql = @"
            SELECT h.id, h.user_id AS UserId, h.category_id AS CategoryId, h.name, h.description,
                   h.frequency, h.target_frequency_days AS TargetFrequencyDays, h.target_value AS TargetValue,
                   h.target_unit AS TargetUnit, h.start_date AS StartDate, h.end_date AS EndDate,
                   h.is_active AS IsActive, h.display_order AS DisplayOrder,
                   h.created_at_utc AS CreatedAtUtc, h.updated_at_utc AS UpdatedAtUtc, h.deleted_at_utc AS DeletedAtUtc,
                   c.category_name AS CategoryName, c.category_code AS CategoryCode, c.icon AS CategoryIcon, c.color AS CategoryColor
            FROM user_habits h
            INNER JOIN habit_categories c ON h.category_id = c.id
            WHERE h.user_id = @userId AND h.deleted_at_utc IS NULL " +
            (activeOnly ? "AND h.is_active = 1 " : "") +
            "ORDER BY h.display_order ASC, h.id ASC;";

        var result = await connection.QueryAsync<UserHabit>(
            new CommandDefinition(sql, new { userId }, cancellationToken: ct));
        return result.ToList().AsReadOnly();
    }

    public async Task<UserHabit?> GetHabitByIdAsync(ulong habitId, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT h.id, h.user_id AS UserId, h.category_id AS CategoryId, h.name, h.description,
                   h.frequency, h.target_frequency_days AS TargetFrequencyDays, h.target_value AS TargetValue,
                   h.target_unit AS TargetUnit, h.start_date AS StartDate, h.end_date AS EndDate,
                   h.is_active AS IsActive, h.display_order AS DisplayOrder,
                   h.created_at_utc AS CreatedAtUtc, h.updated_at_utc AS UpdatedAtUtc, h.deleted_at_utc AS DeletedAtUtc,
                   c.category_name AS CategoryName, c.category_code AS CategoryCode, c.icon AS CategoryIcon, c.color AS CategoryColor
            FROM user_habits h
            INNER JOIN habit_categories c ON h.category_id = c.id
            WHERE h.id = @habitId AND h.user_id = @userId AND h.deleted_at_utc IS NULL;";

        return await connection.QuerySingleOrDefaultAsync<UserHabit>(
            new CommandDefinition(sql, new { habitId, userId }, cancellationToken: ct));
    }

    public async Task<ulong> CreateHabitAsync(UserHabit habit, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO user_habits (user_id, category_id, name, description, frequency, target_frequency_days,
                                     target_value, target_unit, start_date, end_date, is_active, display_order,
                                     created_at_utc, updated_at_utc)
            VALUES (@UserId, @CategoryId, @Name, @Description, @Frequency, @TargetFrequencyDays,
                    @TargetValue, @TargetUnit, @StartDate, @EndDate, @IsActive, @DisplayOrder,
                    UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        return await connection.ExecuteScalarAsync<ulong>(
            new CommandDefinition(sql, new
            {
                habit.UserId,
                habit.CategoryId,
                habit.Name,
                habit.Description,
                habit.Frequency,
                habit.TargetFrequencyDays,
                habit.TargetValue,
                habit.TargetUnit,
                StartDate = habit.StartDate.ToDateTime(TimeOnly.MinValue),
                EndDate = habit.EndDate.HasValue ? habit.EndDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                habit.IsActive,
                habit.DisplayOrder
            }, cancellationToken: ct));
    }

    public async Task<bool> UpdateHabitAsync(UserHabit habit, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE user_habits
            SET category_id = @CategoryId,
                name = @Name,
                description = @Description,
                frequency = @Frequency,
                target_frequency_days = @TargetFrequencyDays,
                target_value = @TargetValue,
                target_unit = @TargetUnit,
                start_date = @StartDate,
                end_date = @EndDate,
                is_active = @IsActive,
                display_order = @DisplayOrder,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id AND user_id = @UserId AND deleted_at_utc IS NULL;";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                habit.Id,
                habit.UserId,
                habit.CategoryId,
                habit.Name,
                habit.Description,
                habit.Frequency,
                habit.TargetFrequencyDays,
                habit.TargetValue,
                habit.TargetUnit,
                StartDate = habit.StartDate.ToDateTime(TimeOnly.MinValue),
                EndDate = habit.EndDate.HasValue ? habit.EndDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                habit.IsActive,
                habit.DisplayOrder
            }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<bool> DeleteHabitAsync(ulong habitId, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE user_habits
            SET deleted_at_utc = UTC_TIMESTAMP()
            WHERE id = @habitId AND user_id = @userId AND deleted_at_utc IS NULL;";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { habitId, userId }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<IReadOnlyList<HabitCompletion>> GetCompletionsForDateAsync(ulong userId, DateOnly date, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, habit_id AS HabitId, completion_date AS CompletionDate,
                   status, actual_value AS ActualValue, skip_reason AS SkipReason, notes,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM habit_completions
            WHERE user_id = @userId AND completion_date = @date;";

        var result = await connection.QueryAsync<HabitCompletion>(
            new CommandDefinition(sql, new
            {
                userId,
                date = date.ToDateTime(TimeOnly.MinValue)
            }, cancellationToken: ct));

        return result.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<HabitCompletion>> GetCompletionsForRangeAsync(ulong userId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, habit_id AS HabitId, completion_date AS CompletionDate,
                   status, actual_value AS ActualValue, skip_reason AS SkipReason, notes,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM habit_completions
            WHERE user_id = @userId AND completion_date BETWEEN @startDate AND @endDate
            ORDER BY completion_date ASC, habit_id ASC;";

        var result = await connection.QueryAsync<HabitCompletion>(
            new CommandDefinition(sql, new
            {
                userId,
                startDate = startDate.ToDateTime(TimeOnly.MinValue),
                endDate = endDate.ToDateTime(TimeOnly.MinValue)
            }, cancellationToken: ct));

        return result.ToList().AsReadOnly();
    }

    public async Task<HabitCompletion?> GetCompletionAsync(ulong userId, ulong habitId, DateOnly date, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, habit_id AS HabitId, completion_date AS CompletionDate,
                   status, actual_value AS ActualValue, skip_reason AS SkipReason, notes,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM habit_completions
            WHERE user_id = @userId AND habit_id = @habitId AND completion_date = @date;";

        return await connection.QuerySingleOrDefaultAsync<HabitCompletion>(
            new CommandDefinition(sql, new
            {
                userId,
                habitId,
                date = date.ToDateTime(TimeOnly.MinValue)
            }, cancellationToken: ct));
    }

    public async Task<bool> RecordCompletionAsync(HabitCompletion completion, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO habit_completions (user_id, habit_id, completion_date, status, actual_value, skip_reason, notes, created_at_utc, updated_at_utc)
            VALUES (@UserId, @HabitId, @CompletionDate, @Status, @ActualValue, @SkipReason, @Notes, UTC_TIMESTAMP(), UTC_TIMESTAMP())
            ON DUPLICATE KEY UPDATE
                status = VALUES(status),
                actual_value = VALUES(actual_value),
                skip_reason = VALUES(skip_reason),
                notes = VALUES(notes),
                updated_at_utc = UTC_TIMESTAMP();";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                completion.UserId,
                completion.HabitId,
                CompletionDate = completion.CompletionDate.ToDateTime(TimeOnly.MinValue),
                completion.Status,
                completion.ActualValue,
                completion.SkipReason,
                completion.Notes
            }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<bool> DeleteCompletionAsync(ulong userId, ulong habitId, DateOnly date, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            DELETE FROM habit_completions
            WHERE user_id = @userId AND habit_id = @habitId AND completion_date = @date;";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                userId,
                habitId,
                date = date.ToDateTime(TimeOnly.MinValue)
            }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<IReadOnlyList<HabitStreakInfo>> GetStreaksAsync(ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string habitsSql = @"
            SELECT id, name, frequency, target_frequency_days AS TargetFrequencyDays, start_date AS StartDate
            FROM user_habits 
            WHERE user_id = @userId AND is_active = 1 AND deleted_at_utc IS NULL;";

        var habits = (await connection.QueryAsync<(ulong Id, string Name, string Frequency, uint TargetFrequencyDays, DateTime StartDate)>(
            new CommandDefinition(habitsSql, new { userId }, cancellationToken: ct))).ToList();

        if (habits.Count == 0)
        {
            return Array.Empty<HabitStreakInfo>();
        }

        const string completionsSql = @"
            SELECT habit_id AS HabitId, completion_date AS CompletionDate, status
            FROM habit_completions
            WHERE user_id = @userId
            ORDER BY completion_date DESC;";

        var allCompletions = (await connection.QueryAsync<(ulong HabitId, DateTime CompletionDate, string Status)>(
            new CommandDefinition(completionsSql, new { userId }, cancellationToken: ct))).ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDaysAgo = today.AddDays(-29);

        var streakList = new List<HabitStreakInfo>();

        foreach (var habit in habits)
        {
            var habitComps = allCompletions.Where(c => c.HabitId == habit.Id).ToList();

            var completedDates = habitComps
                .Where(c => string.Equals(c.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                .Select(c => DateOnly.FromDateTime(c.CompletionDate))
                .ToHashSet();

            var skippedDates = habitComps
                .Where(c => string.Equals(c.Status, "Skipped", StringComparison.OrdinalIgnoreCase))
                .Select(c => DateOnly.FromDateTime(c.CompletionDate))
                .ToHashSet();

            var totalCompletions = completedDates.Count;
            var past30DaysCompletions = completedDates.Count(d => d >= thirtyDaysAgo && d <= today);
            DateOnly? lastCompleted = completedDates.Count > 0 ? completedDates.OrderByDescending(d => d).First() : null;

            var (currentStreak, longestStreak, rate30Days) = CalculateHabitStreak(
                habit.Frequency,
                habit.TargetFrequencyDays > 0 ? habit.TargetFrequencyDays : 1,
                completedDates,
                skippedDates,
                today,
                thirtyDaysAgo,
                past30DaysCompletions);

            streakList.Add(new HabitStreakInfo
            {
                HabitId = habit.Id,
                HabitName = habit.Name,
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak,
                TotalCompletions = totalCompletions,
                CompletionRate30Days = rate30Days,
                LastCompletedDate = lastCompleted
            });
        }

        return streakList.AsReadOnly();
    }

    private static (int CurrentStreak, int LongestStreak, double Rate30Days) CalculateHabitStreak(
        string frequency,
        uint targetFrequencyDays,
        HashSet<DateOnly> completedDates,
        HashSet<DateOnly> skippedDates,
        DateOnly today,
        DateOnly thirtyDaysAgo,
        int past30DaysCompletions)
    {
        var freq = frequency?.Trim().ToUpperInvariant() ?? "DAILY";

        if (freq == "WEEKLY" || freq == "CUSTOMDAYS")
        {
            return CalculateWeeklyStreak(targetFrequencyDays, completedDates, today, past30DaysCompletions);
        }

        if (freq == "WEEKDAYS")
        {
            return CalculateWeekdaysStreak(completedDates, skippedDates, today, thirtyDaysAgo, past30DaysCompletions);
        }

        if (freq == "WEEKENDS")
        {
            return CalculateWeekendsStreak(completedDates, skippedDates, today, thirtyDaysAgo, past30DaysCompletions);
        }

        // Default: Daily
        return CalculateDailyStreak(completedDates, skippedDates, today, past30DaysCompletions);
    }

    private static (int CurrentStreak, int LongestStreak, double Rate30Days) CalculateDailyStreak(
        HashSet<DateOnly> completedDates,
        HashSet<DateOnly> skippedDates,
        DateOnly today,
        int past30DaysCompletions)
    {
        var rate30Days = Math.Min(100.0, Math.Round((double)past30DaysCompletions / 30.0 * 100.0, 1));

        if (completedDates.Count == 0)
        {
            return (0, 0, rate30Days);
        }

        // Current streak
        var currentStreak = 0;
        var checkDate = today;
        if (!completedDates.Contains(today))
        {
            if (skippedDates.Contains(today))
            {
                checkDate = today.AddDays(-1);
            }
            else if (completedDates.Contains(today.AddDays(-1)) || skippedDates.Contains(today.AddDays(-1)))
            {
                checkDate = today.AddDays(-1);
            }
            else
            {
                checkDate = DateOnly.MinValue; // Streak broken
            }
        }

        if (checkDate != DateOnly.MinValue)
        {
            while (completedDates.Contains(checkDate) || skippedDates.Contains(checkDate))
            {
                if (completedDates.Contains(checkDate))
                {
                    currentStreak++;
                }
                checkDate = checkDate.AddDays(-1);
            }
        }

        // Longest streak
        var ascending = completedDates.OrderBy(d => d).ToList();
        var longestStreak = 1;
        var run = 1;

        for (var i = 1; i < ascending.Count; i++)
        {
            var gap = ascending[i].DayNumber - ascending[i - 1].DayNumber;
            if (gap == 1)
            {
                run++;
            }
            else if (gap > 1)
            {
                var allSkipped = true;
                for (var d = ascending[i - 1].AddDays(1); d < ascending[i]; d = d.AddDays(1))
                {
                    if (!skippedDates.Contains(d))
                    {
                        allSkipped = false;
                        break;
                    }
                }
                if (allSkipped)
                {
                    run++;
                }
                else
                {
                    run = 1;
                }
            }
            if (run > longestStreak) longestStreak = run;
        }

        return (currentStreak, longestStreak, rate30Days);
    }

    private static (int CurrentStreak, int LongestStreak, double Rate30Days) CalculateWeekdaysStreak(
        HashSet<DateOnly> completedDates,
        HashSet<DateOnly> skippedDates,
        DateOnly today,
        DateOnly thirtyDaysAgo,
        int past30DaysCompletions)
    {
        var weekdayCount = 0;
        for (var d = thirtyDaysAgo; d <= today; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
            {
                weekdayCount++;
            }
        }
        var rate30Days = weekdayCount > 0 ? Math.Min(100.0, Math.Round((double)past30DaysCompletions / weekdayCount * 100.0, 1)) : 0.0;

        if (completedDates.Count == 0)
        {
            return (0, 0, rate30Days);
        }

        // Start checking from today if weekday, or previous weekday
        var checkDate = (today.DayOfWeek == DayOfWeek.Saturday || today.DayOfWeek == DayOfWeek.Sunday)
            ? GetPreviousWeekday(today)
            : today;

        if (!completedDates.Contains(checkDate))
        {
            var prevWk = GetPreviousWeekday(checkDate);
            if (skippedDates.Contains(checkDate))
            {
                checkDate = prevWk;
            }
            else if (completedDates.Contains(prevWk) || skippedDates.Contains(prevWk))
            {
                checkDate = prevWk;
            }
            else
            {
                checkDate = DateOnly.MinValue;
            }
        }

        var currentStreak = 0;
        if (checkDate != DateOnly.MinValue)
        {
            while (completedDates.Contains(checkDate) || skippedDates.Contains(checkDate))
            {
                if (completedDates.Contains(checkDate))
                {
                    currentStreak++;
                }
                checkDate = GetPreviousWeekday(checkDate);
            }
        }

        // Longest weekday streak
        var weekdayCompletions = completedDates
            .Where(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
            .OrderBy(d => d)
            .ToList();

        var longestStreak = weekdayCompletions.Count > 0 ? 1 : 0;
        var run = 1;

        for (var i = 1; i < weekdayCompletions.Count; i++)
        {
            var expectedPrev = GetPreviousWeekday(weekdayCompletions[i]);
            if (weekdayCompletions[i - 1] == expectedPrev)
            {
                run++;
            }
            else
            {
                run = 1;
            }
            if (run > longestStreak) longestStreak = run;
        }

        return (currentStreak, longestStreak, rate30Days);
    }

    private static (int CurrentStreak, int LongestStreak, double Rate30Days) CalculateWeekendsStreak(
        HashSet<DateOnly> completedDates,
        HashSet<DateOnly> skippedDates,
        DateOnly today,
        DateOnly thirtyDaysAgo,
        int past30DaysCompletions)
    {
        var weekendCount = 0;
        for (var d = thirtyDaysAgo; d <= today; d = d.AddDays(1))
        {
            if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
            {
                weekendCount++;
            }
        }
        var rate30Days = weekendCount > 0 ? Math.Min(100.0, Math.Round((double)past30DaysCompletions / weekendCount * 100.0, 1)) : 0.0;

        if (completedDates.Count == 0)
        {
            return (0, 0, rate30Days);
        }

        var checkDate = (today.DayOfWeek == DayOfWeek.Saturday || today.DayOfWeek == DayOfWeek.Sunday)
            ? today
            : GetPreviousWeekendDay(today);

        if (!completedDates.Contains(checkDate))
        {
            var prevWk = GetPreviousWeekendDay(checkDate);
            if (skippedDates.Contains(checkDate))
            {
                checkDate = prevWk;
            }
            else if (completedDates.Contains(prevWk) || skippedDates.Contains(prevWk))
            {
                checkDate = prevWk;
            }
            else
            {
                checkDate = DateOnly.MinValue;
            }
        }

        var currentStreak = 0;
        if (checkDate != DateOnly.MinValue)
        {
            while (completedDates.Contains(checkDate) || skippedDates.Contains(checkDate))
            {
                if (completedDates.Contains(checkDate))
                {
                    currentStreak++;
                }
                checkDate = GetPreviousWeekendDay(checkDate);
            }
        }

        var weekendCompletions = completedDates
            .Where(d => d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
            .OrderBy(d => d)
            .ToList();

        var longestStreak = weekendCompletions.Count > 0 ? 1 : 0;
        var run = 1;

        for (var i = 1; i < weekendCompletions.Count; i++)
        {
            var expectedPrev = GetPreviousWeekendDay(weekendCompletions[i]);
            if (weekendCompletions[i - 1] == expectedPrev)
            {
                run++;
            }
            else
            {
                run = 1;
            }
            if (run > longestStreak) longestStreak = run;
        }

        return (currentStreak, longestStreak, rate30Days);
    }

    private static (int CurrentStreak, int LongestStreak, double Rate30Days) CalculateWeeklyStreak(
        uint targetFrequencyDays,
        HashSet<DateOnly> completedDates,
        DateOnly today,
        int past30DaysCompletions)
    {
        var target = Math.Max(1u, targetFrequencyDays);
        var expected30Days = (30.0 / 7.0) * target;
        var rate30Days = Math.Min(100.0, Math.Round((double)past30DaysCompletions / expected30Days * 100.0, 1));

        if (completedDates.Count == 0)
        {
            return (0, 0, rate30Days);
        }

        // Group completions by ISO calendar week start (Monday)
        var weeklyCounts = new Dictionary<DateOnly, int>();
        foreach (var date in completedDates)
        {
            var monday = GetMondayOfWeek(date);
            weeklyCounts[monday] = weeklyCounts.GetValueOrDefault(monday) + 1;
        }

        var currentMonday = GetMondayOfWeek(today);
        var prevMonday = currentMonday.AddDays(-7);

        var currentStreak = 0;
        var checkMonday = currentMonday;

        // If current week not yet satisfied, check if it's ongoing or should start from previous week
        if (weeklyCounts.GetValueOrDefault(currentMonday) < target)
        {
            if (weeklyCounts.GetValueOrDefault(prevMonday) >= target)
            {
                checkMonday = prevMonday;
            }
            else
            {
                checkMonday = DateOnly.MinValue;
            }
        }

        if (checkMonday != DateOnly.MinValue)
        {
            while (weeklyCounts.GetValueOrDefault(checkMonday) >= target)
            {
                currentStreak++;
                checkMonday = checkMonday.AddDays(-7);
            }
        }

        // Longest streak across satisfied weeks
        var satisfiedWeeks = weeklyCounts
            .Where(kv => kv.Value >= target)
            .Select(kv => kv.Key)
            .OrderBy(w => w)
            .ToList();

        var longestStreak = satisfiedWeeks.Count > 0 ? 1 : 0;
        var run = 1;

        for (var i = 1; i < satisfiedWeeks.Count; i++)
        {
            if (satisfiedWeeks[i] == satisfiedWeeks[i - 1].AddDays(7))
            {
                run++;
            }
            else
            {
                run = 1;
            }
            if (run > longestStreak) longestStreak = run;
        }

        return (currentStreak, longestStreak, rate30Days);
    }

    private static DateOnly GetPreviousWeekday(DateOnly d)
    {
        var prev = d.AddDays(-1);
        while (prev.DayOfWeek == DayOfWeek.Saturday || prev.DayOfWeek == DayOfWeek.Sunday)
        {
            prev = prev.AddDays(-1);
        }
        return prev;
    }

    private static DateOnly GetPreviousWeekendDay(DateOnly d)
    {
        var prev = d.AddDays(-1);
        while (prev.DayOfWeek != DayOfWeek.Saturday && prev.DayOfWeek != DayOfWeek.Sunday)
        {
            prev = prev.AddDays(-1);
        }
        return prev;
    }

    private static DateOnly GetMondayOfWeek(DateOnly d)
    {
        var diff = (7 + (d.DayOfWeek - DayOfWeek.Monday)) % 7;
        return d.AddDays(-diff);
    }
}

