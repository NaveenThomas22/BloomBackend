using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Bloom.Core.Exceptions.ValidationException;

namespace Bloom.BLL.Services;

public class ActivityRecordService : IActivityRecordService
{
    private readonly IActivityRecordRepository _activityRepo;
    private readonly IActivityCategoryRepository _categoryRepo;
    private readonly IStudySessionDetailRepository _studyDetailRepo;
    private readonly ISyllabusRepository _syllabusRepo;
    private readonly IUserActivityLockRepository _userLockRepo;
    private readonly IValidator<CreateActivityRecordDto> _createValidator;
    private readonly IValidator<UpdateActivityRecordDto> _updateValidator;
    private readonly ILogger<ActivityRecordService> _logger;

    public ActivityRecordService(
        IActivityRecordRepository activityRepo,
        IActivityCategoryRepository categoryRepo,
        IStudySessionDetailRepository studyDetailRepo,
        ISyllabusRepository syllabusRepo,
        IUserActivityLockRepository userLockRepo,
        IValidator<CreateActivityRecordDto> createValidator,
        IValidator<UpdateActivityRecordDto> updateValidator,
        ILogger<ActivityRecordService> logger)
    {
        _activityRepo = activityRepo ?? throw new ArgumentNullException(nameof(activityRepo));
        _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        _studyDetailRepo = studyDetailRepo ?? throw new ArgumentNullException(nameof(studyDetailRepo));
        _syllabusRepo = syllabusRepo ?? throw new ArgumentNullException(nameof(syllabusRepo));
        _userLockRepo = userLockRepo ?? throw new ArgumentNullException(nameof(userLockRepo));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ActivityRecordDto> GetRecordByIdAsync(ulong userId, ulong recordId, CancellationToken ct = default)
    {
        var record = await _activityRepo.GetRecordByIdAsync(recordId, userId, null, null, ct);
        if (record == null)
        {
            throw new NotFoundException($"Activity record with ID {recordId} not found.");
        }

        var cat = await _categoryRepo.GetCategoryByIdAsync(record.ActivityCategoryId, userId, ct);
        var studyDetail = await _studyDetailRepo.GetByActivityIdAsync(record.Id, ct);

        StudySessionDetailDto? mappedStudyDetail = null;
        if (studyDetail != null)
        {
            var topic = studyDetail.TopicId.HasValue 
                ? await _syllabusRepo.GetTopicByIdAsync(studyDetail.TopicId.Value, userId, ct) 
                : null;

            mappedStudyDetail = new StudySessionDetailDto
            {
                ActivityId = record.Id,
                TopicId = studyDetail.TopicId,
                TopicName = topic?.Name,
                StudyMode = studyDetail.StudyMode,
                DifficultyLevel = studyDetail.DifficultyLevel,
                QuestionsAttempted = studyDetail.QuestionsAttempted,
                QuestionsCorrect = studyDetail.QuestionsCorrect,
                SessionNotes = studyDetail.SessionNotes
            };
        }

        return MapToDto(record, cat, mappedStudyDetail);
    }

    public async Task<ActivityRecordDto> CreateRecordAsync(ulong userId, CreateActivityRecordDto dto, CancellationToken ct = default)
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

        if (dto.TopicId.HasValue)
        {
            var topic = await _syllabusRepo.GetTopicByIdAsync(dto.TopicId.Value, userId, ct);
            if (topic == null)
            {
                throw new NotFoundException($"Syllabus topic with ID {dto.TopicId.Value} not found.");
            }
        }

        var effectiveEnd = dto.EndAtUtc ?? DateTime.UtcNow;

        return await _userLockRepo.ExecuteWithUserLockAsync(userId, async (conn, tx) =>
        {
            // Check for overlapping intervals under exclusive row lock
            var overlaps = await _activityRepo.GetOverlappingRecordsAsync(userId, dto.StartAtUtc, effectiveEnd, null, conn, tx, ct);
            if (overlaps.Count > 0)
            {
                var first = overlaps[0];
                throw new ValidationException(
                    "TimeInterval",
                    $"Activity interval [{dto.StartAtUtc:HH:mm} - {effectiveEnd:HH:mm} UTC] overlaps with existing activity '{first.ActivityTitle}' [{first.StartAtUtc:HH:mm} - {first.EndAtUtc:HH:mm} UTC]. Activities cannot occupy overlapping time.");
            }

            var record = new ActivityRecord
            {
                UserId = userId,
                ActivityCategoryId = dto.ActivityCategoryId,
                ActivityTitle = dto.ActivityTitle.Trim(),
                Description = dto.Description?.Trim(),
                StartAtUtc = dto.StartAtUtc,
                EndAtUtc = dto.EndAtUtc,
                TimezoneId = string.IsNullOrWhiteSpace(dto.TimezoneId) ? "Asia/Kolkata" : dto.TimezoneId.Trim(),
                ActivityStatus = dto.EndAtUtc.HasValue ? "Completed" : "Running",
                EntrySource = dto.EntrySource,
                IsPlanned = dto.IsPlanned,
                SessionGroupId = dto.SessionGroupId
            };

            var created = await _activityRepo.CreateRecordAsync(record, conn, tx, ct);

            StudySessionDetailDto? studyDto = null;
            if (cat.CategoryGroup == "Study" || dto.TopicId.HasValue || !string.IsNullOrWhiteSpace(dto.SessionNotes))
            {
                var detail = new StudySessionDetail
                {
                    ActivityId = created.Id,
                    TopicId = dto.TopicId,
                    StudyMode = dto.StudyMode ?? "Practice",
                    DifficultyLevel = dto.DifficultyLevel ?? "Moderate",
                    QuestionsAttempted = dto.QuestionsAttempted,
                    QuestionsCorrect = dto.QuestionsCorrect,
                    SessionNotes = dto.SessionNotes?.Trim()
                };
                await _studyDetailRepo.UpsertAsync(detail, conn, tx, ct);

                studyDto = new StudySessionDetailDto
                {
                    ActivityId = created.Id,
                    TopicId = detail.TopicId,
                    StudyMode = detail.StudyMode,
                    DifficultyLevel = detail.DifficultyLevel,
                    QuestionsAttempted = detail.QuestionsAttempted,
                    QuestionsCorrect = detail.QuestionsCorrect,
                    SessionNotes = detail.SessionNotes
                };
            }

            _logger.LogInformation("User {UserId} created activity record {RecordId} '{Title}'.", userId, created.Id, created.ActivityTitle);
            return MapToDto(created, cat, studyDto);
        }, ct);
    }

    public async Task<ActivityRecordDto> UpdateRecordAsync(ulong userId, ulong recordId, UpdateActivityRecordDto dto, CancellationToken ct = default)
    {
        var valResult = await _updateValidator.ValidateAsync(dto, ct);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var cat = await _categoryRepo.GetCategoryByIdAsync(dto.ActivityCategoryId, userId, ct);
        if (cat == null)
        {
            throw new NotFoundException($"Activity category with ID {dto.ActivityCategoryId} not found.");
        }

        return await _userLockRepo.ExecuteWithUserLockAsync(userId, async (conn, tx) =>
        {
            var existing = await _activityRepo.GetRecordByIdAsync(recordId, userId, conn, tx, ct);
            if (existing == null)
            {
                throw new NotFoundException($"Activity record with ID {recordId} not found.");
            }

            // Overlap re-validation excluding this record under lock
            var overlaps = await _activityRepo.GetOverlappingRecordsAsync(userId, dto.StartAtUtc, dto.EndAtUtc, recordId, conn, tx, ct);
            if (overlaps.Count > 0)
            {
                var first = overlaps[0];
                throw new ValidationException(
                    "TimeInterval",
                    $"Updated interval [{dto.StartAtUtc:HH:mm} - {dto.EndAtUtc:HH:mm} UTC] overlaps with existing activity '{first.ActivityTitle}' [{first.StartAtUtc:HH:mm} - {first.EndAtUtc:HH:mm} UTC].");
            }

            existing.ActivityCategoryId = dto.ActivityCategoryId;
            existing.ActivityTitle = dto.ActivityTitle.Trim();
            existing.Description = dto.Description?.Trim();
            existing.StartAtUtc = dto.StartAtUtc;
            existing.EndAtUtc = dto.EndAtUtc;
            existing.TimezoneId = string.IsNullOrWhiteSpace(dto.TimezoneId) ? existing.TimezoneId : dto.TimezoneId.Trim();
            existing.ActivityStatus = "Completed";

            await _activityRepo.UpdateRecordAsync(existing, conn, tx, ct);

            StudySessionDetailDto? studyDto = null;
            if (cat.CategoryGroup == "Study" || dto.TopicId.HasValue || !string.IsNullOrWhiteSpace(dto.SessionNotes))
            {
                var detail = await _studyDetailRepo.GetByActivityIdAsync(recordId, ct) ?? new StudySessionDetail
                {
                    ActivityId = recordId
                };

                detail.TopicId = dto.TopicId;
                detail.StudyMode = dto.StudyMode ?? detail.StudyMode ?? "Practice";
                detail.DifficultyLevel = dto.DifficultyLevel ?? detail.DifficultyLevel ?? "Moderate";
                detail.QuestionsAttempted = dto.QuestionsAttempted;
                detail.QuestionsCorrect = dto.QuestionsCorrect;
                detail.SessionNotes = dto.SessionNotes?.Trim();

                await _studyDetailRepo.UpsertAsync(detail, conn, tx, ct);

                var topic = detail.TopicId.HasValue 
                    ? await _syllabusRepo.GetTopicByIdAsync(detail.TopicId.Value, userId, ct) 
                    : null;

                studyDto = new StudySessionDetailDto
                {
                    ActivityId = recordId,
                    TopicId = detail.TopicId,
                    TopicName = topic?.Name,
                    StudyMode = detail.StudyMode,
                    DifficultyLevel = detail.DifficultyLevel,
                    QuestionsAttempted = detail.QuestionsAttempted,
                    QuestionsCorrect = detail.QuestionsCorrect,
                    SessionNotes = detail.SessionNotes
                };
            }

            _logger.LogInformation("User {UserId} updated activity record {RecordId} '{Title}'.", userId, existing.Id, existing.ActivityTitle);
            return MapToDto(existing, cat, studyDto);
        }, ct);
    }

    public async Task<bool> DeleteRecordAsync(ulong userId, ulong recordId, CancellationToken ct = default)
    {
        var existing = await _activityRepo.GetRecordByIdAsync(recordId, userId, null, null, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Activity record with ID {recordId} not found.");
        }

        var deleted = await _activityRepo.SoftDeleteRecordAsync(recordId, userId, ct);
        if (deleted)
        {
            _logger.LogInformation("User {UserId} soft-deleted activity record {RecordId}.", userId, recordId);
        }
        return deleted;
    }

    private static ActivityRecordDto MapToDto(ActivityRecord r, ActivityCategory? c, StudySessionDetailDto? study)
    {
        return new ActivityRecordDto
        {
            Id = r.Id,
            UserId = r.UserId,
            ActivityCategoryId = r.ActivityCategoryId,
            CategoryCode = c?.CategoryCode ?? "OTHER",
            CategoryName = c?.CategoryName ?? "Activity",
            CategoryGroup = c?.CategoryGroup ?? "Other",
            Color = c?.Color ?? "#A8BDA5",
            Icon = c?.Icon ?? "activity",
            ActivityTitle = r.ActivityTitle,
            Description = r.Description,
            StartAtUtc = r.StartAtUtc,
            EndAtUtc = r.EndAtUtc,
            TimezoneId = r.TimezoneId,
            ActivityStatus = r.ActivityStatus,
            EntrySource = r.EntrySource,
            IsPlanned = r.IsPlanned,
            SessionGroupId = r.SessionGroupId,
            DurationMinutes = Math.Round(r.DurationMinutes, 1),
            StudyDetails = study
        };
    }
}
