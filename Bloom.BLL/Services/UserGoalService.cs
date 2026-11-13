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

public class UserGoalService : IUserGoalService
{
    private readonly IUserGoalRepository _goalRepo;
    private readonly IGoalProgressService _progressService;
    private readonly ISyllabusRepository _syllabusRepo;
    private readonly IUserProfileRepository _userProfileRepo;
    private readonly IValidator<CreateGoalDto> _createValidator;
    private readonly IValidator<UpdateGoalDto> _updateValidator;
    private readonly ILogger<UserGoalService> _logger;

    public UserGoalService(
        IUserGoalRepository goalRepo,
        IGoalProgressService progressService,
        ISyllabusRepository syllabusRepo,
        IUserProfileRepository userProfileRepo,
        IValidator<CreateGoalDto> createValidator,
        IValidator<UpdateGoalDto> updateValidator,
        ILogger<UserGoalService> logger)
    {
        _goalRepo = goalRepo ?? throw new ArgumentNullException(nameof(goalRepo));
        _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
        _syllabusRepo = syllabusRepo ?? throw new ArgumentNullException(nameof(syllabusRepo));
        _userProfileRepo = userProfileRepo ?? throw new ArgumentNullException(nameof(userProfileRepo));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<UserGoalDto>> GetGoalsAsync(ulong userId, string? period = null, string? status = null, CancellationToken ct = default)
    {
        var goals = await _goalRepo.GetGoalsAsync(userId, period, status, false, ct);
        var tz = await ResolveTimeZoneAsync(userId, ct);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));

        var dtos = new List<UserGoalDto>();
        foreach (var g in goals)
        {
            var dto = await EnrichGoalDtoAsync(g, userId, today, ct);
            dtos.Add(dto);
        }

        return dtos;
    }

    public async Task<UserGoalDto> GetGoalByIdAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        var goal = await _goalRepo.GetGoalByIdAsync(id, userId, ct);
        if (goal == null)
        {
            throw new NotFoundException($"Goal with ID {id} not found.");
        }

        var tz = await ResolveTimeZoneAsync(userId, ct);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
        return await EnrichGoalDtoAsync(goal, userId, today, ct);
    }

    public async Task<GoalDashboardSummaryDto> GetDashboardAsync(ulong userId, string period, DateOnly? date = null, CancellationToken ct = default)
    {
        var tz = await ResolveTimeZoneAsync(userId, ct);
        var referenceDate = date ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));

        DateOnly startDate;
        DateOnly endDate;

        var normalizedPeriod = period.Trim();
        if (string.Equals(normalizedPeriod, GoalPeriod.Daily.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            startDate = referenceDate;
            endDate = referenceDate;
            normalizedPeriod = "Daily";
        }
        else if (string.Equals(normalizedPeriod, GoalPeriod.Weekly.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            int diff = (7 + (referenceDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            startDate = referenceDate.AddDays(-diff);
            endDate = startDate.AddDays(6);
            normalizedPeriod = "Weekly";
        }
        else if (string.Equals(normalizedPeriod, GoalPeriod.Monthly.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            startDate = new DateOnly(referenceDate.Year, referenceDate.Month, 1);
            endDate = startDate.AddMonths(1).AddDays(-1);
            normalizedPeriod = "Monthly";
        }
        else
        {
            startDate = referenceDate;
            endDate = referenceDate.AddDays(30);
            normalizedPeriod = "Custom";
        }

        var goals = await _goalRepo.GetActiveGoalsForRangeAsync(userId, startDate, endDate, normalizedPeriod, ct);
        var goalDtos = new List<UserGoalDto>();

        int achievedCount = 0;
        decimal totalPercentageSum = 0m;

        foreach (var g in goals)
        {
            var dto = await EnrichGoalDtoAsync(g, userId, referenceDate, ct);
            goalDtos.Add(dto);

            if (dto.IsAchieved) achievedCount++;
            totalPercentageSum += Math.Min(dto.ProgressPercentage, 100m);
        }

        var avgPercentage = goalDtos.Count > 0 ? Math.Round(totalPercentageSum / goalDtos.Count, 1) : 0m;

        return new GoalDashboardSummaryDto
        {
            Period = normalizedPeriod,
            StartDate = startDate,
            EndDate = endDate,
            TotalGoals = goalDtos.Count,
            AchievedGoals = achievedCount,
            AverageProgressPercentage = avgPercentage,
            Goals = goalDtos
        };
    }

    public async Task<UserGoalDto> CreateGoalAsync(ulong userId, CreateGoalDto dto, CancellationToken ct = default)
    {
        var valResult = await _createValidator.ValidateAsync(dto, ct);
        if (!valResult.IsValid)
        {
            var errors = valResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        if (dto.SubjectId.HasValue)
        {
            var subject = await _syllabusRepo.GetSubjectByIdAsync(dto.SubjectId.Value, userId, ct);
            if (subject == null)
            {
                throw new NotFoundException($"Subject with ID {dto.SubjectId.Value} not found.");
            }
        }

        if (dto.TopicId.HasValue)
        {
            var topic = await _syllabusRepo.GetTopicByIdAsync(dto.TopicId.Value, userId, ct);
            if (topic == null)
            {
                throw new NotFoundException($"Topic with ID {dto.TopicId.Value} not found.");
            }
        }

        var goal = new UserGoal
        {
            UserId = userId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            GoalPeriod = dto.GoalPeriod.Trim(),
            TargetMetric = dto.TargetMetric.Trim(),
            TargetValue = dto.TargetValue,
            Unit = dto.Unit.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            SubjectId = dto.SubjectId,
            TopicId = dto.TopicId,
            Status = "Active",
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var created = await _goalRepo.CreateGoalAsync(goal, ct);
        _logger.LogInformation("Created user goal {GoalId} for user {UserId} ({Title})", created.Id, userId, created.Title);

        var tz = await ResolveTimeZoneAsync(userId, ct);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
        return await EnrichGoalDtoAsync(created, userId, today, ct);
    }

    public async Task<UserGoalDto> UpdateGoalAsync(ulong id, ulong userId, UpdateGoalDto dto, CancellationToken ct = default)
    {
        var valResult = await _updateValidator.ValidateAsync(dto, ct);
        if (!valResult.IsValid)
        {
            var errors = valResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var existing = await _goalRepo.GetGoalByIdAsync(id, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Goal with ID {id} not found.");
        }

        if (dto.SubjectId.HasValue)
        {
            var subject = await _syllabusRepo.GetSubjectByIdAsync(dto.SubjectId.Value, userId, ct);
            if (subject == null)
            {
                throw new NotFoundException($"Subject with ID {dto.SubjectId.Value} not found.");
            }
        }

        if (dto.TopicId.HasValue)
        {
            var topic = await _syllabusRepo.GetTopicByIdAsync(dto.TopicId.Value, userId, ct);
            if (topic == null)
            {
                throw new NotFoundException($"Topic with ID {dto.TopicId.Value} not found.");
            }
        }

        existing.Title = dto.Title.Trim();
        existing.Description = dto.Description?.Trim();
        existing.TargetValue = dto.TargetValue;
        existing.Unit = dto.Unit.Trim();
        existing.StartDate = dto.StartDate;
        existing.EndDate = dto.EndDate;
        existing.SubjectId = dto.SubjectId;
        existing.TopicId = dto.TopicId;
        existing.Status = dto.Status.Trim();
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await _goalRepo.UpdateGoalAsync(existing, ct);
        _logger.LogInformation("Updated user goal {GoalId} for user {UserId}", id, userId);

        var tz = await ResolveTimeZoneAsync(userId, ct);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
        return await EnrichGoalDtoAsync(existing, userId, today, ct);
    }

    public async Task DeleteGoalAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        var existing = await _goalRepo.GetGoalByIdAsync(id, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Goal with ID {id} not found.");
        }

        await _goalRepo.SoftDeleteGoalAsync(id, userId, ct);
        _logger.LogInformation("Soft-deleted user goal {GoalId} for user {UserId}", id, userId);
    }

    private async Task<UserGoalDto> EnrichGoalDtoAsync(UserGoal goal, ulong userId, DateOnly today, CancellationToken ct)
    {
        string? subjName = null;
        if (goal.SubjectId.HasValue)
        {
            var subj = await _syllabusRepo.GetSubjectByIdAsync(goal.SubjectId.Value, userId, ct);
            subjName = subj?.Name;
        }

        string? topicName = null;
        if (goal.TopicId.HasValue)
        {
            var topic = await _syllabusRepo.GetTopicByIdAsync(goal.TopicId.Value, userId, ct);
            topicName = topic?.Name;
        }

        var currentValue = await _progressService.CalculateCurrentProgressAsync(
            userId,
            goal.TargetMetric,
            goal.StartDate,
            goal.EndDate,
            goal.SubjectId,
            goal.TopicId,
            ct);

        decimal progressPercentage = 0m;
        if (goal.TargetValue > 0)
        {
            progressPercentage = Math.Round((currentValue / goal.TargetValue) * 100m, 1);
        }

        bool isAchieved = currentValue >= goal.TargetValue;
        int daysRemaining = Math.Max(0, goal.EndDate.DayNumber - today.DayNumber);

        return new UserGoalDto
        {
            Id = goal.Id,
            UserId = goal.UserId,
            Title = goal.Title,
            Description = goal.Description,
            GoalPeriod = goal.GoalPeriod,
            TargetMetric = goal.TargetMetric,
            TargetValue = goal.TargetValue,
            Unit = goal.Unit,
            StartDate = goal.StartDate,
            EndDate = goal.EndDate,
            SubjectId = goal.SubjectId,
            SubjectName = subjName,
            TopicId = goal.TopicId,
            TopicName = topicName,
            Status = goal.Status,
            CurrentValue = currentValue,
            ProgressPercentage = progressPercentage,
            IsAchieved = isAchieved,
            DaysRemaining = daysRemaining,
            CreatedAtUtc = goal.CreatedAtUtc,
            UpdatedAtUtc = goal.UpdatedAtUtc
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
}

