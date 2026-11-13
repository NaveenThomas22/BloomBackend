using Bloom.BLL.DTOs;

namespace Bloom.BLL.Interfaces;

public interface IActivityCategoryService
{
    Task<IReadOnlyList<ActivityCategoryDto>> GetCategoriesAsync(ulong userId, bool includeInactive = false, CancellationToken ct = default);
    Task<ActivityCategoryDto> GetCategoryByIdAsync(ulong userId, ulong categoryId, CancellationToken ct = default);
    Task<ActivityCategoryDto> CreateCustomCategoryAsync(ulong userId, CreateCategoryDto dto, CancellationToken ct = default);
    Task<ActivityCategoryDto> UpdateCategoryAsync(ulong userId, ulong categoryId, UpdateCategoryDto dto, CancellationToken ct = default);
    Task<bool> DeleteCategoryAsync(ulong userId, ulong categoryId, CancellationToken ct = default);
}

public interface IActivityRecordService
{
    Task<ActivityRecordDto> GetRecordByIdAsync(ulong userId, ulong recordId, CancellationToken ct = default);
    Task<ActivityRecordDto> CreateRecordAsync(ulong userId, CreateActivityRecordDto dto, CancellationToken ct = default);
    Task<ActivityRecordDto> UpdateRecordAsync(ulong userId, ulong recordId, UpdateActivityRecordDto dto, CancellationToken ct = default);
    Task<bool> DeleteRecordAsync(ulong userId, ulong recordId, CancellationToken ct = default);
}

public interface ITimeAccountingService
{
    Task<DailyTimelineDto> GetDailyTimelineAsync(ulong userId, DateOnly targetDate, string? timezoneId = null, CancellationToken ct = default);
    Task<DailyTimeSummaryDto> GetDailySummaryAsync(ulong userId, DateOnly targetDate, string? timezoneId = null, CancellationToken ct = default);
}

public interface IStudyTimerService
{
    Task<ActiveTimerDto> GetActiveTimerAsync(ulong userId, CancellationToken ct = default);
    Task<ActiveTimerDto> StartTimerAsync(ulong userId, StartTimerDto dto, CancellationToken ct = default);
    Task<ActiveTimerDto> PauseTimerAsync(ulong userId, CancellationToken ct = default);
    Task<ActiveTimerDto> ResumeTimerAsync(ulong userId, CancellationToken ct = default);
    Task<ActivityRecordDto> CompleteTimerAsync(ulong userId, CompleteTimerDto dto, CancellationToken ct = default);
    Task<bool> CancelTimerAsync(ulong userId, CancellationToken ct = default);
}

public interface IPlannedActivityService
{
    Task<IReadOnlyList<PlannedActivityDto>> GetPlannedActivitiesAsync(ulong userId, DateOnly targetDate, string? timezoneId = null, CancellationToken ct = default);
    Task<PlannedActivityDto> GetPlannedByIdAsync(ulong userId, ulong planId, CancellationToken ct = default);
    Task<PlannedActivityDto> CreatePlannedActivityAsync(ulong userId, CreatePlannedActivityDto dto, CancellationToken ct = default);
    Task<PlannedActivityDto> UpdatePlannedActivityAsync(ulong userId, ulong planId, UpdatePlannedActivityDto dto, CancellationToken ct = default);
    Task<bool> DeletePlannedActivityAsync(ulong userId, ulong planId, CancellationToken ct = default);
    Task<ActivityRecordDto> StartPlannedActivityAsync(ulong userId, ulong planId, CancellationToken ct = default);
    Task<PlannedActivityDto> SkipPlannedActivityAsync(ulong userId, ulong planId, CancellationToken ct = default);
}

public interface IActivityReportService
{
    Task<DailyTimeSummaryDto> GetDailyReportAsync(ulong userId, DateOnly targetDate, string? timezoneId = null, CancellationToken ct = default);
    Task<WeeklyReportDto> GetWeeklyReportAsync(ulong userId, DateOnly referenceDate, string? timezoneId = null, CancellationToken ct = default);
    Task<MonthlyReportDto> GetMonthlyReportAsync(ulong userId, int year, int month, string? timezoneId = null, CancellationToken ct = default);
}

