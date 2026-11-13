using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Bloom.Core.Exceptions.ValidationException;

namespace Bloom.BLL.Services;

public class PersonalMilestoneService : IPersonalMilestoneService
{
    private readonly IPersonalMilestoneRepository _milestoneRepository;
    private readonly IValidator<CreateMilestoneRequest> _createValidator;
    private readonly IValidator<UpdateMilestoneRequest> _updateValidator;
    private readonly ILogger<PersonalMilestoneService> _logger;

    public PersonalMilestoneService(
        IPersonalMilestoneRepository milestoneRepository,
        IValidator<CreateMilestoneRequest> createValidator,
        IValidator<UpdateMilestoneRequest> updateValidator,
        ILogger<PersonalMilestoneService> logger)
    {
        _milestoneRepository = milestoneRepository ?? throw new ArgumentNullException(nameof(milestoneRepository));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<PersonalMilestoneDto>> GetMilestonesAsync(ulong userId, string? status = null, CancellationToken ct = default)
    {
        var milestones = await _milestoneRepository.GetMilestonesAsync(userId, status, ct);
        return milestones.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<PersonalMilestoneDto?> GetMilestoneByIdAsync(ulong milestoneId, ulong userId, CancellationToken ct = default)
    {
        var milestone = await _milestoneRepository.GetMilestoneByIdAsync(milestoneId, userId, ct);
        return milestone != null ? MapToDto(milestone) : null;
    }

    public async Task<PersonalMilestoneDto> CreateMilestoneAsync(ulong userId, CreateMilestoneRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.ToDictionary());
        }

        var isAchieved = request.CurrentValue >= request.TargetValue;
        var milestone = new PersonalMilestone
        {
            UserId = userId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Category = request.Category.Trim().ToUpperInvariant(),
            TargetValue = request.TargetValue,
            CurrentValue = request.CurrentValue,
            Unit = request.Unit.Trim(),
            TargetDate = request.TargetDate,
            Status = isAchieved ? "Achieved" : "InProgress",
            AchievedAtUtc = isAchieved ? DateTime.UtcNow : null,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var id = await _milestoneRepository.CreateMilestoneAsync(milestone, ct);
        milestone.Id = id;

        _logger.LogInformation("Milestone {MilestoneId} '{Title}' created for user {UserId}", id, milestone.Title, userId);
        return MapToDto(milestone);
    }

    public async Task<PersonalMilestoneDto?> UpdateMilestoneAsync(ulong milestoneId, ulong userId, UpdateMilestoneRequest request, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.ToDictionary());
        }

        var existing = await _milestoneRepository.GetMilestoneByIdAsync(milestoneId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Milestone with ID {milestoneId} not found.");
        }

        var isNowAchieved = request.CurrentValue >= request.TargetValue || string.Equals(request.Status, "Achieved", StringComparison.OrdinalIgnoreCase);

        existing.Title = request.Title.Trim();
        existing.Description = request.Description?.Trim();
        existing.Category = request.Category.Trim().ToUpperInvariant();
        existing.TargetValue = request.TargetValue;
        existing.CurrentValue = request.CurrentValue;
        existing.Unit = request.Unit.Trim();
        existing.TargetDate = request.TargetDate;
        existing.Status = isNowAchieved ? "Achieved" : request.Status.Trim();
        if (isNowAchieved && existing.AchievedAtUtc == null)
        {
            existing.AchievedAtUtc = DateTime.UtcNow;
        }

        var success = await _milestoneRepository.UpdateMilestoneAsync(existing, ct);
        if (!success)
        {
            throw new InvalidOperationException("Failed to update milestone.");
        }

        _logger.LogInformation("Milestone {MilestoneId} updated for user {UserId}", milestoneId, userId);
        return MapToDto(existing);
    }

    public async Task<bool> DeleteMilestoneAsync(ulong milestoneId, ulong userId, CancellationToken ct = default)
    {
        var existing = await _milestoneRepository.GetMilestoneByIdAsync(milestoneId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Milestone with ID {milestoneId} not found.");
        }

        var success = await _milestoneRepository.DeleteMilestoneAsync(milestoneId, userId, ct);
        _logger.LogInformation("Milestone {MilestoneId} deleted for user {UserId}", milestoneId, userId);
        return success;
    }

    public async Task<MilestoneOverviewDto> GetOverviewAsync(ulong userId, CancellationToken ct = default)
    {
        var milestones = await _milestoneRepository.GetMilestonesAsync(userId, null, ct);
        var dtos = milestones.Select(MapToDto).ToList();

        var achieved = dtos.Count(m => string.Equals(m.Status, "Achieved", StringComparison.OrdinalIgnoreCase));
        var inProgress = dtos.Count(m => string.Equals(m.Status, "InProgress", StringComparison.OrdinalIgnoreCase));
        var avgProgress = dtos.Count > 0 ? Math.Round(dtos.Average(m => m.ProgressPercentage), 1) : 0.0;

        return new MilestoneOverviewDto
        {
            Milestones = dtos.AsReadOnly(),
            TotalMilestones = dtos.Count,
            AchievedCount = achieved,
            InProgressCount = inProgress,
            OverallProgressPercentage = avgProgress
        };
    }

    public async Task<int> SyncMilestoneProgressAsync(ulong userId, CancellationToken ct = default)
    {
        var milestones = await _milestoneRepository.GetMilestonesAsync(userId, "InProgress", ct);
        if (milestones.Count == 0)
        {
            return 0;
        }

        var metrics = await _milestoneRepository.GetUserSystemMetricsAsync(userId, ct);
        var updatedCount = 0;

        foreach (var m in milestones)
        {
            decimal newValue = m.CurrentValue;

            switch (m.Category.ToUpperInvariant())
            {
                case "STUDY_HOURS":
                    newValue = Math.Round(metrics.StudyHours, 1);
                    break;
                case "QUESTIONS_SOLVED":
                    newValue = metrics.QuestionsSolved;
                    break;
                case "TOPICS_MASTERED":
                    newValue = metrics.TopicsMastered;
                    break;
                case "MOCK_TESTS":
                    newValue = metrics.MockTestsCompleted;
                    break;
            }

            if (newValue != m.CurrentValue)
            {
                var isAchieved = newValue >= m.TargetValue;
                var status = isAchieved ? "Achieved" : m.Status;
                DateTime? achievedAt = isAchieved ? DateTime.UtcNow : null;

                await _milestoneRepository.UpdateProgressAsync(m.Id, userId, newValue, status, achievedAt, ct);
                updatedCount++;
            }
        }

        _logger.LogInformation("Synced {UpdatedCount} milestones for user {UserId}", updatedCount, userId);
        return updatedCount;
    }

    private static PersonalMilestoneDto MapToDto(PersonalMilestone m)
    {
        var pct = m.TargetValue > 0
            ? Math.Min(100.0, Math.Round((double)(m.CurrentValue / m.TargetValue) * 100.0, 1))
            : 0.0;

        return new PersonalMilestoneDto
        {
            Id = m.Id,
            UserId = m.UserId,
            Title = m.Title,
            Description = m.Description,
            Category = m.Category,
            TargetValue = m.TargetValue,
            CurrentValue = m.CurrentValue,
            ProgressPercentage = pct,
            Unit = m.Unit,
            TargetDate = m.TargetDate,
            Status = m.Status,
            AchievedAtUtc = m.AchievedAtUtc,
            CreatedAtUtc = m.CreatedAtUtc,
            UpdatedAtUtc = m.UpdatedAtUtc
        };
    }
}
