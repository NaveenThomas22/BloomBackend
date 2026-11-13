using System.Data;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AnalyticsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<AnalyticsOverviewData> GetOverviewMetricsAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            -- Period study hours
            SELECT COALESCE(SUM(TIMESTAMPDIFF(SECOND, ar.start_at_utc, ar.end_at_utc)), 0) / 3600.0
            FROM activity_records ar
            INNER JOIN study_session_details ssd ON ssd.activity_id = ar.id
            WHERE ar.user_id = @userId
              AND ar.activity_status = 'Completed'
              AND ar.is_planned = 0
              AND ar.end_at_utc IS NOT NULL
              AND ar.deleted_at_utc IS NULL
              AND (@startUtc IS NULL OR ar.start_at_utc >= @startUtc)
              AND (@endUtc IS NULL OR ar.start_at_utc <= @endUtc);

            -- All-time study hours
            SELECT COALESCE(SUM(TIMESTAMPDIFF(SECOND, ar.start_at_utc, ar.end_at_utc)), 0) / 3600.0
            FROM activity_records ar
            INNER JOIN study_session_details ssd ON ssd.activity_id = ar.id
            WHERE ar.user_id = @userId
              AND ar.activity_status = 'Completed'
              AND ar.is_planned = 0
              AND ar.end_at_utc IS NOT NULL
              AND ar.deleted_at_utc IS NULL;

            -- Question practice stats
            SELECT 
                COUNT(*) AS Attempted,
                COALESCE(SUM(CASE WHEN is_correct = 1 THEN 1 ELSE 0 END), 0) AS Correct
            FROM question_attempts
            WHERE user_id = @userId
              AND source_type = 'Practice'
              AND (@startUtc IS NULL OR created_at_utc >= @startUtc)
              AND (@endUtc IS NULL OR created_at_utc <= @endUtc);

            -- Topics stats
            SELECT 
                COALESCE(SUM(CASE WHEN status = 'Mastered' THEN 1 ELSE 0 END), 0) AS Mastered,
                COALESCE(SUM(CASE WHEN status = 'Completed' THEN 1 ELSE 0 END), 0) AS Completed
            FROM user_topic_progress
            WHERE user_id = @userId;

            SELECT COUNT(*) FROM syllabus_topics WHERE is_active = 1;

            -- Mock tests stats
            SELECT 
                COUNT(*) AS CompletedMocks,
                COALESCE(AVG(total_score), 0) AS AvgScore
            FROM exam_attempts
            WHERE user_id = @userId
              AND status IN ('Submitted', 'AutoSubmitted')
              AND (@startUtc IS NULL OR submitted_at_utc >= @startUtc)
              AND (@endUtc IS NULL OR submitted_at_utc <= @endUtc);

            -- Active timer state
            SELECT started_at_utc
            FROM user_timer_states
            WHERE user_id = @userId AND timer_state = 'Running';
        ";

        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sql, new { userId, startUtc, endUtc }, cancellationToken: ct));

        var studyHoursPeriod = await multi.ReadSingleOrDefaultAsync<decimal>();
        var studyHoursAllTime = await multi.ReadSingleOrDefaultAsync<decimal>();

        var qRow = await multi.ReadSingleOrDefaultAsync<dynamic>();
        int qAttempted = qRow != null ? (int)qRow.Attempted : 0;
        int qCorrect = qRow != null ? Convert.ToInt32(qRow.Correct) : 0;
        decimal practiceAccuracy = qAttempted > 0 ? Math.Round((decimal)qCorrect / qAttempted * 100m, 2) : 0m;

        var tRow = await multi.ReadSingleOrDefaultAsync<dynamic>();
        int topicsMastered = tRow != null ? Convert.ToInt32(tRow.Mastered) : 0;
        int topicsCompleted = tRow != null ? Convert.ToInt32(tRow.Completed) : 0;

        int totalTopics = await multi.ReadSingleOrDefaultAsync<int>();

        var mRow = await multi.ReadSingleOrDefaultAsync<dynamic>();
        int mockCount = mRow != null ? Convert.ToInt32(mRow.CompletedMocks) : 0;
        decimal avgMockScore = mRow != null ? Convert.ToDecimal(mRow.AvgScore) : 0m;

        var timerStart = await multi.ReadSingleOrDefaultAsync<DateTime?>();
        bool hasActiveTimer = timerStart.HasValue;
        int activeTimerMinutes = timerStart.HasValue
            ? (int)Math.Max(0, (DateTime.UtcNow - timerStart.Value).TotalMinutes)
            : 0;

        // Current streak query (consecutive days with completed study activity)
        const string streakSql = @"
            SELECT DISTINCT DATE(start_at_utc) AS StudyDate
            FROM activity_records ar
            INNER JOIN study_session_details ssd ON ssd.activity_id = ar.id
            WHERE ar.user_id = @userId
              AND ar.activity_status = 'Completed'
              AND ar.is_planned = 0
              AND ar.end_at_utc IS NOT NULL
              AND ar.deleted_at_utc IS NULL
            ORDER BY StudyDate DESC
            LIMIT 60;
        ";

        var studyDates = (await connection.QueryAsync<DateTime>(new CommandDefinition(streakSql, new { userId }, cancellationToken: ct)))
            .Select(d => DateOnly.FromDateTime(d))
            .ToHashSet();

        int currentStreak = 0;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var checkDate = studyDates.Contains(today) ? today : today.AddDays(-1);

        while (studyDates.Contains(checkDate))
        {
            currentStreak++;
            checkDate = checkDate.AddDays(-1);
        }

        return new AnalyticsOverviewData
        {
            StudyHoursPeriod = Math.Round(studyHoursPeriod, 2),
            StudyHoursAllTime = Math.Round(studyHoursAllTime, 2),
            QuestionsAttempted = qAttempted,
            QuestionsCorrect = qCorrect,
            PracticeAccuracy = practiceAccuracy,
            TopicsMastered = topicsMastered,
            TopicsCompleted = topicsCompleted,
            TotalTopics = totalTopics,
            MockTestsCompleted = mockCount,
            AverageMockScore = Math.Round(avgMockScore, 2),
            CurrentStreakDays = currentStreak,
            HasActiveTimer = hasActiveTimer,
            ActiveTimerMinutes = activeTimerMinutes
        };
    }

    public async Task<IReadOnlyList<SubjectPerformanceData>> GetSubjectPerformanceAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT 
                s.id AS SubjectId,
                s.code AS SubjectCode,
                s.name AS SubjectName,
                (SELECT COUNT(st.id) 
                 FROM syllabus_topics st 
                 JOIN syllabus_modules sm ON sm.id = st.module_id 
                 WHERE sm.subject_id = s.id AND st.is_active = 1) AS TotalTopics,
                (SELECT COUNT(utp.id) 
                 FROM user_topic_progress utp 
                 JOIN syllabus_topics st ON st.id = utp.topic_id 
                 JOIN syllabus_modules sm ON sm.id = st.module_id 
                 WHERE sm.subject_id = s.id AND utp.user_id = @userId AND utp.status = 'Completed') AS CompletedTopics,
                (SELECT COUNT(utp.id) 
                 FROM user_topic_progress utp 
                 JOIN syllabus_topics st ON st.id = utp.topic_id 
                 JOIN syllabus_modules sm ON sm.id = st.module_id 
                 WHERE sm.subject_id = s.id AND utp.user_id = @userId AND utp.status = 'Mastered') AS MasteredTopics,
                COALESCE((
                    SELECT SUM(TIMESTAMPDIFF(SECOND, ar.start_at_utc, ar.end_at_utc)) / 3600.0
                    FROM activity_records ar
                    JOIN study_session_details ssd ON ssd.activity_id = ar.id
                    JOIN syllabus_topics st ON st.id = ssd.topic_id
                    JOIN syllabus_modules sm ON sm.id = st.module_id
                    WHERE ar.user_id = @userId
                      AND sm.subject_id = s.id
                      AND ar.activity_status = 'Completed'
                      AND ar.is_planned = 0
                      AND ar.end_at_utc IS NOT NULL
                      AND ar.deleted_at_utc IS NULL
                      AND (@startUtc IS NULL OR ar.start_at_utc >= @startUtc)
                      AND (@endUtc IS NULL OR ar.start_at_utc <= @endUtc)
                ), 0) AS StudyHours,
                COALESCE((
                    SELECT COUNT(*)
                    FROM question_attempts qa
                    JOIN questions q ON q.id = qa.question_id
                    WHERE qa.user_id = @userId
                      AND q.subject_id = s.id
                      AND (@startUtc IS NULL OR qa.created_at_utc >= @startUtc)
                      AND (@endUtc IS NULL OR qa.created_at_utc <= @endUtc)
                ), 0) AS QuestionsAttempted,
                COALESCE((
                    SELECT SUM(CASE WHEN qa.is_correct = 1 THEN 1 ELSE 0 END)
                    FROM question_attempts qa
                    JOIN questions q ON q.id = qa.question_id
                    WHERE qa.user_id = @userId
                      AND q.subject_id = s.id
                      AND (@startUtc IS NULL OR qa.created_at_utc >= @startUtc)
                      AND (@endUtc IS NULL OR qa.created_at_utc <= @endUtc)
                ), 0) AS QuestionsCorrect,
                COALESCE((
                    SELECT AVG(qa.time_spent_seconds)
                    FROM question_attempts qa
                    JOIN questions q ON q.id = qa.question_id
                    WHERE qa.user_id = @userId
                      AND q.subject_id = s.id
                      AND (@startUtc IS NULL OR qa.created_at_utc >= @startUtc)
                      AND (@endUtc IS NULL OR qa.created_at_utc <= @endUtc)
                ), 0) AS AverageTimeSpentSeconds
            FROM syllabus_subjects s
            WHERE s.is_active = 1
            ORDER BY s.display_order ASC;
        ";

        var list = (await connection.QueryAsync<SubjectPerformanceData>(
            new CommandDefinition(sql, new { userId, startUtc, endUtc }, cancellationToken: ct))).ToList();

        foreach (var item in list)
        {
            item.StudyHours = Math.Round(item.StudyHours, 2);
            item.AverageTimeSpentSeconds = Math.Round(item.AverageTimeSpentSeconds, 1);
            item.AccuracyPercentage = item.QuestionsAttempted > 0
                ? Math.Round((decimal)item.QuestionsCorrect / item.QuestionsAttempted * 100m, 2)
                : 0m;
        }

        return list;
    }

    public async Task<IReadOnlyList<TopicPerformanceData>> GetTopicPerformanceListAsync(ulong userId, ulong? subjectId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT 
                st.id AS TopicId,
                st.name AS TopicName,
                ss.code AS SubjectCode,
                ss.name AS SubjectName,
                sm.name AS ModuleName,
                COALESCE(utp.status, 'NotStarted') AS Status,
                COALESCE(utp.confidence_rating, 0) AS ConfidenceRating,
                COALESCE(qa_stats.Attempted, 0) AS QuestionsAttempted,
                COALESCE(qa_stats.Correct, 0) AS QuestionsCorrect,
                COALESCE(study_stats.StudyHours, 0) AS StudyHours
            FROM syllabus_topics st
            JOIN syllabus_modules sm ON sm.id = st.module_id
            JOIN syllabus_subjects ss ON ss.id = sm.subject_id
            LEFT JOIN user_topic_progress utp ON utp.topic_id = st.id AND utp.user_id = @userId
            LEFT JOIN (
                SELECT 
                    qa.topic_id,
                    COUNT(*) AS Attempted,
                    SUM(CASE WHEN qa.is_correct = 1 THEN 1 ELSE 0 END) AS Correct
                FROM question_attempts qa
                WHERE qa.user_id = @userId
                  AND (@startUtc IS NULL OR qa.created_at_utc >= @startUtc)
                  AND (@endUtc IS NULL OR qa.created_at_utc <= @endUtc)
                GROUP BY qa.topic_id
            ) qa_stats ON qa_stats.topic_id = st.id
            LEFT JOIN (
                SELECT 
                    ssd.topic_id,
                    SUM(TIMESTAMPDIFF(SECOND, ar.start_at_utc, ar.end_at_utc)) / 3600.0 AS StudyHours
                FROM activity_records ar
                JOIN study_session_details ssd ON ssd.activity_id = ar.id
                WHERE ar.user_id = @userId
                  AND ar.activity_status = 'Completed'
                  AND ar.is_planned = 0
                  AND ar.end_at_utc IS NOT NULL
                  AND ar.deleted_at_utc IS NULL
                  AND (@startUtc IS NULL OR ar.start_at_utc >= @startUtc)
                  AND (@endUtc IS NULL OR ar.start_at_utc <= @endUtc)
                GROUP BY ssd.topic_id
            ) study_stats ON study_stats.topic_id = st.id
            WHERE st.is_active = 1
              AND (@subjectId IS NULL OR ss.id = @subjectId)
            ORDER BY qa_stats.Attempted DESC, st.name ASC;
        ";

        var list = (await connection.QueryAsync<TopicPerformanceData>(
            new CommandDefinition(sql, new { userId, subjectId, startUtc, endUtc }, cancellationToken: ct))).ToList();

        foreach (var item in list)
        {
            item.StudyHours = Math.Round(item.StudyHours, 2);
            item.AccuracyPercentage = item.QuestionsAttempted > 0
                ? Math.Round((decimal)item.QuestionsCorrect / item.QuestionsAttempted * 100m, 2)
                : 0m;
        }

        return list;
    }

    public async Task<IReadOnlyList<DailyStudyTimeData>> GetDailyStudyTimeTrendsAsync(ulong userId, DateTime startUtc, DateTime endUtc, string timezoneId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT 
                ar.start_at_utc AS StartAtUtc,
                ar.end_at_utc AS EndAtUtc,
                ss.code AS SubjectCode
            FROM activity_records ar
            INNER JOIN study_session_details ssd ON ssd.activity_id = ar.id
            LEFT JOIN syllabus_topics st ON st.id = ssd.topic_id
            LEFT JOIN syllabus_modules sm ON sm.id = st.module_id
            LEFT JOIN syllabus_subjects ss ON ss.id = sm.subject_id
            WHERE ar.user_id = @userId
              AND ar.activity_status = 'Completed'
              AND ar.is_planned = 0
              AND ar.end_at_utc IS NOT NULL
              AND ar.deleted_at_utc IS NULL
              AND ar.start_at_utc >= @startUtc
              AND ar.start_at_utc <= @endUtc
            ORDER BY ar.start_at_utc ASC;
        ";

        var records = (await connection.QueryAsync<dynamic>(
            new CommandDefinition(sql, new { userId, startUtc, endUtc }, cancellationToken: ct))).ToList();

        TimeZoneInfo userTz;
        try
        {
            userTz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch
        {
            userTz = TimeZoneInfo.Utc;
        }

        var dailyMap = new Dictionary<DateOnly, DailyStudyTimeData>();

        // Pre-populate all dates in range
        var startLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc, userTz));
        var endLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(endUtc, userTz));

        for (var d = startLocal; d <= endLocal; d = d.AddDays(1))
        {
            dailyMap[d] = new DailyStudyTimeData { Date = d };
        }

        foreach (var row in records)
        {
            DateTime rowStart = row.StartAtUtc;
            DateTime rowEnd = row.EndAtUtc;
            string? subCode = row.SubjectCode;

            var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(rowStart, userTz));
            var hours = (decimal)(rowEnd - rowStart).TotalHours;
            if (hours <= 0) continue;

            if (!dailyMap.TryGetValue(localDate, out var dayData))
            {
                dayData = new DailyStudyTimeData { Date = localDate };
                dailyMap[localDate] = dayData;
            }

            dayData.StudyHours += hours;
            if (string.Equals(subCode, "QA", StringComparison.OrdinalIgnoreCase))
                dayData.QaHours += hours;
            else if (string.Equals(subCode, "DILR", StringComparison.OrdinalIgnoreCase))
                dayData.DilrHours += hours;
            else if (string.Equals(subCode, "VARC", StringComparison.OrdinalIgnoreCase))
                dayData.VarcHours += hours;
            else
                dayData.OtherHours += hours;
        }

        foreach (var item in dailyMap.Values)
        {
            item.StudyHours = Math.Round(item.StudyHours, 2);
            item.QaHours = Math.Round(item.QaHours, 2);
            item.DilrHours = Math.Round(item.DilrHours, 2);
            item.VarcHours = Math.Round(item.VarcHours, 2);
            item.OtherHours = Math.Round(item.OtherHours, 2);
        }

        return dailyMap.Values.OrderBy(x => x.Date).ToList();
    }

    public async Task<IReadOnlyList<TimeOfDayDistributionData>> GetTimeOfDayDistributionAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, string timezoneId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT 
                ar.start_at_utc AS StartAtUtc,
                ar.end_at_utc AS EndAtUtc
            FROM activity_records ar
            INNER JOIN study_session_details ssd ON ssd.activity_id = ar.id
            WHERE ar.user_id = @userId
              AND ar.activity_status = 'Completed'
              AND ar.is_planned = 0
              AND ar.end_at_utc IS NOT NULL
              AND ar.deleted_at_utc IS NULL
              AND (@startUtc IS NULL OR ar.start_at_utc >= @startUtc)
              AND (@endUtc IS NULL OR ar.start_at_utc <= @endUtc);
        ";

        var records = (await connection.QueryAsync<dynamic>(
            new CommandDefinition(sql, new { userId, startUtc, endUtc }, cancellationToken: ct))).ToList();

        TimeZoneInfo userTz;
        try { userTz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId); }
        catch { userTz = TimeZoneInfo.Utc; }

        var blocks = new Dictionary<string, TimeOfDayDistributionData>
        {
            ["Morning"] = new() { TimeBlock = "Morning (05:00 - 11:59)" },
            ["Afternoon"] = new() { TimeBlock = "Afternoon (12:00 - 16:59)" },
            ["Evening"] = new() { TimeBlock = "Evening (17:00 - 21:59)" },
            ["Night"] = new() { TimeBlock = "Night (22:00 - 04:59)" }
        };

        foreach (var row in records)
        {
            DateTime start = row.StartAtUtc;
            DateTime end = row.EndAtUtc;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(start, userTz);
            var hours = (decimal)(end - start).TotalHours;

            string key = localTime.Hour switch
            {
                >= 5 and < 12 => "Morning",
                >= 12 and < 17 => "Afternoon",
                >= 17 and < 22 => "Evening",
                _ => "Night"
            };

            blocks[key].StudyHours += hours;
            blocks[key].ActivityCount++;
        }

        foreach (var item in blocks.Values)
        {
            item.StudyHours = Math.Round(item.StudyHours, 2);
        }

        return blocks.Values.ToList();
    }

    public async Task<IReadOnlyList<LifeCategoryDistributionData>> GetLifeCategoryDistributionAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT 
                ac.category_group AS CategoryGroup,
                ac.category_name AS CategoryName,
                ac.color AS Color,
                COALESCE(SUM(TIMESTAMPDIFF(SECOND, ar.start_at_utc, ar.end_at_utc)), 0) / 3600.0 AS TotalHours
            FROM activity_records ar
            INNER JOIN activity_categories ac ON ac.id = ar.activity_category_id
            WHERE ar.user_id = @userId
              AND ar.activity_status = 'Completed'
              AND ar.is_planned = 0
              AND ar.end_at_utc IS NOT NULL
              AND ar.deleted_at_utc IS NULL
              AND (@startUtc IS NULL OR ar.start_at_utc >= @startUtc)
              AND (@endUtc IS NULL OR ar.start_at_utc <= @endUtc)
            GROUP BY ac.category_group, ac.category_name, ac.color
            ORDER BY TotalHours DESC;
        ";

        var list = (await connection.QueryAsync<LifeCategoryDistributionData>(
            new CommandDefinition(sql, new { userId, startUtc, endUtc }, cancellationToken: ct))).ToList();

        decimal totalHours = list.Sum(x => x.TotalHours);

        foreach (var item in list)
        {
            item.TotalHours = Math.Round(item.TotalHours, 2);
            item.Percentage = totalHours > 0 ? Math.Round(item.TotalHours / totalHours * 100m, 2) : 0m;
        }

        return list;
    }

    public async Task<LifeAccountingDayData> GetDailyLifeAccountingAsync(ulong userId, DateOnly localDate, string timezoneId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        TimeZoneInfo userTz;
        try { userTz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId); }
        catch { userTz = TimeZoneInfo.Utc; }

        var startLocal = localDate.ToDateTime(TimeOnly.MinValue);
        var endLocal = localDate.ToDateTime(TimeOnly.MaxValue);

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, userTz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, userTz);

        const string sql = @"
            SELECT COALESCE(SUM(TIMESTAMPDIFF(SECOND, ar.start_at_utc, ar.end_at_utc)), 0) / 3600.0
            FROM activity_records ar
            WHERE ar.user_id = @userId
              AND ar.activity_status = 'Completed'
              AND ar.is_planned = 0
              AND ar.end_at_utc IS NOT NULL
              AND ar.deleted_at_utc IS NULL
              AND ar.start_at_utc >= @startUtc
              AND ar.start_at_utc <= @endUtc;
        ";

        var accountedHours = await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(sql, new { userId, startUtc, endUtc }, cancellationToken: ct));

        decimal calendarHours = (decimal)(endUtc - startUtc).TotalHours;
        if (calendarHours <= 0) calendarHours = 24.0m;

        decimal untracked = Math.Max(0m, calendarHours - accountedHours);

        return new LifeAccountingDayData
        {
            Date = localDate,
            AccountedHours = Math.Round(accountedHours, 2),
            UntrackedHours = Math.Round(untracked, 2),
            CalendarDayHours = Math.Round(calendarHours, 2)
        };
    }

    public async Task<PlanAdherenceData> GetPlanAdherenceAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT 
                COALESCE(SUM(TIMESTAMPDIFF(SECOND, planned_start_at_utc, planned_end_at_utc)), 0) / 3600.0 AS PlannedHours,
                COUNT(*) AS ScheduledCount,
                COALESCE(SUM(CASE WHEN plan_status = 'Completed' THEN 1 ELSE 0 END), 0) AS CompletedCount
            FROM planned_activities
            WHERE user_id = @userId
              AND (@startUtc IS NULL OR planned_start_at_utc >= @startUtc)
              AND (@endUtc IS NULL OR planned_start_at_utc <= @endUtc);

            SELECT COALESCE(SUM(TIMESTAMPDIFF(SECOND, ar.start_at_utc, ar.end_at_utc)), 0) / 3600.0
            FROM activity_records ar
            JOIN planned_activities pa ON pa.actual_activity_id = ar.id
            WHERE pa.user_id = @userId
              AND ar.activity_status = 'Completed'
              AND ar.is_planned = 0
              AND ar.end_at_utc IS NOT NULL
              AND ar.deleted_at_utc IS NULL
              AND (@startUtc IS NULL OR pa.planned_start_at_utc >= @startUtc)
              AND (@endUtc IS NULL OR pa.planned_start_at_utc <= @endUtc);
        ";

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { userId, startUtc, endUtc }, cancellationToken: ct));

        var planRow = await multi.ReadSingleOrDefaultAsync<dynamic>();
        decimal plannedHours = planRow != null ? Convert.ToDecimal(planRow.PlannedHours) : 0m;
        int scheduledCount = planRow != null ? Convert.ToInt32(planRow.ScheduledCount) : 0;
        int completedCount = planRow != null ? Convert.ToInt32(planRow.CompletedCount) : 0;

        decimal actualHours = await multi.ReadSingleOrDefaultAsync<decimal>();

        decimal adherenceRate = scheduledCount > 0
            ? Math.Round((decimal)completedCount / scheduledCount * 100m, 2)
            : 0m;

        return new PlanAdherenceData
        {
            TotalPlannedHours = Math.Round(plannedHours, 2),
            TotalActualHours = Math.Round(actualHours, 2),
            TotalScheduledPlansCount = scheduledCount,
            CompletedPlansCount = completedCount,
            AdherenceRatePercentage = adherenceRate
        };
    }

    public async Task<QuestionPracticeSummaryData> GetQuestionPracticeSummaryAsync(ulong userId, ulong? subjectId, ulong? topicId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT COUNT(*)
            FROM practice_sessions
            WHERE user_id = @userId
              AND status = 'Completed'
              AND (@subjectId IS NULL OR subject_id = @subjectId)
              AND (@topicId IS NULL OR topic_id = @topicId)
              AND (@startUtc IS NULL OR started_at_utc >= @startUtc)
              AND (@endUtc IS NULL OR started_at_utc <= @endUtc);

            SELECT 
                COUNT(*) AS TotalAttempts,
                COALESCE(SUM(CASE WHEN qa.is_correct = 1 THEN 1 ELSE 0 END), 0) AS TotalCorrect,
                COALESCE(SUM(CASE WHEN q.question_type = 'MCQ' THEN 1 ELSE 0 END), 0) AS McqAttempts,
                COALESCE(SUM(CASE WHEN q.question_type = 'MCQ' AND qa.is_correct = 1 THEN 1 ELSE 0 END), 0) AS McqCorrect,
                COALESCE(SUM(CASE WHEN q.question_type = 'TITA' THEN 1 ELSE 0 END), 0) AS TitaAttempts,
                COALESCE(SUM(CASE WHEN q.question_type = 'TITA' AND qa.is_correct = 1 THEN 1 ELSE 0 END), 0) AS TitaCorrect,
                COALESCE(SUM(CASE WHEN q.difficulty_level = 'Easy' THEN 1 ELSE 0 END), 0) AS EasyAttempts,
                COALESCE(SUM(CASE WHEN q.difficulty_level = 'Easy' AND qa.is_correct = 1 THEN 1 ELSE 0 END), 0) AS EasyCorrect,
                COALESCE(SUM(CASE WHEN q.difficulty_level = 'Medium' THEN 1 ELSE 0 END), 0) AS MediumAttempts,
                COALESCE(SUM(CASE WHEN q.difficulty_level = 'Medium' AND qa.is_correct = 1 THEN 1 ELSE 0 END), 0) AS MediumCorrect,
                COALESCE(SUM(CASE WHEN q.difficulty_level = 'Hard' THEN 1 ELSE 0 END), 0) AS HardAttempts,
                COALESCE(SUM(CASE WHEN q.difficulty_level = 'Hard' AND qa.is_correct = 1 THEN 1 ELSE 0 END), 0) AS HardCorrect,
                COALESCE(AVG(qa.time_spent_seconds), 0) AS AvgSeconds
            FROM question_attempts qa
            JOIN questions q ON q.id = qa.question_id
            WHERE qa.user_id = @userId
              AND qa.source_type = 'Practice'
              AND (@subjectId IS NULL OR q.subject_id = @subjectId)
              AND (@topicId IS NULL OR q.topic_id = @topicId)
              AND (@startUtc IS NULL OR qa.created_at_utc >= @startUtc)
              AND (@endUtc IS NULL OR qa.created_at_utc <= @endUtc);
        ";

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { userId, subjectId, topicId, startUtc, endUtc }, cancellationToken: ct));

        int totalSessions = await multi.ReadSingleOrDefaultAsync<int>();
        var r = await multi.ReadSingleOrDefaultAsync<dynamic>();

        int totalAttempts = r != null ? Convert.ToInt32(r.TotalAttempts) : 0;
        int totalCorrect = r != null ? Convert.ToInt32(r.TotalCorrect) : 0;
        int mcqAttempts = r != null ? Convert.ToInt32(r.McqAttempts) : 0;
        int mcqCorrect = r != null ? Convert.ToInt32(r.McqCorrect) : 0;
        int titaAttempts = r != null ? Convert.ToInt32(r.TitaAttempts) : 0;
        int titaCorrect = r != null ? Convert.ToInt32(r.TitaCorrect) : 0;
        int easyAttempts = r != null ? Convert.ToInt32(r.EasyAttempts) : 0;
        int easyCorrect = r != null ? Convert.ToInt32(r.EasyCorrect) : 0;
        int medAttempts = r != null ? Convert.ToInt32(r.MediumAttempts) : 0;
        int medCorrect = r != null ? Convert.ToInt32(r.MediumCorrect) : 0;
        int hardAttempts = r != null ? Convert.ToInt32(r.HardAttempts) : 0;
        int hardCorrect = r != null ? Convert.ToInt32(r.HardCorrect) : 0;
        decimal avgSec = r != null ? Convert.ToDecimal(r.AvgSeconds) : 0m;

        return new QuestionPracticeSummaryData
        {
            TotalSessions = totalSessions,
            TotalQuestionsAttempted = totalAttempts,
            TotalQuestionsCorrect = totalCorrect,
            OverallAccuracy = totalAttempts > 0 ? Math.Round((decimal)totalCorrect / totalAttempts * 100m, 2) : 0m,
            McqAttempted = mcqAttempts,
            McqCorrect = mcqCorrect,
            McqAccuracy = mcqAttempts > 0 ? Math.Round((decimal)mcqCorrect / mcqAttempts * 100m, 2) : 0m,
            TitaAttempted = titaAttempts,
            TitaCorrect = titaCorrect,
            TitaAccuracy = titaAttempts > 0 ? Math.Round((decimal)titaCorrect / titaAttempts * 100m, 2) : 0m,
            EasyAccuracy = easyAttempts > 0 ? Math.Round((decimal)easyCorrect / easyAttempts * 100m, 2) : 0m,
            MediumAccuracy = medAttempts > 0 ? Math.Round((decimal)medCorrect / medAttempts * 100m, 2) : 0m,
            HardAccuracy = hardAttempts > 0 ? Math.Round((decimal)hardCorrect / hardAttempts * 100m, 2) : 0m,
            AvgSecondsPerQuestion = Math.Round(avgSec, 1)
        };
    }

    public async Task<IReadOnlyList<MockTestSummaryItem>> GetMockTestHistoryAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT 
                ea.id AS AttemptId,
                ea.exam_template_id AS ExamTemplateId,
                et.title AS ExamTitle,
                et.exam_type AS ExamType,
                ea.submitted_at_utc AS SubmittedAtUtc,
                ea.total_score AS TotalScore,
                ea.max_possible_score AS MaxPossibleScore,
                ea.accuracy_percentage AS AccuracyPercentage,
                ea.total_attempted AS TotalAttempted,
                ea.total_correct AS TotalCorrect,
                ea.total_incorrect AS TotalIncorrect,
                ea.total_unanswered AS TotalUnanswered,
                ea.time_spent_seconds AS TimeSpentSeconds
            FROM exam_attempts ea
            JOIN exam_templates et ON et.id = ea.exam_template_id
            WHERE ea.user_id = @userId
              AND ea.status IN ('Submitted', 'AutoSubmitted')
              AND (@startUtc IS NULL OR ea.submitted_at_utc >= @startUtc)
              AND (@endUtc IS NULL OR ea.submitted_at_utc <= @endUtc)
            ORDER BY ea.submitted_at_utc DESC
            LIMIT 50;

            SELECT 
                sr.exam_attempt_id AS AttemptId,
                sr.section_name AS SectionName,
                sr.score AS Score,
                sr.max_score AS MaxScore,
                sr.attempted AS Attempted,
                sr.correct AS Correct,
                sr.accuracy_percentage AS AccuracyPercentage,
                sr.time_spent_seconds AS TimeSpentSeconds
            FROM exam_attempt_section_results sr
            JOIN exam_attempts ea ON ea.id = sr.exam_attempt_id
            WHERE ea.user_id = @userId
              AND ea.status IN ('Submitted', 'AutoSubmitted')
              AND (@startUtc IS NULL OR ea.submitted_at_utc >= @startUtc)
              AND (@endUtc IS NULL OR ea.submitted_at_utc <= @endUtc)
            ORDER BY sr.display_order ASC;
        ";

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { userId, startUtc, endUtc }, cancellationToken: ct));

        var attempts = (await multi.ReadAsync<MockTestSummaryItem>()).ToList();
        var sectionResults = (await multi.ReadAsync<dynamic>()).ToList();

        var sectionLookup = sectionResults
            .GroupBy(s => (ulong)s.AttemptId)
            .ToDictionary(g => g.Key, g => g.Select(s => new MockSectionScoreItem
            {
                SectionName = (string)s.SectionName,
                Score = (decimal)s.Score,
                MaxScore = (decimal)s.MaxScore,
                Attempted = (int)s.Attempted,
                Correct = (int)s.Correct,
                AccuracyPercentage = (decimal)s.AccuracyPercentage,
                TimeSpentSeconds = (int)s.TimeSpentSeconds
            }).ToList());

        foreach (var att in attempts)
        {
            if (sectionLookup.TryGetValue(att.AttemptId, out var sections))
            {
                att.SectionScores = sections;
            }
        }

        return attempts;
    }

    public async Task<RevisionRetentionData> GetRevisionRetentionSummaryAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT 
                COUNT(*) AS TotalSchedules,
                COALESCE(SUM(CASE WHEN status = 'Completed' THEN 1 ELSE 0 END), 0) AS CompletedCount,
                COALESCE(SUM(CASE WHEN status = 'Overdue' THEN 1 ELSE 0 END), 0) AS OverdueCount,
                COALESCE(SUM(CASE WHEN status IN ('Upcoming', 'DueToday') THEN 1 ELSE 0 END), 0) AS UpcomingCount
            FROM revision_schedules
            WHERE user_id = @userId
              AND is_deleted = 0
              AND (@startUtc IS NULL OR scheduled_date >= DATE(@startUtc))
              AND (@endUtc IS NULL OR scheduled_date <= DATE(@endUtc));

            SELECT 
                COALESCE(AVG(confidence_rating), 0) AS AvgConfidence
            FROM revision_attempts
            WHERE user_id = @userId
              AND (@startUtc IS NULL OR revised_at_utc >= @startUtc)
              AND (@endUtc IS NULL OR revised_at_utc <= @endUtc);

            SELECT 
                understanding_level AS UnderstandingLevel,
                COUNT(*) AS TotalCount
            FROM revision_attempts
            WHERE user_id = @userId
              AND (@startUtc IS NULL OR revised_at_utc >= @startUtc)
              AND (@endUtc IS NULL OR revised_at_utc <= @endUtc)
            GROUP BY understanding_level;
        ";

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { userId, startUtc, endUtc }, cancellationToken: ct));

        var schedRow = await multi.ReadSingleOrDefaultAsync<dynamic>();
        int totalSched = schedRow != null ? Convert.ToInt32(schedRow.TotalSchedules) : 0;
        int completedSched = schedRow != null ? Convert.ToInt32(schedRow.CompletedCount) : 0;
        int overdueSched = schedRow != null ? Convert.ToInt32(schedRow.OverdueCount) : 0;
        int upcomingSched = schedRow != null ? Convert.ToInt32(schedRow.UpcomingCount) : 0;

        decimal avgConfidence = await multi.ReadSingleOrDefaultAsync<decimal>();

        var levelRows = (await multi.ReadAsync<dynamic>()).ToList();
        var distribution = new Dictionary<string, int>();
        foreach (var row in levelRows)
        {
            string lvl = row.UnderstandingLevel ?? "Unknown";
            distribution[lvl] = Convert.ToInt32(row.TotalCount);
        }

        decimal completionRate = totalSched > 0
            ? Math.Round((decimal)completedSched / totalSched * 100m, 2)
            : 0m;

        return new RevisionRetentionData
        {
            TotalSchedules = totalSched,
            CompletedSchedules = completedSched,
            OverdueSchedules = overdueSched,
            UpcomingSchedules = upcomingSched,
            CompletionRatePercentage = completionRate,
            AverageConfidence = Math.Round(avgConfidence, 2),
            UnderstandingDistribution = distribution
        };
    }

    public async Task<IReadOnlyList<GoalAnalyticsItem>> GetGoalAnalyticsAsync(ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT 
                ug.id AS GoalId,
                ug.title AS Title,
                ug.goal_period AS GoalPeriod,
                ug.target_metric AS TargetMetric,
                ug.target_value AS TargetValue,
                ug.unit AS Unit,
                ug.status AS Status
            FROM user_goals ug
            WHERE ug.user_id = @userId AND ug.is_deleted = 0
            ORDER BY ug.created_at_utc DESC;
        ";

        var list = (await connection.QueryAsync<dynamic>(
            new CommandDefinition(sql, new { userId }, cancellationToken: ct))).ToList();

        var result = new List<GoalAnalyticsItem>();
        foreach (var r in list)
        {
            decimal target = Convert.ToDecimal(r.TargetValue);
            string status = (string)r.Status;
            bool isCompleted = string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase);
            decimal current = isCompleted ? target : 0m;
            decimal progress = isCompleted ? 100m : 0m;

            result.Add(new GoalAnalyticsItem
            {
                GoalId = (ulong)r.GoalId,
                Title = (string)r.Title,
                GoalPeriod = (string)r.GoalPeriod,
                TargetMetric = (string)r.TargetMetric,
                TargetValue = target,
                CurrentValue = current,
                Unit = (string)r.Unit,
                ProgressPercentage = progress,
                IsCompleted = isCompleted
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<HabitConsistencyItem>> GetHabitConsistencyAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT 
                uh.id AS HabitId,
                uh.name AS HabitName,
                hc.category_name AS CategoryName,
                uh.frequency AS Frequency,
                COALESCE(SUM(CASE WHEN hcomp.status = 'Completed' THEN 1 ELSE 0 END), 0) AS TotalCompleted,
                COALESCE(SUM(CASE WHEN hcomp.status = 'Skipped' THEN 1 ELSE 0 END), 0) AS TotalSkipped,
                COALESCE(SUM(CASE WHEN hcomp.status = 'Missed' THEN 1 ELSE 0 END), 0) AS TotalMissed
            FROM user_habits uh
            JOIN habit_categories hc ON hc.id = uh.category_id
            LEFT JOIN habit_completions hcomp ON hcomp.habit_id = uh.id AND hcomp.user_id = uh.user_id
              AND (@startUtc IS NULL OR hcomp.completion_date >= DATE(@startUtc))
              AND (@endUtc IS NULL OR hcomp.completion_date <= DATE(@endUtc))
            WHERE uh.user_id = @userId
              AND uh.deleted_at_utc IS NULL
            GROUP BY uh.id, uh.name, hc.category_name, uh.frequency
            ORDER BY uh.display_order ASC;
        ";

        var list = (await connection.QueryAsync<dynamic>(
            new CommandDefinition(sql, new { userId, startUtc, endUtc }, cancellationToken: ct))).ToList();

        // Calculate streaks per habit
        const string datesSql = @"
            SELECT habit_id AS HabitId, completion_date AS CompletionDate
            FROM habit_completions
            WHERE user_id = @userId AND status = 'Completed'
            ORDER BY habit_id, completion_date DESC;
        ";

        var completionRows = (await connection.QueryAsync<dynamic>(
            new CommandDefinition(datesSql, new { userId }, cancellationToken: ct))).ToList();

        var completionDatesByHabit = completionRows
            .GroupBy(x => (ulong)x.HabitId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => DateOnly.FromDateTime((DateTime)x.CompletionDate)).ToHashSet()
            );

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = new List<HabitConsistencyItem>();

        foreach (var row in list)
        {
            ulong habitId = (ulong)row.HabitId;
            int completed = Convert.ToInt32(row.TotalCompleted);
            int skipped = Convert.ToInt32(row.TotalSkipped);
            int missed = Convert.ToInt32(row.TotalMissed);
            int totalExpected = completed + skipped + missed;

            decimal compRate = totalExpected > 0
                ? Math.Round((decimal)completed / totalExpected * 100m, 2)
                : 0m;

            int currentStreak = 0;
            int longestStreak = 0;

            if (completionDatesByHabit.TryGetValue(habitId, out var dates) && dates.Count > 0)
            {
                var checkDate = dates.Contains(today) ? today : today.AddDays(-1);
                while (dates.Contains(checkDate))
                {
                    currentStreak++;
                    checkDate = checkDate.AddDays(-1);
                }

                // Calculate longest streak from sorted dates
                var sortedDates = dates.OrderBy(d => d).ToList();
                int tempStreak = 0;
                DateOnly? prev = null;
                foreach (var d in sortedDates)
                {
                    if (prev.HasValue && d == prev.Value.AddDays(1))
                    {
                        tempStreak++;
                    }
                    else
                    {
                        tempStreak = 1;
                    }
                    if (tempStreak > longestStreak) longestStreak = tempStreak;
                    prev = d;
                }
            }

            result.Add(new HabitConsistencyItem
            {
                HabitId = habitId,
                HabitName = (string)row.HabitName,
                CategoryName = (string)row.CategoryName,
                Frequency = (string)row.Frequency,
                CurrentStreak = currentStreak,
                LongestStreak = Math.Max(currentStreak, longestStreak),
                CompletionRatePercentage = compRate,
                TotalCompleted = completed,
                TotalSkipped = skipped,
                TotalMissed = missed
            });
        }

        return result;
    }

    public async Task<MindsetGrowthData> GetMindsetGrowthDataAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT 
                COUNT(CASE WHEN morning_completed_at_utc IS NOT NULL THEN 1 END) AS MorningCount,
                COUNT(CASE WHEN evening_completed_at_utc IS NOT NULL THEN 1 END) AS EveningCount,
                COUNT(DISTINCT reflection_date) AS TotalDays,
                AVG(morning_energy_level) AS AvgEnergy,
                AVG(satisfaction_rating) AS AvgSatisfaction,
                COALESCE(SUM(study_target_minutes), 0) AS PlannedStudyMinutes
            FROM daily_reflections
            WHERE user_id = @userId
              AND (@startUtc IS NULL OR reflection_date >= DATE(@startUtc))
              AND (@endUtc IS NULL OR reflection_date <= DATE(@endUtc));

            -- Actual study minutes in that period
            SELECT COALESCE(SUM(TIMESTAMPDIFF(SECOND, ar.start_at_utc, ar.end_at_utc)), 0) / 60
            FROM activity_records ar
            INNER JOIN study_session_details ssd ON ssd.activity_id = ar.id
            WHERE ar.user_id = @userId
              AND ar.activity_status = 'Completed'
              AND ar.is_planned = 0
              AND ar.end_at_utc IS NOT NULL
              AND ar.deleted_at_utc IS NULL
              AND (@startUtc IS NULL OR ar.start_at_utc >= @startUtc)
              AND (@endUtc IS NULL OR ar.start_at_utc <= @endUtc);

            -- Mood distribution from morning and evening moods (strictly ID and code, zero text)
            SELECT 
                rm.mood_code AS MoodCode,
                rm.mood_name AS MoodName,
                rm.emoji AS Emoji,
                rm.color AS Color,
                COUNT(*) AS MoodCount
            FROM (
                SELECT morning_mood_id AS mood_id FROM daily_reflections 
                WHERE user_id = @userId AND morning_mood_id IS NOT NULL
                  AND (@startUtc IS NULL OR reflection_date >= DATE(@startUtc))
                  AND (@endUtc IS NULL OR reflection_date <= DATE(@endUtc))
                UNION ALL
                SELECT evening_mood_id AS mood_id FROM daily_reflections 
                WHERE user_id = @userId AND evening_mood_id IS NOT NULL
                  AND (@startUtc IS NULL OR reflection_date >= DATE(@startUtc))
                  AND (@endUtc IS NULL OR reflection_date <= DATE(@endUtc))
            ) combined
            JOIN reflection_moods rm ON rm.id = combined.mood_id
            GROUP BY rm.mood_code, rm.mood_name, rm.emoji, rm.color
            ORDER BY MoodCount DESC;
        ";

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { userId, startUtc, endUtc }, cancellationToken: ct));

        var statsRow = await multi.ReadSingleOrDefaultAsync<dynamic>();
        int morningCount = statsRow != null ? Convert.ToInt32(statsRow.MorningCount) : 0;
        int eveningCount = statsRow != null ? Convert.ToInt32(statsRow.EveningCount) : 0;
        int totalDays = statsRow != null ? Convert.ToInt32(statsRow.TotalDays) : 0;
        decimal? avgEnergy = statsRow?.AvgEnergy != null ? Convert.ToDecimal(statsRow.AvgEnergy) : (decimal?)null;
        decimal? avgSatisfaction = statsRow?.AvgSatisfaction != null ? Convert.ToDecimal(statsRow.AvgSatisfaction) : (decimal?)null;
        int plannedMins = statsRow != null ? Convert.ToInt32(statsRow.PlannedStudyMinutes) : 0;

        int actualStudyMins = Convert.ToInt32(await multi.ReadSingleOrDefaultAsync<decimal>());

        var moodRows = (await multi.ReadAsync<dynamic>()).ToList();
        int totalMoodCount = moodRows.Sum(m => Convert.ToInt32(m.MoodCount));

        var moodDist = new List<MoodCountData>();
        foreach (var m in moodRows)
        {
            int count = Convert.ToInt32(m.MoodCount);
            moodDist.Add(new MoodCountData
            {
                MoodCode = (string)m.MoodCode,
                MoodName = (string)m.MoodName,
                Emoji = (string)m.Emoji,
                Color = (string)m.Color,
                Count = count,
                Percentage = totalMoodCount > 0 ? Math.Round((decimal)count / totalMoodCount * 100m, 2) : 0m
            });
        }

        return new MindsetGrowthData
        {
            MorningReflectionsCount = morningCount,
            EveningReflectionsCount = eveningCount,
            TotalDaysWithReflections = totalDays,
            AverageMorningEnergy = avgEnergy.HasValue ? Math.Round(avgEnergy.Value, 1) : null,
            AverageEveningSatisfaction = avgSatisfaction.HasValue ? Math.Round(avgSatisfaction.Value, 1) : null,
            TotalPlannedStudyMinutes = plannedMins,
            ActualStudyMinutes = actualStudyMins,
            MoodDistribution = moodDist
        };
    }
}

