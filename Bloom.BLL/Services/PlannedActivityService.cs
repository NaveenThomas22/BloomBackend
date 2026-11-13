using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Bloom.Core.Exceptions.ValidationException;

namespace Bloom.BLL.Services;

public class PlannedActivityService : IPlannedActivityService
{
    private readonly IPlannedActivityRepository _plannedRepo;
    private readonly IActivityCategoryRepository _categoryRepo;
    private readonly IActivityRecordService _activityRecordService;
    private readonly ISyllabusRepository _syllabusRepo;
    private readonly IUserProfileRepository _userProfileRepo;
    private readonly IValidator<CreatePlannedActivityDto> _createValidator;
    private readonly IValidator<UpdatePlannedActivityDto> _updateValidator;
    private readonly ILogger<PlannedActivityService> _logger;

    public PlannedActivityService(
        IPlannedActivityRepository plannedRepo,
        IActivityCategoryRepository categoryRepo,
        IActivityRecordService activityRecordService,
        ISyllabusRepository syllabusRepo,
        IUserProfileRepository userProfileRepo,
        IValidator<CreatePlannedActivityDto> createValidator,
        IValidator<UpdatePlannedActivityDto> updateValidator,
        ILogger<PlannedActivityService> logger)
    {
        _plannedRepo = plannedRepo ?? throw new ArgumentNullException(nameof(plannedRepo));
        _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        _activityRecordService = activityRecordService ?? throw new ArgumentNullException(nameof(activityRecordService));
        _syllabusRepo = syllabusRepo ?? throw new ArgumentNullException(nameof(syllabusRepo));
        _userProfileRepo = userProfileRepo ?? throw new ArgumentNullException(nameof(userProfileRepo));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<PlannedActivityDto>> GetPlannedActivitiesAsync(
        ulong userId, 
        DateOnly targetDate, 
        string? timezoneId = null, 
        CancellationToken ct = default)
    {
        var tz = await ResolveTimeZoneAsync(userId, timezoneId, ct);
        var localStart = targetDate.ToDateTime(TimeOnly.MinValue);
        var localEnd = targetDate.ToDateTime(TimeOnly.MaxValue);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, tz);

        var plans = await _plannedRepo.GetPlannedByIntervalAsync(userId, startUtc, endUtc, ct);
        var categories = await _categoryRepo.GetCategoriesAsync(userId, true, ct);
        var catMap = categories.ToDictionary(c => c.Id, c => c);

        var topicIds = plans.Where(p => p.LinkedTopicId.HasValue).Select(p => p.LinkedTopicId!.Value).Distinct().ToList();
        var topicMap = new Dictionary<ulong, SyllabusTopic>();
        foreach (var tid in topicIds)
        {
            var t = await _syllabusRepo.GetTopicByIdAsync(tid, userId, ct);
            if (t != null) topicMap[tid] = t;
        }

        return plans.Select(p =>
        {
            catMap.TryGetValue(p.ActivityCategoryId, out var cat);
            topicMap.TryGetValue(p.LinkedTopicId ?? 0, out var t);
            return MapToDto(p, cat, t?.Name);
        }).ToList();
    }

    public async Task<PlannedActivityDto> GetPlannedByIdAsync(ulong userId, ulong planId, CancellationToken ct = default)
    {
        var plan = await _plannedRepo.GetByIdAsync(planId, userId, ct);
        if (plan == null)
        {
            throw new NotFoundException($"Planned activity with ID {planId} not found.");
        }

        var cat = await _categoryRepo.GetCategoryByIdAsync(plan.ActivityCategoryId, userId, ct);
        var topic = plan.LinkedTopicId.HasValue ? await _syllabusRepo.GetTopicByIdAsync(plan.LinkedTopicId.Value, userId, ct) : null;

        return MapToDto(plan, cat, topic?.Name);
    }

    public async Task<PlannedActivityDto> CreatePlannedActivityAsync(ulong userId, CreatePlannedActivityDto dto, CancellationToken ct = default)
    {
        var valResult = await _createValidator.ValidateAsync(dto, ct);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var cat = await _categoryRepo.GetCategoryByIdAsync(dto.ActivityCategoryId, userId, ct);
        if (cat == null)
        {
            throw new NotFoundException($"Activity category with ID {dto.ActivityCategoryId} not found.");
        }

        if (dto.LinkedTopicId.HasValue)
        {
            var topic = await _syllabusRepo.GetTopicByIdAsync(dto.LinkedTopicId.Value, userId, ct);
            if (topic == null)
            {
                throw new NotFoundException($"Syllabus topic with ID {dto.LinkedTopicId.Value} not found.");
            }
        }

        var plan = new PlannedActivity
        {
            UserId = userId,
            ActivityCategoryId = dto.ActivityCategoryId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            PlannedStartAtUtc = dto.PlannedStartAtUtc,
            PlannedEndAtUtc = dto.PlannedEndAtUtc,
            TimezoneId = string.IsNullOrWhiteSpace(dto.TimezoneId) ? "Asia/Kolkata" : dto.TimezoneId.Trim(),
            LinkedTopicId = dto.LinkedTopicId,
            PlanStatus = "Scheduled"
        };

        var created = await _plannedRepo.CreatePlannedActivityAsync(plan, ct);
        _logger.LogInformation("User {UserId} created planned activity {PlanId} '{Title}'.", userId, created.Id, created.Title);

        return MapToDto(created, cat, null);
    }

    public async Task<PlannedActivityDto> UpdatePlannedActivityAsync(ulong userId, ulong planId, UpdatePlannedActivityDto dto, CancellationToken ct = default)
    {
        var valResult = await _updateValidator.ValidateAsync(dto, ct);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var existing = await _plannedRepo.GetByIdAsync(planId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Planned activity with ID {planId} not found.");
        }

        var cat = await _categoryRepo.GetCategoryByIdAsync(dto.ActivityCategoryId, userId, ct);
        if (cat == null)
        {
            throw new NotFoundException($"Activity category with ID {dto.ActivityCategoryId} not found.");
        }

        existing.ActivityCategoryId = dto.ActivityCategoryId;
        existing.Title = dto.Title.Trim();
        existing.Description = dto.Description?.Trim();
        existing.PlannedStartAtUtc = dto.PlannedStartAtUtc;
        existing.PlannedEndAtUtc = dto.PlannedEndAtUtc;
        existing.TimezoneId = string.IsNullOrWhiteSpace(dto.TimezoneId) ? existing.TimezoneId : dto.TimezoneId.Trim();
        existing.LinkedTopicId = dto.LinkedTopicId;
        existing.PlanStatus = dto.PlanStatus;

        await _plannedRepo.UpdatePlannedActivityAsync(existing, ct);
        return MapToDto(existing, cat, null);
    }

    public async Task<bool> DeletePlannedActivityAsync(ulong userId, ulong planId, CancellationToken ct = default)
    {
        var existing = await _plannedRepo.GetByIdAsync(planId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Planned activity with ID {planId} not found.");
        }

        return await _plannedRepo.DeletePlannedActivityAsync(planId, userId, ct);
    }

    public async Task<ActivityRecordDto> StartPlannedActivityAsync(ulong userId, ulong planId, CancellationToken ct = default)
    {
        var plan = await _plannedRepo.GetByIdAsync(planId, userId, ct);
        if (plan == null)
        {
            throw new NotFoundException($"Planned activity with ID {planId} not found.");
        }

        // Idempotency: repeated start requests must not create duplicate activities
        if (plan.ActualActivityId.HasValue || plan.PlanStatus == "InProgress" || plan.PlanStatus == "Started" || plan.PlanStatus == "Completed")
        {
            if (plan.ActualActivityId.HasValue)
            {
                return await _activityRecordService.GetRecordByIdAsync(userId, plan.ActualActivityId.Value, ct);
            }
            throw new ConflictException("Planned activity has already been started or completed.");
        }

        var nowUtc = DateTime.UtcNow;

        // Convert plan to actual activity record
        var createDto = new CreateActivityRecordDto
        {
            ActivityCategoryId = plan.ActivityCategoryId,
            ActivityTitle = plan.Title,
            Description = plan.Description,
            StartAtUtc = nowUtc,
            EndAtUtc = null, // Running
            TimezoneId = plan.TimezoneId,
            EntrySource = "PlanConverted",
            IsPlanned = true,
            TopicId = plan.LinkedTopicId
        };

        var actual = await _activityRecordService.CreateRecordAsync(userId, createDto, ct);

        plan.PlanStatus = "Started";
        plan.ActualActivityId = actual.Id;
        await _plannedRepo.UpdatePlannedActivityAsync(plan, ct);

        return actual;
    }

    public async Task<PlannedActivityDto> SkipPlannedActivityAsync(ulong userId, ulong planId, CancellationToken ct = default)
    {
        var plan = await _plannedRepo.GetByIdAsync(planId, userId, ct);
        if (plan == null)
        {
            throw new NotFoundException($"Planned activity with ID {planId} not found.");
        }

        plan.PlanStatus = "Skipped";
        await _plannedRepo.UpdatePlannedActivityAsync(plan, ct);

        var cat = await _categoryRepo.GetCategoryByIdAsync(plan.ActivityCategoryId, userId, ct);
        return MapToDto(plan, cat, null);
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

    private static PlannedActivityDto MapToDto(PlannedActivity p, ActivityCategory? c, string? topicName)
    {
        return new PlannedActivityDto
        {
            Id = p.Id,
            UserId = p.UserId,
            ActivityCategoryId = p.ActivityCategoryId,
            CategoryCode = c?.CategoryCode ?? "OTHER",
            CategoryName = c?.CategoryName ?? "Planned Activity",
            CategoryGroup = c?.CategoryGroup ?? "Other",
            Color = c?.Color ?? "#A8BDA5",
            Icon = c?.Icon ?? "activity",
            Title = p.Title,
            Description = p.Description,
            PlannedStartAtUtc = p.PlannedStartAtUtc,
            PlannedEndAtUtc = p.PlannedEndAtUtc,
            TimezoneId = p.TimezoneId,
            LinkedTopicId = p.LinkedTopicId,
            LinkedTopicName = topicName,
            PlanStatus = p.PlanStatus,
            ActualActivityId = p.ActualActivityId,
            PlannedDurationMinutes = Math.Round(p.PlannedDurationMinutes, 1)
        };
    }
}
