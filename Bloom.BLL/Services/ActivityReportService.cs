using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;

namespace Bloom.BLL.Services;

public class ActivityReportService : IActivityReportService
{
    private readonly ITimeAccountingService _timeAccountingService;

    public ActivityReportService(ITimeAccountingService timeAccountingService)
    {
        _timeAccountingService = timeAccountingService;
    }

    public async Task<DailyTimeSummaryDto> GetDailyReportAsync(ulong userId, DateOnly targetDate, string? timezoneId = null, CancellationToken ct = default)
    {
        return await _timeAccountingService.GetDailySummaryAsync(userId, targetDate, timezoneId, ct);
    }

    public async Task<WeeklyReportDto> GetWeeklyReportAsync(ulong userId, DateOnly referenceDate, string? timezoneId = null, CancellationToken ct = default)
    {
        // Monday-based week
        int diff = (7 + (referenceDate.DayOfWeek - DayOfWeek.Monday)) % 7;
        var startOfWeek = referenceDate.AddDays(-diff);
        var endOfWeek = startOfWeek.AddDays(6);

        var dailySummaries = new List<DailyReportSummaryDto>();
        var groupMinuteMap = new Dictionary<string, (double Minutes, string Color)>(StringComparer.OrdinalIgnoreCase);
        var subjectMinuteMap = new Dictionary<string, (string Name, double Minutes)>(StringComparer.OrdinalIgnoreCase);

        double totalTrackedMinutes = 0;
        double totalStudyMinutes = 0;
        double totalSleepMinutes = 0;
        double totalWeekMinutes = 0;

        for (int i = 0; i < 7; i++)
        {
            var date = startOfWeek.AddDays(i);
            var daySummary = await _timeAccountingService.GetDailySummaryAsync(userId, date, timezoneId, ct);

            totalTrackedMinutes += daySummary.TrackedMinutes;
            totalStudyMinutes += daySummary.StudyMinutes;
            totalSleepMinutes += daySummary.SleepMinutes;
            totalWeekMinutes += daySummary.TotalDayMinutes;

            dailySummaries.Add(new DailyReportSummaryDto
            {
                Date = date,
                TotalTrackedHours = Math.Round(daySummary.TrackedMinutes / 60.0, 2),
                StudyHours = Math.Round(daySummary.StudyMinutes / 60.0, 2),
                SleepHours = Math.Round(daySummary.SleepMinutes / 60.0, 2),
                UntrackedHours = Math.Round(daySummary.UntrackedMinutes / 60.0, 2)
            });

            foreach (var item in daySummary.Distribution)
            {
                if (!groupMinuteMap.ContainsKey(item.CategoryGroup))
                {
                    groupMinuteMap[item.CategoryGroup] = (item.TotalMinutes, item.Color);
                }
                else
                {
                    var existing = groupMinuteMap[item.CategoryGroup];
                    groupMinuteMap[item.CategoryGroup] = (existing.Minutes + item.TotalMinutes, existing.Color);
                }
            }

            foreach (var subj in daySummary.SubjectStudyBreakdown)
            {
                if (!subjectMinuteMap.ContainsKey(subj.SubjectCode))
                {
                    subjectMinuteMap[subj.SubjectCode] = (subj.SubjectName, subj.TotalMinutes);
                }
                else
                {
                    var existing = subjectMinuteMap[subj.SubjectCode];
                    subjectMinuteMap[subj.SubjectCode] = (existing.Name, existing.Minutes + subj.TotalMinutes);
                }
            }
        }

        var categoryDistribution = groupMinuteMap.Select(kv => new CategoryTimeDistributionDto
        {
            CategoryGroup = kv.Key,
            TotalMinutes = Math.Round(kv.Value.Minutes, 1),
            PercentageOfDay = totalWeekMinutes > 0 ? Math.Round((kv.Value.Minutes / totalWeekMinutes) * 100.0, 1) : 0,
            Color = kv.Value.Color
        }).OrderByDescending(x => x.TotalMinutes).ToList();

        var subjectBreakdown = subjectMinuteMap.Select(kv => new SubjectStudyTimeDto
        {
            SubjectCode = kv.Key,
            SubjectName = kv.Value.Name,
            TotalMinutes = Math.Round(kv.Value.Minutes, 1)
        }).OrderByDescending(x => x.TotalMinutes).ToList();

        return new WeeklyReportDto
        {
            StartDate = startOfWeek,
            EndDate = endOfWeek,
            TotalTrackedHours = Math.Round(totalTrackedMinutes / 60.0, 2),
            TotalStudyHours = Math.Round(totalStudyMinutes / 60.0, 2),
            TotalSleepHours = Math.Round(totalSleepMinutes / 60.0, 2),
            AverageDailyStudyHours = Math.Round((totalStudyMinutes / 60.0) / 7.0, 2),
            AverageDailySleepHours = Math.Round((totalSleepMinutes / 60.0) / 7.0, 2),
            DailySummaries = dailySummaries,
            CategoryDistribution = categoryDistribution,
            SubjectBreakdown = subjectBreakdown
        };
    }

    public async Task<MonthlyReportDto> GetMonthlyReportAsync(ulong userId, int year, int month, string? timezoneId = null, CancellationToken ct = default)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);

        var dailySummaries = new List<DailyReportSummaryDto>();
        var groupMinuteMap = new Dictionary<string, (double Minutes, string Color)>(StringComparer.OrdinalIgnoreCase);
        var subjectMinuteMap = new Dictionary<string, (string Name, double Minutes)>(StringComparer.OrdinalIgnoreCase);

        double totalTrackedMinutes = 0;
        double totalStudyMinutes = 0;
        double totalSleepMinutes = 0;
        double totalMonthMinutes = 0;
        int activeStudyDays = 0;

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            var daySummary = await _timeAccountingService.GetDailySummaryAsync(userId, date, timezoneId, ct);

            totalTrackedMinutes += daySummary.TrackedMinutes;
            totalStudyMinutes += daySummary.StudyMinutes;
            totalSleepMinutes += daySummary.SleepMinutes;
            totalMonthMinutes += daySummary.TotalDayMinutes;

            if (daySummary.StudyMinutes > 0)
            {
                activeStudyDays++;
            }

            dailySummaries.Add(new DailyReportSummaryDto
            {
                Date = date,
                TotalTrackedHours = Math.Round(daySummary.TrackedMinutes / 60.0, 2),
                StudyHours = Math.Round(daySummary.StudyMinutes / 60.0, 2),
                SleepHours = Math.Round(daySummary.SleepMinutes / 60.0, 2),
                UntrackedHours = Math.Round(daySummary.UntrackedMinutes / 60.0, 2)
            });

            foreach (var item in daySummary.Distribution)
            {
                if (!groupMinuteMap.ContainsKey(item.CategoryGroup))
                {
                    groupMinuteMap[item.CategoryGroup] = (item.TotalMinutes, item.Color);
                }
                else
                {
                    var existing = groupMinuteMap[item.CategoryGroup];
                    groupMinuteMap[item.CategoryGroup] = (existing.Minutes + item.TotalMinutes, existing.Color);
                }
            }

            foreach (var subj in daySummary.SubjectStudyBreakdown)
            {
                if (!subjectMinuteMap.ContainsKey(subj.SubjectCode))
                {
                    subjectMinuteMap[subj.SubjectCode] = (subj.SubjectName, subj.TotalMinutes);
                }
                else
                {
                    var existing = subjectMinuteMap[subj.SubjectCode];
                    subjectMinuteMap[subj.SubjectCode] = (existing.Name, existing.Minutes + subj.TotalMinutes);
                }
            }
        }

        var categoryDistribution = groupMinuteMap.Select(kv => new CategoryTimeDistributionDto
        {
            CategoryGroup = kv.Key,
            TotalMinutes = Math.Round(kv.Value.Minutes, 1),
            PercentageOfDay = totalMonthMinutes > 0 ? Math.Round((kv.Value.Minutes / totalMonthMinutes) * 100.0, 1) : 0,
            Color = kv.Value.Color
        }).OrderByDescending(x => x.TotalMinutes).ToList();

        var subjectBreakdown = subjectMinuteMap.Select(kv => new SubjectStudyTimeDto
        {
            SubjectCode = kv.Key,
            SubjectName = kv.Value.Name,
            TotalMinutes = Math.Round(kv.Value.Minutes, 1)
        }).OrderByDescending(x => x.TotalMinutes).ToList();

        return new MonthlyReportDto
        {
            Year = year,
            Month = month,
            TotalTrackedHours = Math.Round(totalTrackedMinutes / 60.0, 2),
            TotalStudyHours = Math.Round(totalStudyMinutes / 60.0, 2),
            TotalSleepHours = Math.Round(totalSleepMinutes / 60.0, 2),
            AverageDailyStudyHours = Math.Round((totalStudyMinutes / 60.0) / daysInMonth, 2),
            TotalActiveStudyDays = activeStudyDays,
            DailySummaries = dailySummaries,
            CategoryDistribution = categoryDistribution,
            SubjectBreakdown = subjectBreakdown
        };
    }
}

