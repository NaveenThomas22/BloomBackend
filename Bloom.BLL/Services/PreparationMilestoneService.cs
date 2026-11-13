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

public class PreparationMilestoneService : IPreparationMilestoneService
{
    private readonly IPreparationMilestoneRepository _milestoneRepo;
    private readonly ISyllabusRepository _syllabusRepo;
    private readonly IUserProfileRepository _userProfileRepo;
    private readonly IValidator<CreateMilestoneDto> _createValidator;
    private readonly IValidator<UpdateMilestoneDto> _updateValidator;
    private readonly ILogger<PreparationMilestoneService> _logger;

    public PreparationMilestoneService(
        IPreparationMilestoneRepository milestoneRepo,
        ISyllabusRepository syllabusRepo,
        IUserProfileRepository userProfileRepo,
        IValidator<CreateMilestoneDto> createValidator,
        IValidator<UpdateMilestoneDto> updateValidator,
        ILogger<PreparationMilestoneService> logger)
    {
        _milestoneRepo = milestoneRepo ?? throw new ArgumentNullException(nameof(milestoneRepo));
        _syllabusRepo = syllabusRepo ?? throw new ArgumentNullException(nameof(syllabusRepo));
        _userProfileRepo = userProfileRepo ?? throw new ArgumentNullException(nameof(userProfileRepo));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<PreparationMilestoneDto>> GetMilestonesAsync(ulong userId, string? status = null, CancellationToken ct = default)
    {
        var milestones = await _milestoneRepo.GetMilestonesAsync(userId, status, false, ct);
        var tz = await ResolveTimeZoneAsync(userId, ct);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));

        var dtos = new List<PreparationMilestoneDto>();
        foreach (var m in milestones)
        {
            var dto = await EnrichMilestoneDtoAsync(m, userId, today, ct);
            dtos.Add(dto);
        }

        return dtos;
    }

    public async Task<PreparationMilestoneDto> GetMilestoneByIdAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        var milestone = await _milestoneRepo.GetMilestoneByIdAsync(id, userId, ct);
        if (milestone == null)
        {
            throw new NotFoundException($"Milestone with ID {id} not found.");
        }

        var tz = await ResolveTimeZoneAsync(userId, ct);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
        return await EnrichMilestoneDtoAsync(milestone, userId, today, ct);
    }

    public async Task<PreparationMilestoneDto> CreateMilestoneAsync(ulong userId, CreateMilestoneDto dto, CancellationToken ct = default)
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

        var milestone = new PreparationMilestone
        {
            UserId = userId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            TargetDate = dto.TargetDate,
            SubjectId = dto.SubjectId,
            TopicId = dto.TopicId,
            Priority = dto.Priority.Trim(),
            Status = "Pending",
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var created = await _milestoneRepo.CreateMilestoneAsync(milestone, ct);
        _logger.LogInformation("Created milestone {MilestoneId} for user {UserId} ({Title})", created.Id, userId, created.Title);

        var tz = await ResolveTimeZoneAsync(userId, ct);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
        return await EnrichMilestoneDtoAsync(created, userId, today, ct);
    }

    public async Task<PreparationMilestoneDto> UpdateMilestoneAsync(ulong id, ulong userId, UpdateMilestoneDto dto, CancellationToken ct = default)
    {
        var valResult = await _updateValidator.ValidateAsync(dto, ct);
        if (!valResult.IsValid)
        {
            var errors = valResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var existing = await _milestoneRepo.GetMilestoneByIdAsync(id, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Milestone with ID {id} not found.");
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
        existing.TargetDate = dto.TargetDate;
        existing.SubjectId = dto.SubjectId;
        existing.TopicId = dto.TopicId;
        existing.Priority = dto.Priority.Trim();

        var newStatus = dto.Status.Trim();
        if (string.Equals(newStatus, "Completed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(existing.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            existing.CompletedAtUtc = DateTime.UtcNow;
        }
        else if (!string.Equals(newStatus, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            existing.CompletedAtUtc = null;
        }
        existing.Status = newStatus;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await _milestoneRepo.UpdateMilestoneAsync(existing, ct);
        _logger.LogInformation("Updated milestone {MilestoneId} for user {UserId}", id, userId);

        var tz = await ResolveTimeZoneAsync(userId, ct);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
        return await EnrichMilestoneDtoAsync(existing, userId, today, ct);
    }

    public async Task DeleteMilestoneAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        var existing = await _milestoneRepo.GetMilestoneByIdAsync(id, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Milestone with ID {id} not found.");
        }

        await _milestoneRepo.SoftDeleteMilestoneAsync(id, userId, ct);
        _logger.LogInformation("Soft-deleted milestone {MilestoneId} for user {UserId}", id, userId);
    }

    private async Task<PreparationMilestoneDto> EnrichMilestoneDtoAsync(PreparationMilestone m, ulong userId, DateOnly today, CancellationToken ct)
    {
        string? subjName = null;
        if (m.SubjectId.HasValue)
        {
            var subj = await _syllabusRepo.GetSubjectByIdAsync(m.SubjectId.Value, userId, ct);
            subjName = subj?.Name;
        }

        string? topicName = null;
        if (m.TopicId.HasValue)
        {
            var topic = await _syllabusRepo.GetTopicByIdAsync(m.TopicId.Value, userId, ct);
            topicName = topic?.Name;
        }

        var status = m.Status;
        if (string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase) && m.TargetDate < today)
        {
            status = "Overdue";
        }

        return new PreparationMilestoneDto
        {
            Id = m.Id,
            UserId = m.UserId,
            Title = m.Title,
            Description = m.Description,
            TargetDate = m.TargetDate,
            SubjectId = m.SubjectId,
            SubjectName = subjName,
            TopicId = m.TopicId,
            TopicName = topicName,
            Priority = m.Priority,
            Status = status,
            CompletedAtUtc = m.CompletedAtUtc,
            CreatedAtUtc = m.CreatedAtUtc,
            UpdatedAtUtc = m.UpdatedAtUtc
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

