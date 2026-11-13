using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bloom.BLL.Services;

public class TimeAccountingService : ITimeAccountingService
{
    private readonly IActivityRecordRepository _activityRepo;
    private readonly IActivityCategoryRepository _categoryRepo;
    private readonly IStudySessionDetailRepository _studyDetailRepo;
    private readonly IPlannedActivityRepository _plannedRepo;
    private readonly ISyllabusRepository _syllabusRepo;
    private readonly IUserProfileRepository _userProfileRepo;
    private readonly ILogger<TimeAccountingService> _logger;

    public TimeAccountingService(
        IActivityRecordRepository activityRepo,
        IActivityCategoryRepository categoryRepo,
        IStudySessionDetailRepository studyDetailRepo,
        IPlannedActivityRepository plannedRepo,
        ISyllabusRepository syllabusRepo,
        IUserProfileRepository userProfileRepo,
        ILogger<TimeAccountingService> logger)
    {
        _activityRepo = activityRepo ?? throw new ArgumentNullException(nameof(activityRepo));
        _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        _studyDetailRepo = studyDetailRepo ?? throw new ArgumentNullException(nameof(studyDetailRepo));
        _plannedRepo = plannedRepo ?? throw new ArgumentNullException(nameof(plannedRepo));
        _syllabusRepo = syllabusRepo ?? throw new ArgumentNullException(nameof(syllabusRepo));
        _userProfileRepo = userProfileRepo ?? throw new ArgumentNullException(nameof(userProfileRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DailyTimelineDto> GetDailyTimelineAsync(
        ulong userId, 
        DateOnly targetDate, 
        string? timezoneId = null, 
        CancellationToken ct = default)
    {
        var tz = await ResolveTimeZoneAsync(userId, timezoneId, ct);
        var (startUtc, endUtc, totalDayMinutes) = ComputeLocalDayBoundsUtc(targetDate, tz);
        var nowUtc = DateTime.UtcNow;

        // Fetch categories map
        var categories = (await _categoryRepo.GetCategoriesAsync(userId, true, ct)) ?? Array.Empty<ActivityCategory>();
        var catMap = categories.ToDictionary(c => c.Id, c => c);

        // Fetch records intersecting the local day
        var records = (await _activityRepo.GetRecordsByIntervalAsync(userId, startUtc, endUtc, false, ct)) ?? Array.Empty<ActivityRecord>();
        var recordIds = records.Select(r => r.Id).ToList();
        var studyDetailsMap = (await _studyDetailRepo.GetByActivityIdsAsync(recordIds, ct)) ?? new Dictionary<ulong, StudySessionDetail>();

        // Fetch planned items
        var plannedActivities = (await _plannedRepo.GetPlannedByIntervalAsync(userId, startUtc, endUtc, ct)) ?? Array.Empty<PlannedActivity>();

        // Topic lookup for enriched names
        var topicIds = studyDetailsMap.Values
            .Where(s => s.TopicId.HasValue)
            .Select(s => s.TopicId!.Value)
            .Distinct()
            .ToList();
        var topicMap = new Dictionary<ulong, SyllabusTopic>();
        foreach (var tid in topicIds)
        {
            var t = await _syllabusRepo.GetTopicByIdAsync(tid, userId, ct);
            if (t != null) topicMap[tid] = t;
        }

        // Build sorted timeline items
        var timelineItems = new List<TimelineItemDto>();
        var totalTrackedMinutes = 0.0;

        // Time tracking cursor for untracked gaps
        var cursorUtc = startUtc;
        var maxTimeForGapsUtc = nowUtc < endUtc ? nowUtc : endUtc;

        foreach (var rec in records.OrderBy(r => r.StartAtUtc))
        {
            var recEffectiveEndUtc = rec.EndAtUtc ?? (nowUtc < endUtc ? nowUtc : endUtc);
            if (recEffectiveEndUtc <= startUtc) continue;

            var itemStartUtc = rec.StartAtUtc < startUtc ? startUtc : rec.StartAtUtc;
            var itemEndUtc = recEffectiveEndUtc > endUtc ? endUtc : recEffectiveEndUtc;

            if (itemEndUtc <= itemStartUtc) continue;

            // Untracked gap before this activity
            if (itemStartUtc > cursorUtc && cursorUtc < maxTimeForGapsUtc)
            {
                var gapEnd = itemStartUtc < maxTimeForGapsUtc ? itemStartUtc : maxTimeForGapsUtc;
                if (gapEnd > cursorUtc)
                {
                    var gapMins = Math.Round((gapEnd - cursorUtc).TotalMinutes, 1);
                    if (gapMins >= 1.0)
                    {
                        timelineItems.Add(new TimelineItemDto
                        {
                            ItemType = "Untracked",
                            CategoryCode = "UNTRACKED",
                            CategoryName = "Untracked Time",
                            CategoryGroup = "Other",
                            Color = "#E9E0D1",
                            Icon = "clock",
                            Title = "Untracked Time",
                            StartTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(cursorUtc, tz),
                            EndTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(gapEnd, tz),
                            DurationMinutes = gapMins,
                            IsRunning = false
                        });
                    }
                }
            }

            var durationMins = Math.Round((itemEndUtc - itemStartUtc).TotalMinutes, 1);
            totalTrackedMinutes += durationMins;

            catMap.TryGetValue(rec.ActivityCategoryId, out var cat);
            studyDetailsMap.TryGetValue(rec.Id, out var sDetail);

            StudySessionDetailDto? mappedStudyDetail = null;
            if (sDetail != null)
            {
                topicMap.TryGetValue(sDetail.TopicId ?? 0, out var tEntity);
                mappedStudyDetail = new StudySessionDetailDto
                {
                    ActivityId = rec.Id,
                    TopicId = sDetail.TopicId,
                    TopicName = tEntity?.Name,
                    StudyMode = sDetail.StudyMode,
                    DifficultyLevel = sDetail.DifficultyLevel,
                    QuestionsAttempted = sDetail.QuestionsAttempted,
                    QuestionsCorrect = sDetail.QuestionsCorrect,
                    SessionNotes = sDetail.SessionNotes
                };
            }

            timelineItems.Add(new TimelineItemDto
            {
                ItemType = "Activity",
                ActivityId = rec.Id,
                CategoryId = rec.ActivityCategoryId,
                CategoryCode = cat?.CategoryCode ?? "OTHER",
                CategoryName = cat?.CategoryName ?? "Activity",
                CategoryGroup = cat?.CategoryGroup ?? "Other",
                Color = cat?.Color ?? "#A8BDA5",
                Icon = cat?.Icon ?? "activity",
                Title = rec.ActivityTitle,
                Description = rec.Description,
                StartTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(itemStartUtc, tz),
                EndTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(itemEndUtc, tz),
                DurationMinutes = durationMins,
                IsRunning = rec.ActivityStatus == "Running" && rec.EndAtUtc == null,
                StudyDetails = mappedStudyDetail
            });

            if (itemEndUtc > cursorUtc)
            {
                cursorUtc = itemEndUtc;
            }
        }

        // Untracked gap after last activity up to nowUtc or endUtc
        if (cursorUtc < maxTimeForGapsUtc)
        {
            var remainingGapMins = Math.Round((maxTimeForGapsUtc - cursorUtc).TotalMinutes, 1);
            if (remainingGapMins >= 1.0)
            {
                timelineItems.Add(new TimelineItemDto
                {
                    ItemType = "Untracked",
                    CategoryCode = "UNTRACKED",
                    CategoryName = "Untracked Time",
                    CategoryGroup = "Other",
                    Color = "#E9E0D1",
                    Icon = "clock",
                    Title = "Untracked Time",
                    StartTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(cursorUtc, tz),
                    EndTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(maxTimeForGapsUtc, tz),
                    DurationMinutes = remainingGapMins,
                    IsRunning = false
                });
            }
            cursorUtc = maxTimeForGapsUtc;
        }

        // Future unallocated time for remaining part of today
        if (cursorUtc < endUtc)
        {
            var futureMins = Math.Round((endUtc - cursorUtc).TotalMinutes, 1);
            if (futureMins >= 1.0)
            {
                timelineItems.Add(new TimelineItemDto
                {
                    ItemType = "Future",
                    CategoryCode = "FUTURE",
                    CategoryName = "Remaining Day",
                    CategoryGroup = "Other",
                    Color = "#F8F5EB",
                    Icon = "calendar",
                    Title = "Remaining Day",
                    StartTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(cursorUtc, tz),
                    EndTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(endUtc, tz),
                    DurationMinutes = futureMins,
                    IsRunning = false
                });
            }
        }

        // Map planned items
        var mappedPlanned = plannedActivities.Select(p =>
        {
            catMap.TryGetValue(p.ActivityCategoryId, out var cat);
            topicMap.TryGetValue(p.LinkedTopicId ?? 0, out var t);
            return new PlannedActivityDto
            {
                Id = p.Id,
                UserId = p.UserId,
                ActivityCategoryId = p.ActivityCategoryId,
                CategoryCode = cat?.CategoryCode ?? "OTHER",
                CategoryName = cat?.CategoryName ?? "Planned Activity",
                CategoryGroup = cat?.CategoryGroup ?? "Other",
                Color = cat?.Color ?? "#A8BDA5",
                Icon = cat?.Icon ?? "activity",
                Title = p.Title,
                Description = p.Description,
                PlannedStartAtUtc = p.PlannedStartAtUtc,
                PlannedEndAtUtc = p.PlannedEndAtUtc,
                TimezoneId = p.TimezoneId,
                LinkedTopicId = p.LinkedTopicId,
                LinkedTopicName = t?.Name,
                PlanStatus = p.PlanStatus,
                ActualActivityId = p.ActualActivityId,
                PlannedDurationMinutes = p.PlannedDurationMinutes
            };
        }).ToList();

        var totalPlannedMinutes = mappedPlanned.Sum(p => p.PlannedDurationMinutes);
        var totalUntrackedMinutes = Math.Max(0, totalDayMinutes - totalTrackedMinutes);

        return new DailyTimelineDto
        {
            TargetDate = targetDate,
            TimezoneId = tz.Id,
            TotalDayMinutes = Math.Round(totalDayMinutes, 1),
            TrackedMinutes = Math.Round(totalTrackedMinutes, 1),
            UntrackedMinutes = Math.Round(totalUntrackedMinutes, 1),
            PlannedMinutes = Math.Round(totalPlannedMinutes, 1),
            TimelineItems = timelineItems,
            PlannedItems = mappedPlanned
        };
    }

    public async Task<DailyTimeSummaryDto> GetDailySummaryAsync(
        ulong userId, 
        DateOnly targetDate, 
        string? timezoneId = null, 
        CancellationToken ct = default)
    {
        var tz = await ResolveTimeZoneAsync(userId, timezoneId, ct);
        var (startUtc, endUtc, totalDayMinutes) = ComputeLocalDayBoundsUtc(targetDate, tz);
        var nowUtc = DateTime.UtcNow;

        var categories = (await _categoryRepo.GetCategoriesAsync(userId, true, ct)) ?? Array.Empty<ActivityCategory>();
        var catMap = categories.ToDictionary(c => c.Id, c => c);

        var records = (await _activityRepo.GetRecordsByIntervalAsync(userId, startUtc, endUtc, false, ct)) ?? Array.Empty<ActivityRecord>();
        var recordIds = records.Select(r => r.Id).ToList();
        var studyDetailsMap = (await _studyDetailRepo.GetByActivityIdsAsync(recordIds, ct)) ?? new Dictionary<ulong, StudySessionDetail>();

        // Group durations by CategoryGroup
        var groupMinutes = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var totalTrackedMinutes = 0.0;

        // Subject study tracking (QA, DILR, VARC)
        var subjectMinutes = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        // Pre-fetch topic details for study breakdown
        var topicIds = studyDetailsMap.Values
            .Where(s => s.TopicId.HasValue)
            .Select(s => s.TopicId!.Value)
            .Distinct()
            .ToList();
        var topicModuleSubjectMap = new Dictionary<ulong, (string code, string name)>();
        foreach (var tid in topicIds)
        {
            var t = await _syllabusRepo.GetTopicByIdAsync(tid, userId, ct);
            if (t != null)
            {
                var m = await _syllabusRepo.GetModuleByIdAsync(t.ModuleId, userId, ct);
                if (m != null)
                {
                    var s = await _syllabusRepo.GetSubjectByIdAsync(m.SubjectId, userId, ct);
                    if (s != null)
                    {
                        topicModuleSubjectMap[tid] = (s.Code, s.Name);
                    }
                }
            }
        }

        foreach (var rec in records)
        {
            var recEffectiveEndUtc = rec.EndAtUtc ?? (nowUtc < endUtc ? nowUtc : endUtc);
            if (recEffectiveEndUtc <= startUtc) continue;

            var itemStartUtc = rec.StartAtUtc < startUtc ? startUtc : rec.StartAtUtc;
            var itemEndUtc = recEffectiveEndUtc > endUtc ? endUtc : recEffectiveEndUtc;

            if (itemEndUtc <= itemStartUtc) continue;

            var durationMins = (itemEndUtc - itemStartUtc).TotalMinutes;
            totalTrackedMinutes += durationMins;

            catMap.TryGetValue(rec.ActivityCategoryId, out var cat);
            var grp = cat?.CategoryGroup ?? "Other";

            groupMinutes[grp] = groupMinutes.GetValueOrDefault(grp) + durationMins;

            // If study, record subject breakdown
            if (studyDetailsMap.TryGetValue(rec.Id, out var sDetail) && sDetail.TopicId.HasValue)
            {
                if (topicModuleSubjectMap.TryGetValue(sDetail.TopicId.Value, out var sInfo))
                {
                    subjectMinutes[sInfo.code] = subjectMinutes.GetValueOrDefault(sInfo.code) + durationMins;
                }
            }
        }

        var totalUntrackedMinutes = Math.Max(0, totalDayMinutes - totalTrackedMinutes);

        // Build distribution list
        var distribution = groupMinutes.Select(kvp => new CategoryTimeDistributionDto
        {
            CategoryGroup = kvp.Key,
            TotalMinutes = Math.Round(kvp.Value, 1),
            PercentageOfDay = totalDayMinutes > 0 ? Math.Round((kvp.Value / totalDayMinutes) * 100.0, 1) : 0,
            Color = GetGroupDefaultColor(kvp.Key)
        }).OrderByDescending(d => d.TotalMinutes).ToList();

        // Build subject breakdown
        var subjectBreakdown = subjectMinutes.Select(kvp => new SubjectStudyTimeDto
        {
            SubjectCode = kvp.Key,
            SubjectName = GetSubjectNameByCode(kvp.Key),
            TotalMinutes = Math.Round(kvp.Value, 1)
        }).OrderByDescending(s => s.TotalMinutes).ToList();

        // Fetch planned duration
        var planned = await _plannedRepo.GetPlannedByIntervalAsync(userId, startUtc, endUtc, ct);
        var totalPlannedMinutes = planned.Sum(p => p.PlannedDurationMinutes);

        return new DailyTimeSummaryDto
        {
            TargetDate = targetDate,
            TimezoneId = tz.Id,
            TotalDayMinutes = Math.Round(totalDayMinutes, 1),
            TrackedMinutes = Math.Round(totalTrackedMinutes, 1),
            UntrackedMinutes = Math.Round(totalUntrackedMinutes, 1),
            StudyMinutes = Math.Round(groupMinutes.GetValueOrDefault("Study"), 1),
            SleepMinutes = Math.Round(groupMinutes.GetValueOrDefault("RestAndSleep") + groupMinutes.GetValueOrDefault("Sleep"), 1),
            MealMinutes = Math.Round(groupMinutes.GetValueOrDefault("MealsAndBreaks") + groupMinutes.GetValueOrDefault("Meals"), 1),
            WorkMinutes = Math.Round(groupMinutes.GetValueOrDefault("WorkAndCommute") + groupMinutes.GetValueOrDefault("Work"), 1),
            FitnessMinutes = Math.Round(groupMinutes.GetValueOrDefault("HealthAndFitness") + groupMinutes.GetValueOrDefault("Fitness"), 1),
            LeisureMinutes = Math.Round(groupMinutes.GetValueOrDefault("LeisureAndSocial") + groupMinutes.GetValueOrDefault("Leisure"), 1),
            PersonalMinutes = Math.Round(groupMinutes.GetValueOrDefault("Personal"), 1),
            OtherMinutes = Math.Round(groupMinutes.GetValueOrDefault("Other"), 1),
            PlannedMinutes = Math.Round(totalPlannedMinutes, 1),
            Distribution = distribution,
            SubjectStudyBreakdown = subjectBreakdown
        };
    }

    private async Task<TimeZoneInfo> ResolveTimeZoneAsync(ulong userId, string? requestedTimezone, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(requestedTimezone))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(requestedTimezone); } catch { /* fallback */ }
        }

        var profile = await _userProfileRepo.GetByUserIdAsync(userId, ct);
        if (!string.IsNullOrWhiteSpace(profile?.Timezone))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(profile.Timezone); } catch { /* fallback */ }
        }

        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); } catch { return TimeZoneInfo.Utc; }
    }

    public static (DateTime startUtc, DateTime endUtc, double totalMinutes) ComputeLocalDayBoundsUtc(DateOnly date, TimeZoneInfo tz)
    {
        var localStart = date.ToDateTime(TimeOnly.MinValue);
        var localNextDayStart = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var startUtc = ConvertLocalToUtcSafe(localStart, tz, preferDaylight: false);
        var nextDayStartUtc = ConvertLocalToUtcSafe(localNextDayStart, tz, preferDaylight: false);
        var totalMinutes = (nextDayStartUtc - startUtc).TotalMinutes;

        return (startUtc, nextDayStartUtc, totalMinutes);
    }

    private static DateTime ConvertLocalToUtcSafe(DateTime localTime, TimeZoneInfo tz, bool preferDaylight)
    {
        if (tz.IsInvalidTime(localTime))
        {
            var offset = tz.GetUtcOffset(localTime.AddHours(1));
            return DateTime.SpecifyKind(localTime - offset, DateTimeKind.Utc);
        }

        if (tz.IsAmbiguousTime(localTime))
        {
            var offsets = tz.GetAmbiguousTimeOffsets(localTime);
            var offset = preferDaylight ? offsets.Max() : offsets.Min();
            return DateTime.SpecifyKind(localTime - offset, DateTimeKind.Utc);
        }

        return TimeZoneInfo.ConvertTimeToUtc(localTime, tz);
    }

    private static string GetGroupDefaultColor(string group) => group switch
    {
        "Study" => "#2D6A4F",
        "RestAndSleep" => "#405B49",
        "MealsAndBreaks" => "#BC6C25",
        "WorkAndCommute" => "#457B9D",
        "HealthAndFitness" => "#E76F51",
        "LeisureAndSocial" => "#FFB703",
        "Personal" => "#D8B4E2",
        _ => "#A8BDA5"
    };

    private static string GetSubjectNameByCode(string code) => code.ToUpperInvariant() switch
    {
        "QA" => "Quantitative Aptitude",
        "DILR" => "Data Interpretation & LR",
        "VARC" => "Verbal Ability & RC",
        _ => code
    };
}
