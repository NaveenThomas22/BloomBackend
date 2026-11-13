using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Enums;
using Bloom.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bloom.BLL.Services;

public class GoalProgressService : IGoalProgressService
{
    private readonly IActivityRecordRepository _activityRepo;
    private readonly IActivityCategoryRepository _categoryRepo;
    private readonly IStudySessionDetailRepository _studyDetailRepo;
    private readonly ISyllabusRepository _syllabusRepo;
    private readonly ISyllabusProgressRepository _syllabusProgressRepo;
    private readonly IRevisionAttemptRepository _revisionAttemptRepo;
    private readonly IUserProfileRepository _userProfileRepo;
    private readonly ILogger<GoalProgressService> _logger;

    public GoalProgressService(
        IActivityRecordRepository activityRepo,
        IActivityCategoryRepository categoryRepo,
        IStudySessionDetailRepository studyDetailRepo,
        ISyllabusRepository syllabusRepo,
        ISyllabusProgressRepository syllabusProgressRepo,
        IRevisionAttemptRepository revisionAttemptRepo,
        IUserProfileRepository userProfileRepo,
        ILogger<GoalProgressService> logger)
    {
        _activityRepo = activityRepo ?? throw new ArgumentNullException(nameof(activityRepo));
        _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        _studyDetailRepo = studyDetailRepo ?? throw new ArgumentNullException(nameof(studyDetailRepo));
        _syllabusRepo = syllabusRepo ?? throw new ArgumentNullException(nameof(syllabusRepo));
        _syllabusProgressRepo = syllabusProgressRepo ?? throw new ArgumentNullException(nameof(syllabusProgressRepo));
        _revisionAttemptRepo = revisionAttemptRepo ?? throw new ArgumentNullException(nameof(revisionAttemptRepo));
        _userProfileRepo = userProfileRepo ?? throw new ArgumentNullException(nameof(userProfileRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<decimal> CalculateCurrentProgressAsync(
        ulong userId,
        string targetMetric,
        DateOnly startDate,
        DateOnly endDate,
        ulong? subjectId,
        ulong? topicId,
        CancellationToken ct = default)
    {
        var tz = await ResolveTimeZoneAsync(userId, ct);
        var (startUtc, _, _) = TimeAccountingService.ComputeLocalDayBoundsUtc(startDate, tz);
        var (_, endUtc, _) = TimeAccountingService.ComputeLocalDayBoundsUtc(endDate, tz);

        if (string.Equals(targetMetric, TargetMetric.StudyMinutes.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return await CalculateStudyMinutesAsync(userId, startUtc, endUtc, subjectId, topicId, ct);
        }

        if (string.Equals(targetMetric, TargetMetric.TopicsCompleted.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return await CalculateTopicsCompletedAsync(userId, startDate, endDate, subjectId, topicId, ct);
        }

        if (string.Equals(targetMetric, TargetMetric.SubtopicsCompleted.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return await CalculateSubtopicsCompletedAsync(userId, startDate, endDate, subjectId, topicId, ct);
        }

        if (string.Equals(targetMetric, TargetMetric.RevisionsCompleted.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var count = await _revisionAttemptRepo.GetCompletedAttemptsCountAsync(userId, startDate, endDate, topicId, subjectId, ct);
            return count;
        }

        if (string.Equals(targetMetric, TargetMetric.ActivitiesCount.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var records = await _activityRepo.GetRecordsByIntervalAsync(userId, startUtc, endUtc, false, ct);
            return records.Count(r => !r.IsPlanned && r.EndAtUtc.HasValue);
        }

        return 0m;
    }

    private async Task<decimal> CalculateStudyMinutesAsync(
        ulong userId,
        DateTime startUtc,
        DateTime endUtc,
        ulong? subjectId,
        ulong? topicId,
        CancellationToken ct)
    {
        var records = await _activityRepo.GetRecordsByIntervalAsync(userId, startUtc, endUtc, false, ct);
        var completedRecords = records.Where(r => !r.IsPlanned && r.EndAtUtc.HasValue && r.EndAtUtc.Value > r.StartAtUtc).ToList();

        if (completedRecords.Count == 0) return 0m;

        var categories = await _categoryRepo.GetCategoriesAsync(userId, true, ct);
        var catGroupMap = categories.ToDictionary(c => c.Id, c => c.CategoryGroup);

        var recordIds = completedRecords.Select(r => r.Id).ToList();
        var studyDetails = await _studyDetailRepo.GetByActivityIdsAsync(recordIds, ct);

        HashSet<ulong>? topicIdFilter = null;
        if (topicId.HasValue)
        {
            topicIdFilter = new HashSet<ulong> { topicId.Value };
        }
        else if (subjectId.HasValue)
        {
            var topics = await GetTopicsForSubjectAsync(subjectId.Value, userId, ct);
            topicIdFilter = topics.Select(t => t.Id).ToHashSet();
        }

        double totalMinutes = 0;
        foreach (var r in completedRecords)
        {
            var isStudyCategory = catGroupMap.TryGetValue(r.ActivityCategoryId, out var group) &&
                (string.Equals(group, "CAT Study", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(group, "Study", StringComparison.OrdinalIgnoreCase));

            var hasStudyDetail = studyDetails.TryGetValue(r.Id, out var detail);

            if (topicIdFilter != null)
            {
                // Must match the filtered topics
                if (!hasStudyDetail || !detail!.TopicId.HasValue || !topicIdFilter.Contains(detail.TopicId.Value))
                {
                    continue;
                }
            }
            else
            {
                // Any study activity
                if (!isStudyCategory && !hasStudyDetail)
                {
                    continue;
                }
            }

            // Calculate overlap between [r.StartAtUtc, r.EndAtUtc] and [startUtc, endUtc]
            var actualStart = r.StartAtUtc > startUtc ? r.StartAtUtc : startUtc;
            var actualEnd = r.EndAtUtc!.Value < endUtc ? r.EndAtUtc.Value : endUtc;
            if (actualEnd > actualStart)
            {
                totalMinutes += (actualEnd - actualStart).TotalMinutes;
            }
        }

        return Math.Round((decimal)totalMinutes, 1);
    }

    private async Task<decimal> CalculateTopicsCompletedAsync(
        ulong userId,
        DateOnly startDate,
        DateOnly endDate,
        ulong? subjectId,
        ulong? topicId,
        CancellationToken ct)
    {
        var allProgress = await _syllabusProgressRepo.GetAllUserProgressAsync(userId, ct);
        var completed = allProgress.Where(p =>
            (string.Equals(p.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(p.Status, "Mastered", StringComparison.OrdinalIgnoreCase)) &&
            p.CompletionDate.HasValue).ToList();

        if (topicId.HasValue)
        {
            completed = completed.Where(p => p.TopicId == topicId.Value).ToList();
        }
        else if (subjectId.HasValue)
        {
            var topics = await GetTopicsForSubjectAsync(subjectId.Value, userId, ct);
            var topicIds = topics.Select(t => t.Id).ToHashSet();
            completed = completed.Where(p => topicIds.Contains(p.TopicId)).ToList();
        }

        var count = completed.Count(p =>
        {
            var date = DateOnly.FromDateTime(p.CompletionDate!.Value);
            return date >= startDate && date <= endDate;
        });

        return count;
    }

    private async Task<decimal> CalculateSubtopicsCompletedAsync(
        ulong userId,
        DateOnly startDate,
        DateOnly endDate,
        ulong? subjectId,
        ulong? topicId,
        CancellationToken ct)
    {
        if (topicId.HasValue)
        {
            var subtopics = await _syllabusRepo.GetSubtopicsByTopicIdAsync(topicId.Value, userId, false, ct);
            var subtopicIds = subtopics.Select(s => s.Id).ToList();
            var map = await _syllabusProgressRepo.GetSubtopicProgressMapAsync(userId, subtopicIds, ct);
            var count = map.Values.Count(p => p.IsCompleted && p.CompletedAtUtc.HasValue &&
                DateOnly.FromDateTime(p.CompletedAtUtc.Value) >= startDate &&
                DateOnly.FromDateTime(p.CompletedAtUtc.Value) <= endDate);
            return count;
        }

        if (subjectId.HasValue)
        {
            var topics = await GetTopicsForSubjectAsync(subjectId.Value, userId, ct);
            int total = 0;
            foreach (var topic in topics)
            {
                var subtopics = await _syllabusRepo.GetSubtopicsByTopicIdAsync(topic.Id, userId, false, ct);
                var subtopicIds = subtopics.Select(s => s.Id).ToList();
                var map = await _syllabusProgressRepo.GetSubtopicProgressMapAsync(userId, subtopicIds, ct);
                total += map.Values.Count(p => p.IsCompleted && p.CompletedAtUtc.HasValue &&
                    DateOnly.FromDateTime(p.CompletedAtUtc.Value) >= startDate &&
                    DateOnly.FromDateTime(p.CompletedAtUtc.Value) <= endDate);
            }
            return total;
        }

        return 0m;
    }

    private async Task<List<SyllabusTopic>> GetTopicsForSubjectAsync(ulong subjectId, ulong userId, CancellationToken ct)
    {
        var modules = await _syllabusRepo.GetModulesBySubjectIdAsync(subjectId, userId, false, ct);
        var result = new List<SyllabusTopic>();
        foreach (var mod in modules)
        {
            var topics = await _syllabusRepo.GetTopicsByModuleIdAsync(mod.Id, userId, false, ct);
            result.AddRange(topics);
        }
        return result;
    }

    private async Task<TimeZoneInfo> ResolveTimeZoneAsync(ulong userId, CancellationToken ct)
    {
        try
        {
            var profile = await _userProfileRepo.GetByUserIdAsync(userId, ct);
            if (!string.IsNullOrWhiteSpace(profile?.Timezone))
            {
                return TimeZoneInfo.FindSystemTimeZoneById(profile.Timezone);
            }
        }
        catch
        {
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}
