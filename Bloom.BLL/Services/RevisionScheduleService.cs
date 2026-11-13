using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Enums;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Bloom.Core.Exceptions.ValidationException;

namespace Bloom.BLL.Services;

public class RevisionScheduleService : IRevisionScheduleService
{
    private readonly IRevisionScheduleRepository _scheduleRepo;
    private readonly IRevisionAttemptRepository _attemptRepo;
    private readonly IRevisionPolicyRepository _policyRepo;
    private readonly ISyllabusRepository _syllabusRepo;
    private readonly IActivityCategoryRepository _categoryRepo;
    private readonly IActivityRecordRepository _activityRepo;
    private readonly IStudySessionDetailRepository _studyDetailRepo;
    private readonly IUserProfileRepository _userProfileRepo;
    private readonly IUserActivityLockRepository _userLockRepo;
    private readonly IValidator<RecordRevisionAttemptRequest> _attemptValidator;
    private readonly IValidator<RescheduleRevisionRequest> _rescheduleValidator;
    private readonly ILogger<RevisionScheduleService> _logger;

    public RevisionScheduleService(
        IRevisionScheduleRepository scheduleRepo,
        IRevisionAttemptRepository attemptRepo,
        IRevisionPolicyRepository policyRepo,
        ISyllabusRepository syllabusRepo,
        IActivityCategoryRepository categoryRepo,
        IActivityRecordRepository activityRepo,
        IStudySessionDetailRepository studyDetailRepo,
        IUserProfileRepository userProfileRepo,
        IUserActivityLockRepository userLockRepo,
        IValidator<RecordRevisionAttemptRequest> attemptValidator,
        IValidator<RescheduleRevisionRequest> rescheduleValidator,
        ILogger<RevisionScheduleService> logger)
    {
        _scheduleRepo = scheduleRepo ?? throw new ArgumentNullException(nameof(scheduleRepo));
        _attemptRepo = attemptRepo ?? throw new ArgumentNullException(nameof(attemptRepo));
        _policyRepo = policyRepo ?? throw new ArgumentNullException(nameof(policyRepo));
        _syllabusRepo = syllabusRepo ?? throw new ArgumentNullException(nameof(syllabusRepo));
        _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        _activityRepo = activityRepo ?? throw new ArgumentNullException(nameof(activityRepo));
        _studyDetailRepo = studyDetailRepo ?? throw new ArgumentNullException(nameof(studyDetailRepo));
        _userProfileRepo = userProfileRepo ?? throw new ArgumentNullException(nameof(userProfileRepo));
        _userLockRepo = userLockRepo ?? throw new ArgumentNullException(nameof(userLockRepo));
        _attemptValidator = attemptValidator ?? throw new ArgumentNullException(nameof(attemptValidator));
        _rescheduleValidator = rescheduleValidator ?? throw new ArgumentNullException(nameof(rescheduleValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RevisionScheduleDto?> GetScheduleByIdAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepo.GetScheduleByIdAsync(id, userId, null, null, ct);
        if (schedule == null) return null;

        var cache = new Dictionary<ulong, TopicDetails>();
        var details = await GetTopicDetailsAsync(schedule.TopicId, userId, cache, ct);
        var policy = await _policyRepo.GetPolicyByIdAsync(schedule.RevisionPolicyId, userId, ct);

        return MapToDto(schedule, details.TopicName, details.SubjectId, details.SubjectName, policy?.PolicyName ?? "Standard");
    }

    public async Task<RevisionDashboardSummaryDto> GetDashboardSummaryAsync(ulong userId, DateOnly? date = null, CancellationToken ct = default)
    {
        var tz = await ResolveTimeZoneAsync(userId, ct);
        var today = date ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));

        // Reconcile any completed topics that might be missing initial revision schedules (self-healing recovery)
        await ReconcileMissingInitialSchedulesAsync(userId, ct);

        // Sync statuses
        await _scheduleRepo.BatchUpdateStatusesAsync(userId, today, ct);

        // Fetch due and overdue
        var dueAndOverdue = await _scheduleRepo.GetDueAndOverdueSchedulesAsync(userId, today, ct);
        var upcoming = await _scheduleRepo.GetUpcomingSchedulesAsync(userId, today.AddDays(1), 15, ct);

        var policies = await _policyRepo.GetPoliciesAsync(userId, true, ct);
        var policyMap = policies.ToDictionary(p => p.Id, p => p.PolicyName);
        var topicCache = new Dictionary<ulong, TopicDetails>();

        var dueTodayList = new List<RevisionScheduleDto>();
        var overdueList = new List<RevisionScheduleDto>();

        foreach (var s in dueAndOverdue)
        {
            var details = await GetTopicDetailsAsync(s.TopicId, userId, topicCache, ct);
            policyMap.TryGetValue(s.RevisionPolicyId, out var polName);

            var dto = MapToDto(s, details.TopicName, details.SubjectId, details.SubjectName, polName ?? "Standard");
            if (s.ScheduledDate < today || string.Equals(s.Status, "Overdue", StringComparison.OrdinalIgnoreCase))
            {
                overdueList.Add(dto);
            }
            else
            {
                dueTodayList.Add(dto);
            }
        }

        var upcomingList = new List<RevisionScheduleDto>();
        foreach (var s in upcoming)
        {
            var details = await GetTopicDetailsAsync(s.TopicId, userId, topicCache, ct);
            policyMap.TryGetValue(s.RevisionPolicyId, out var polName);
            upcomingList.Add(MapToDto(s, details.TopicName, details.SubjectId, details.SubjectName, polName ?? "Standard"));
        }

        var completedCount = await _attemptRepo.GetCompletedAttemptsCountAsync(userId, DateOnly.MinValue, DateOnly.MaxValue, null, null, ct);

        return new RevisionDashboardSummaryDto
        {
            DueTodayCount = dueTodayList.Count,
            OverdueCount = overdueList.Count,
            UpcomingCount = upcomingList.Count,
            CompletedTotalCount = completedCount,
            DueToday = dueTodayList,
            Overdue = overdueList,
            Upcoming = upcomingList
        };
    }

    public async Task<IReadOnlyList<RevisionScheduleDto>> GetUpcomingSchedulesAsync(ulong userId, int limit = 30, CancellationToken ct = default)
    {
        var tz = await ResolveTimeZoneAsync(userId, ct);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
        var schedules = await _scheduleRepo.GetUpcomingSchedulesAsync(userId, today, limit, ct);

        var policies = await _policyRepo.GetPoliciesAsync(userId, true, ct);
        var policyMap = policies.ToDictionary(p => p.Id, p => p.PolicyName);
        var topicCache = new Dictionary<ulong, TopicDetails>();

        var list = new List<RevisionScheduleDto>();
        foreach (var s in schedules)
        {
            var details = await GetTopicDetailsAsync(s.TopicId, userId, topicCache, ct);
            policyMap.TryGetValue(s.RevisionPolicyId, out var polName);
            list.Add(MapToDto(s, details.TopicName, details.SubjectId, details.SubjectName, polName ?? "Standard"));
        }
        return list;
    }

    public async Task<IReadOnlyList<RevisionScheduleDto>> GetSchedulesForRangeAsync(ulong userId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        var schedules = await _scheduleRepo.GetSchedulesByDateRangeAsync(userId, startDate, endDate, ct);

        var policies = await _policyRepo.GetPoliciesAsync(userId, true, ct);
        var policyMap = policies.ToDictionary(p => p.Id, p => p.PolicyName);
        var topicCache = new Dictionary<ulong, TopicDetails>();

        var list = new List<RevisionScheduleDto>();
        foreach (var s in schedules)
        {
            var details = await GetTopicDetailsAsync(s.TopicId, userId, topicCache, ct);
            policyMap.TryGetValue(s.RevisionPolicyId, out var polName);
            list.Add(MapToDto(s, details.TopicName, details.SubjectId, details.SubjectName, polName ?? "Standard"));
        }
        return list;
    }

    public async Task<RevisionScheduleDto> ScheduleInitialRevisionForTopicAsync(
        ulong userId,
        ulong topicId,
        ulong? policyId = null,
        DateOnly? fromDate = null,
        CancellationToken ct = default)
    {
        var topic = await _syllabusRepo.GetTopicByIdAsync(topicId, userId, ct);
        if (topic == null)
        {
            throw new NotFoundException($"Topic with ID {topicId} not found.");
        }

        return await _userLockRepo.ExecuteWithUserLockAsync(userId, async (conn, tx) =>
        {
            // Check if an active schedule already exists within locked transaction
            var existing = await _scheduleRepo.GetActiveScheduleForTopicAsync(userId, topicId, conn, tx, ct);
            if (existing != null)
            {
                var topicCache = new Dictionary<ulong, TopicDetails>();
                var det = await GetTopicDetailsAsync(topicId, userId, topicCache, ct);
                var pol = await _policyRepo.GetPolicyByIdAsync(existing.RevisionPolicyId, userId, ct);
                return MapToDto(existing, det.TopicName, det.SubjectId, det.SubjectName, pol?.PolicyName ?? "Standard");
            }

            // Resolve policy
            RevisionPolicy? policy = null;
            if (policyId.HasValue)
            {
                policy = await _policyRepo.GetPolicyByIdAsync(policyId.Value, userId, ct);
            }
            if (policy == null)
            {
                policy = await _policyRepo.GetDefaultSystemPolicyAsync(ct);
            }
            if (policy == null)
            {
                var all = await _policyRepo.GetPoliciesAsync(userId, false, ct);
                policy = all?.FirstOrDefault();
            }

            var intervals = policy?.GetIntervals() ?? new List<int> { 1, 3, 7, 14, 30 };
            var firstInterval = intervals.Count > 0 ? intervals[0] : 1;

            var tz = await ResolveTimeZoneAsync(userId, ct);
            var baseDate = fromDate ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
            var scheduledDate = baseDate.AddDays(firstInterval);

            var schedule = new RevisionSchedule
            {
                UserId = userId,
                TopicId = topicId,
                RevisionPolicyId = policy?.Id ?? 1,
                StepNumber = 1,
                ScheduledDate = scheduledDate,
                Status = scheduledDate <= baseDate ? "DueToday" : "Upcoming",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            var created = await _scheduleRepo.CreateScheduleAsync(schedule, conn, tx, ct);
            _logger.LogInformation("Scheduled Step 1 revision for user {UserId}, topic {TopicId} on {ScheduledDate}", userId, topicId, scheduledDate);

            var detailsCache = new Dictionary<ulong, TopicDetails>();
            var details = await GetTopicDetailsAsync(topicId, userId, detailsCache, ct);
            return MapToDto(created, details.TopicName, details.SubjectId, details.SubjectName, policy?.PolicyName ?? "Standard");
        }, ct);
    }

    public async Task<RevisionAttemptDto> RecordAttemptAsync(
        ulong userId,
        RecordRevisionAttemptRequest request,
        CancellationToken ct = default)
    {
        var valResult = await _attemptValidator.ValidateAsync(request, ct);
        if (!valResult.IsValid)
        {
            var errors = valResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var topic = await _syllabusRepo.GetTopicByIdAsync(request.TopicId, userId, ct);
        if (topic == null)
        {
            throw new NotFoundException($"Topic with ID {request.TopicId} not found.");
        }

        var tz = await ResolveTimeZoneAsync(userId, ct);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));

        return await _userLockRepo.ExecuteWithUserLockAsync(userId, async (conn, tx) =>
        {
            RevisionSchedule? currentSchedule = null;
            if (request.RevisionScheduleId.HasValue)
            {
                currentSchedule = await _scheduleRepo.GetScheduleByIdAsync(request.RevisionScheduleId.Value, userId, conn, tx, ct);
                if (currentSchedule == null)
                {
                    throw new NotFoundException($"Revision schedule with ID {request.RevisionScheduleId.Value} not found.");
                }
            }
            else
            {
                currentSchedule = await _scheduleRepo.GetActiveScheduleForTopicAsync(userId, request.TopicId, conn, tx, ct);
            }

            // Reject duplicate or invalid state completion requests
            if (currentSchedule != null)
            {
                if (string.Equals(currentSchedule.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ConflictException("This revision schedule has already been completed.");
                }
                if (string.Equals(currentSchedule.Status, "Skipped", StringComparison.OrdinalIgnoreCase) || currentSchedule.IsDeleted)
                {
                    throw new ConflictException("This revision schedule is not in an active state.");
                }
            }

            // Resolve policy
            var policyId = currentSchedule?.RevisionPolicyId;
            RevisionPolicy? policy = null;
            if (policyId.HasValue)
            {
                policy = await _policyRepo.GetPolicyByIdAsync(policyId.Value, userId, ct);
            }
            policy ??= await _policyRepo.GetDefaultSystemPolicyAsync(ct);
            var intervals = policy?.GetIntervals() ?? new List<int> { 1, 3, 7, 14, 30 };

            int currentStep = currentSchedule?.StepNumber ?? 1;

            // Mark current schedule as Completed if it exists
            if (currentSchedule != null)
            {
                currentSchedule.Status = "Completed";
                currentSchedule.CompletedAtUtc = DateTime.UtcNow;
                currentSchedule.UpdatedAtUtc = DateTime.UtcNow;
                await _scheduleRepo.UpdateScheduleAsync(currentSchedule, conn, tx, ct);
            }

            // Calculate next scheduled date based on UnderstandingLevel
            int? nextStep = null;
            DateOnly? nextDate = null;

            var understanding = request.UnderstandingLevel.Trim();
            if (string.Equals(understanding, UnderstandingLevel.Difficult.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                // Reset to step 1 (1 day)
                nextStep = 1;
                nextDate = today.AddDays(1);
            }
            else if (string.Equals(understanding, UnderstandingLevel.Partial.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                // Repeat current step or half interval
                nextStep = currentStep;
                var currentInterval = intervals.ElementAtOrDefault(currentStep - 1);
                var retryDays = Math.Max(1, currentInterval > 0 ? currentInterval / 2 : 1);
                nextDate = today.AddDays(retryDays);
            }
            else if (string.Equals(understanding, UnderstandingLevel.Good.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                // Advance to next step
                if (currentStep < intervals.Count)
                {
                    nextStep = currentStep + 1;
                    var nextInterval = intervals[nextStep.Value - 1];
                    nextDate = today.AddDays(nextInterval);
                }
            }
            else if (string.Equals(understanding, UnderstandingLevel.Mastered.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                // Skip a step or advance to completion
                if (currentStep + 1 < intervals.Count)
                {
                    nextStep = currentStep + 2;
                    var nextInterval = intervals[nextStep.Value - 1];
                    nextDate = today.AddDays(nextInterval);
                }
            }

            // Create next revision schedule if scheduled
            if (nextDate.HasValue && nextStep.HasValue)
            {
                var nextSchedule = new RevisionSchedule
                {
                    UserId = userId,
                    TopicId = request.TopicId,
                    RevisionPolicyId = policy?.Id ?? 1,
                    StepNumber = nextStep.Value,
                    ScheduledDate = nextDate.Value,
                    Status = nextDate.Value <= today ? "DueToday" : "Upcoming",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                await _scheduleRepo.CreateScheduleAsync(nextSchedule, conn, tx, ct);
                _logger.LogInformation("Scheduled next revision Step {Step} for user {UserId}, topic {TopicId} on {ScheduledDate}",
                    nextStep.Value, userId, request.TopicId, nextDate.Value);
            }

            // Optional Activity Recording
            ulong? linkedActivityId = null;
            if (request.CreateActivityRecord && request.MinutesSpent.HasValue && request.MinutesSpent.Value > 0)
            {
                try
                {
                    var categories = await _categoryRepo.GetCategoriesAsync(userId, true, ct);
                    var revCategory = categories.FirstOrDefault(c =>
                        string.Equals(c.CategoryCode, "REV", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(c.CategoryCode, "CAT_STUDY", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(c.CategoryGroup, "CAT Study", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(c.CategoryGroup, "Study", StringComparison.OrdinalIgnoreCase))
                        ?? categories.FirstOrDefault();

                    if (revCategory != null)
                    {
                        var now = DateTime.UtcNow;
                        var activity = new ActivityRecord
                        {
                            UserId = userId,
                            ActivityCategoryId = revCategory.Id,
                            ActivityTitle = $"Revision: {topic.Name}",
                            Description = request.Notes,
                            StartAtUtc = now.AddMinutes(-request.MinutesSpent.Value),
                            EndAtUtc = now,
                            TimezoneId = tz.Id,
                            ActivityStatus = "Completed",
                            EntrySource = "RevisionEngine",
                            IsPlanned = false,
                            CreatedAtUtc = now,
                            UpdatedAtUtc = now
                        };

                        var createdActivity = await _activityRepo.CreateRecordAsync(activity, conn, tx, ct);
                        linkedActivityId = createdActivity.Id;

                        var studyDetail = new StudySessionDetail
                        {
                            ActivityId = createdActivity.Id,
                            TopicId = request.TopicId,
                            StudyMode = "Revision",
                            DifficultyLevel = request.UnderstandingLevel,
                            SessionNotes = request.Notes
                        };
                        await _studyDetailRepo.UpsertAsync(studyDetail, conn, tx, ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create automatic activity record for revision attempt user {UserId}, topic {TopicId}", userId, request.TopicId);
                }
            }

            // Create RevisionAttempt record
            var attempt = new RevisionAttempt
            {
                RevisionScheduleId = currentSchedule?.Id,
                UserId = userId,
                TopicId = request.TopicId,
                RevisedAtUtc = DateTime.UtcNow,
                RevisionDate = today,
                UnderstandingLevel = request.UnderstandingLevel,
                ConfidenceRating = request.ConfidenceRating,
                Notes = request.Notes?.Trim(),
                LinkedActivityRecordId = linkedActivityId,
                NextScheduledDate = nextDate,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createdAttempt = await _attemptRepo.CreateAttemptAsync(attempt, conn, tx, ct);
            _logger.LogInformation("Recorded revision attempt {AttemptId} for user {UserId}, topic {TopicId} (Level={Level})",
                createdAttempt.Id, userId, request.TopicId, request.UnderstandingLevel);

            return new RevisionAttemptDto
            {
                Id = createdAttempt.Id,
                RevisionScheduleId = createdAttempt.RevisionScheduleId,
                UserId = createdAttempt.UserId,
                TopicId = createdAttempt.TopicId,
                TopicName = topic.Name,
                RevisedAtUtc = createdAttempt.RevisedAtUtc,
                RevisionDate = createdAttempt.RevisionDate,
                UnderstandingLevel = createdAttempt.UnderstandingLevel,
                ConfidenceRating = createdAttempt.ConfidenceRating,
                Notes = createdAttempt.Notes,
                LinkedActivityRecordId = createdAttempt.LinkedActivityRecordId,
                NextScheduledDate = createdAttempt.NextScheduledDate,
                CreatedAtUtc = createdAttempt.CreatedAtUtc
            };
        }, ct);
    }

    public async Task<RevisionScheduleDto> RescheduleAsync(ulong id, ulong userId, RescheduleRevisionRequest request, CancellationToken ct = default)
    {
        var valResult = await _rescheduleValidator.ValidateAsync(request, ct);
        if (!valResult.IsValid)
        {
            var errors = valResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var schedule = await _scheduleRepo.GetScheduleByIdAsync(id, userId, null, null, ct);
        if (schedule == null)
        {
            throw new NotFoundException($"Revision schedule with ID {id} not found.");
        }

        var tz = await ResolveTimeZoneAsync(userId, ct);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));

        schedule.ScheduledDate = request.NewDate;
        schedule.Status = request.NewDate <= today ? "DueToday" : "Upcoming";
        schedule.UpdatedAtUtc = DateTime.UtcNow;

        await _scheduleRepo.UpdateScheduleAsync(schedule, null, null, ct);
        _logger.LogInformation("User {UserId} rescheduled revision {ScheduleId} to {NewDate}", userId, id, request.NewDate);

        var topicCache = new Dictionary<ulong, TopicDetails>();
        var details = await GetTopicDetailsAsync(schedule.TopicId, userId, topicCache, ct);
        var policy = await _policyRepo.GetPolicyByIdAsync(schedule.RevisionPolicyId, userId, ct);

        return MapToDto(schedule, details.TopicName, details.SubjectId, details.SubjectName, policy?.PolicyName ?? "Standard");
    }

    public async Task<RevisionScheduleDto> SkipAsync(ulong id, ulong userId, string? reason, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepo.GetScheduleByIdAsync(id, userId, null, null, ct);
        if (schedule == null)
        {
            throw new NotFoundException($"Revision schedule with ID {id} not found.");
        }

        schedule.Status = "Skipped";
        schedule.UpdatedAtUtc = DateTime.UtcNow;

        await _scheduleRepo.UpdateScheduleAsync(schedule, null, null, ct);
        _logger.LogInformation("User {UserId} skipped revision {ScheduleId}. Reason: {Reason}", userId, id, reason);

        var topicCache = new Dictionary<ulong, TopicDetails>();
        var details = await GetTopicDetailsAsync(schedule.TopicId, userId, topicCache, ct);
        var policy = await _policyRepo.GetPolicyByIdAsync(schedule.RevisionPolicyId, userId, ct);

        return MapToDto(schedule, details.TopicName, details.SubjectId, details.SubjectName, policy?.PolicyName ?? "Standard");
    }

    public async Task<IReadOnlyList<RevisionAttemptDto>> GetAttemptsForTopicAsync(ulong userId, ulong topicId, CancellationToken ct = default)
    {
        var attempts = await _attemptRepo.GetAttemptsByTopicIdAsync(userId, topicId, ct);
        var topic = await _syllabusRepo.GetTopicByIdAsync(topicId, userId, ct);
        var topicName = topic?.Name ?? "Unknown Topic";

        return attempts.Select(a => new RevisionAttemptDto
        {
            Id = a.Id,
            RevisionScheduleId = a.RevisionScheduleId,
            UserId = a.UserId,
            TopicId = a.TopicId,
            TopicName = topicName,
            RevisedAtUtc = a.RevisedAtUtc,
            RevisionDate = a.RevisionDate,
            UnderstandingLevel = a.UnderstandingLevel,
            ConfidenceRating = a.ConfidenceRating,
            Notes = a.Notes,
            LinkedActivityRecordId = a.LinkedActivityRecordId,
            NextScheduledDate = a.NextScheduledDate,
            CreatedAtUtc = a.CreatedAtUtc
        }).ToList();
    }

    public async Task<IReadOnlyList<RevisionAttemptDto>> GetRecentAttemptsAsync(ulong userId, int limit = 50, CancellationToken ct = default)
    {
        var attempts = await _attemptRepo.GetRecentAttemptsAsync(userId, limit, ct);
        var topicCache = new Dictionary<ulong, TopicDetails>();

        var list = new List<RevisionAttemptDto>();
        foreach (var a in attempts)
        {
            var details = await GetTopicDetailsAsync(a.TopicId, userId, topicCache, ct);
            list.Add(new RevisionAttemptDto
            {
                Id = a.Id,
                RevisionScheduleId = a.RevisionScheduleId,
                UserId = a.UserId,
                TopicId = a.TopicId,
                TopicName = details.TopicName,
                RevisedAtUtc = a.RevisedAtUtc,
                RevisionDate = a.RevisionDate,
                UnderstandingLevel = a.UnderstandingLevel,
                ConfidenceRating = a.ConfidenceRating,
                Notes = a.Notes,
                LinkedActivityRecordId = a.LinkedActivityRecordId,
                NextScheduledDate = a.NextScheduledDate,
                CreatedAtUtc = a.CreatedAtUtc
            });
        }
        return list;
    }

    public async Task<int> BatchUpdateStatusesAsync(ulong userId, DateOnly today, CancellationToken ct = default)
    {
        return await _scheduleRepo.BatchUpdateStatusesAsync(userId, today, ct);
    }

    public async Task<int> ReconcileMissingInitialSchedulesAsync(ulong? userId = null, CancellationToken ct = default)
    {
        var missing = await _scheduleRepo.GetCompletedTopicsMissingSchedulesAsync(userId, ct);
        if (missing.Count == 0) return 0;

        int reconciledCount = 0;
        foreach (var (missingUserId, missingTopicId) in missing)
        {
            try
            {
                await ScheduleInitialRevisionForTopicAsync(missingUserId, missingTopicId, null, null, ct);
                reconciledCount++;
                _logger.LogInformation("Reconciled missing revision schedule for user {UserId}, topic {TopicId}", missingUserId, missingTopicId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reconcile missing revision schedule for user {UserId}, topic {TopicId}", missingUserId, missingTopicId);
            }
        }

        return reconciledCount;
    }

    private async Task<TopicDetails> GetTopicDetailsAsync(ulong topicId, ulong userId, Dictionary<ulong, TopicDetails> cache, CancellationToken ct)
    {
        if (cache.TryGetValue(topicId, out var cached)) return cached;

        var topic = await _syllabusRepo.GetTopicByIdAsync(topicId, userId, ct);
        if (topic == null)
        {
            var missing = new TopicDetails("Unknown Topic", null, null);
            cache[topicId] = missing;
            return missing;
        }

        var module = await _syllabusRepo.GetModuleByIdAsync(topic.ModuleId, userId, ct);
        var subject = module != null ? await _syllabusRepo.GetSubjectByIdAsync(module.SubjectId, userId, ct) : null;

        var details = new TopicDetails(topic.Name, subject?.Id, subject?.Name);
        cache[topicId] = details;
        return details;
    }

    private static RevisionScheduleDto MapToDto(
        RevisionSchedule entity,
        string topicName,
        ulong? subjectId,
        string? subjectName,
        string policyName)
    {
        return new RevisionScheduleDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            TopicId = entity.TopicId,
            TopicName = topicName,
            SubjectId = subjectId,
            SubjectName = subjectName,
            RevisionPolicyId = entity.RevisionPolicyId,
            PolicyName = policyName,
            StepNumber = entity.StepNumber,
            ScheduledDate = entity.ScheduledDate,
            Status = entity.Status,
            CompletedAtUtc = entity.CompletedAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
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

    private record TopicDetails(string TopicName, ulong? SubjectId, string? SubjectName);
}
