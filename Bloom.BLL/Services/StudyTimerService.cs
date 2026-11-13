using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Bloom.Core.Exceptions.ValidationException;

namespace Bloom.BLL.Services;

public class StudyTimerService : IStudyTimerService
{
    private readonly IUserTimerStateRepository _timerRepo;
    private readonly IActivityRecordRepository _activityRepo;
    private readonly IActivityCategoryRepository _categoryRepo;
    private readonly IStudySessionDetailRepository _studyDetailRepo;
    private readonly ISyllabusRepository _syllabusRepo;
    private readonly IUserActivityLockRepository _userLockRepo;
    private readonly IValidator<StartTimerDto> _startValidator;
    private readonly ILogger<StudyTimerService> _logger;

    public StudyTimerService(
        IUserTimerStateRepository timerRepo,
        IActivityRecordRepository activityRepo,
        IActivityCategoryRepository categoryRepo,
        IStudySessionDetailRepository studyDetailRepo,
        ISyllabusRepository syllabusRepo,
        IUserActivityLockRepository userLockRepo,
        IValidator<StartTimerDto> startValidator,
        ILogger<StudyTimerService> logger)
    {
        _timerRepo = timerRepo ?? throw new ArgumentNullException(nameof(timerRepo));
        _activityRepo = activityRepo ?? throw new ArgumentNullException(nameof(activityRepo));
        _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        _studyDetailRepo = studyDetailRepo ?? throw new ArgumentNullException(nameof(studyDetailRepo));
        _syllabusRepo = syllabusRepo ?? throw new ArgumentNullException(nameof(syllabusRepo));
        _userLockRepo = userLockRepo ?? throw new ArgumentNullException(nameof(userLockRepo));
        _startValidator = startValidator ?? throw new ArgumentNullException(nameof(startValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ActiveTimerDto> GetActiveTimerAsync(ulong userId, CancellationToken ct = default)
    {
        var state = await _timerRepo.GetByUserIdAsync(userId, null, null, ct);
        if (state == null)
        {
            return new ActiveTimerDto { HasActiveTimer = false };
        }

        var cat = await _categoryRepo.GetCategoryByIdAsync(state.ActivityCategoryId, userId, ct);

        SyllabusTopic? topic = null;
        SyllabusModule? module = null;
        SyllabusSubject? subject = null;

        if (state.TopicId.HasValue)
        {
            topic = await _syllabusRepo.GetTopicByIdAsync(state.TopicId.Value, userId, ct);
            if (topic != null)
            {
                module = await _syllabusRepo.GetModuleByIdAsync(topic.ModuleId, userId, ct);
                if (module != null)
                {
                    subject = await _syllabusRepo.GetSubjectByIdAsync(module.SubjectId, userId, ct);
                }
            }
        }

        // Calculate elapsed seconds across all intervals in this session group
        var intervals = (await _activityRepo.GetRecordsBySessionGroupAsync(userId, state.SessionGroupId, null, null, ct)) ?? Array.Empty<ActivityRecord>();
        var totalElapsedSeconds = 0.0;
        DateTime? currentIntervalStart = null;

        foreach (var interval in intervals)
        {
            if (interval.EndAtUtc.HasValue)
            {
                totalElapsedSeconds += (interval.EndAtUtc.Value - interval.StartAtUtc).TotalSeconds;
            }
            else if (interval.ActivityStatus == "Running" && state.TimerState == "Running")
            {
                currentIntervalStart = interval.StartAtUtc;
                totalElapsedSeconds += (DateTime.UtcNow - interval.StartAtUtc).TotalSeconds;
            }
        }

        return new ActiveTimerDto
        {
            HasActiveTimer = true,
            SessionGroupId = state.SessionGroupId,
            ActivityCategoryId = state.ActivityCategoryId,
            CategoryName = cat?.CategoryName,
            CategoryCode = cat?.CategoryCode,
            Color = cat?.Color ?? "#A8BDA5",
            Icon = cat?.Icon ?? "activity",
            TopicId = state.TopicId,
            TopicName = topic?.Name,
            ModuleName = module?.Name,
            SubjectCode = subject?.Code,
            Title = state.Title,
            StudyMode = state.StudyMode,
            TimerState = state.TimerState,
            StartedAtUtc = state.StartedAtUtc,
            CurrentIntervalStartedAtUtc = currentIntervalStart,
            ElapsedSeconds = Math.Max(0, Math.Round(totalElapsedSeconds, 1))
        };
    }

    public async Task<ActiveTimerDto> StartTimerAsync(ulong userId, StartTimerDto dto, CancellationToken ct = default)
    {
        var valResult = await _startValidator.ValidateAsync(dto, ct);
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

        var nowUtc = DateTime.UtcNow;

        // Atomically check and start under exclusive per-user database row lock
        await _userLockRepo.ExecuteWithUserLockAsync(userId, async (conn, tx) =>
        {
            var existingTimer = await _timerRepo.GetByUserIdAsync(userId, conn, tx, ct);
            if (existingTimer != null)
            {
                throw new ConflictException("Another timer is already running or paused. Please complete or cancel it before starting a new timer.");
            }

            // Check if another activity is already recorded/running during this window
            var overlaps = await _activityRepo.GetOverlappingRecordsAsync(userId, nowUtc, nowUtc.AddSeconds(1), null, conn, tx, ct);
            if (overlaps.Count > 0)
            {
                var first = overlaps[0];
                throw new ConflictException($"Cannot start study timer because activity '{first.ActivityTitle}' is already recorded during this time window.");
            }

            var sessionGroupId = Guid.NewGuid().ToString("N");

            var record = new ActivityRecord
            {
                UserId = userId,
                ActivityCategoryId = dto.ActivityCategoryId,
                ActivityTitle = dto.Title.Trim(),
                StartAtUtc = nowUtc,
                EndAtUtc = null, // Actively running
                TimezoneId = string.IsNullOrWhiteSpace(dto.TimezoneId) ? "Asia/Kolkata" : dto.TimezoneId.Trim(),
                ActivityStatus = "Running",
                EntrySource = "Timer",
                IsPlanned = false,
                SessionGroupId = sessionGroupId
            };

            var createdRecord = await _activityRepo.CreateRecordAsync(record, conn, tx, ct);

            if (cat.CategoryGroup == "Study" || dto.TopicId.HasValue)
            {
                var detail = new StudySessionDetail
                {
                    ActivityId = createdRecord.Id,
                    TopicId = dto.TopicId,
                    StudyMode = dto.StudyMode ?? "Practice",
                    DifficultyLevel = "Moderate"
                };
                await _studyDetailRepo.UpsertAsync(detail, conn, tx, ct);
            }

            var timerState = new UserTimerState
            {
                UserId = userId,
                SessionGroupId = sessionGroupId,
                ActivityCategoryId = dto.ActivityCategoryId,
                TopicId = dto.TopicId,
                Title = dto.Title.Trim(),
                StudyMode = dto.StudyMode ?? "Practice",
                TimerState = "Running",
                CurrentIntervalId = createdRecord.Id,
                StartedAtUtc = nowUtc
            };

            await _timerRepo.UpsertAsync(timerState, conn, tx, ct);
            _logger.LogInformation("User {UserId} started timer for session group {SessionGroupId}.", userId, sessionGroupId);

            return createdRecord.Id;
        }, ct);

        return await GetActiveTimerAsync(userId, ct);
    }

    public async Task<ActiveTimerDto> PauseTimerAsync(ulong userId, CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;

        await _userLockRepo.ExecuteWithUserLockAsync(userId, async (conn, tx) =>
        {
            var timerState = await _timerRepo.GetByUserIdAsync(userId, conn, tx, ct);
            if (timerState == null)
            {
                throw new NotFoundException("No active timer found to pause.");
            }

            if (timerState.TimerState == "Paused")
            {
                return true; // Idempotent
            }

            if (timerState.CurrentIntervalId.HasValue)
            {
                var currentInterval = await _activityRepo.GetRecordByIdAsync(timerState.CurrentIntervalId.Value, userId, conn, tx, ct);
                if (currentInterval != null && currentInterval.EndAtUtc == null)
                {
                    currentInterval.EndAtUtc = nowUtc;
                    currentInterval.ActivityStatus = "Completed";
                    await _activityRepo.UpdateRecordAsync(currentInterval, conn, tx, ct);
                }
            }

            timerState.TimerState = "Paused";
            timerState.LastPausedAtUtc = nowUtc;
            timerState.CurrentIntervalId = null;

            await _timerRepo.UpsertAsync(timerState, conn, tx, ct);
            _logger.LogInformation("User {UserId} paused timer for session group {SessionGroupId}.", userId, timerState.SessionGroupId);

            return true;
        }, ct);

        return await GetActiveTimerAsync(userId, ct);
    }

    public async Task<ActiveTimerDto> ResumeTimerAsync(ulong userId, CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;

        await _userLockRepo.ExecuteWithUserLockAsync(userId, async (conn, tx) =>
        {
            var timerState = await _timerRepo.GetByUserIdAsync(userId, conn, tx, ct);
            if (timerState == null)
            {
                throw new NotFoundException("No active timer found to resume.");
            }

            if (timerState.TimerState == "Running")
            {
                return true; // Idempotent
            }

            // Check if another activity is recorded at nowUtc (excluding intervals from this same session group)
            var overlaps = await _activityRepo.GetOverlappingRecordsAsync(userId, nowUtc, nowUtc.AddSeconds(1), null, conn, tx, ct);
            var foreignOverlaps = overlaps.Where(r => r.SessionGroupId != timerState.SessionGroupId).ToList();
            if (foreignOverlaps.Count > 0)
            {
                var first = foreignOverlaps[0];
                throw new ConflictException($"Cannot resume study timer because activity '{first.ActivityTitle}' is already recorded during this time window.");
            }

            var record = new ActivityRecord
            {
                UserId = userId,
                ActivityCategoryId = timerState.ActivityCategoryId,
                ActivityTitle = timerState.Title,
                StartAtUtc = nowUtc,
                EndAtUtc = null,
                TimezoneId = "Asia/Kolkata",
                ActivityStatus = "Running",
                EntrySource = "Timer",
                IsPlanned = false,
                SessionGroupId = timerState.SessionGroupId
            };

            var createdRecord = await _activityRepo.CreateRecordAsync(record, conn, tx, ct);

            if (timerState.TopicId.HasValue)
            {
                var detail = new StudySessionDetail
                {
                    ActivityId = createdRecord.Id,
                    TopicId = timerState.TopicId,
                    StudyMode = timerState.StudyMode,
                    DifficultyLevel = "Moderate"
                };
                await _studyDetailRepo.UpsertAsync(detail, conn, tx, ct);
            }

            timerState.TimerState = "Running";
            timerState.CurrentIntervalId = createdRecord.Id;
            timerState.LastPausedAtUtc = null;

            await _timerRepo.UpsertAsync(timerState, conn, tx, ct);
            _logger.LogInformation("User {UserId} resumed timer for session group {SessionGroupId}.", userId, timerState.SessionGroupId);

            return true;
        }, ct);

        return await GetActiveTimerAsync(userId, ct);
    }

    public async Task<ActivityRecordDto> CompleteTimerAsync(ulong userId, CompleteTimerDto dto, CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;

        var lastRecordId = await _userLockRepo.ExecuteWithUserLockAsync(userId, async (conn, tx) =>
        {
            var timerState = await _timerRepo.GetByUserIdAsync(userId, conn, tx, ct);
            if (timerState == null)
            {
                throw new NotFoundException("No active timer found to complete.");
            }

            ActivityRecord? lastRecord = null;

            if (timerState.CurrentIntervalId.HasValue)
            {
                lastRecord = await _activityRepo.GetRecordByIdAsync(timerState.CurrentIntervalId.Value, userId, conn, tx, ct);
                if (lastRecord != null && lastRecord.EndAtUtc == null)
                {
                    lastRecord.EndAtUtc = nowUtc;
                    lastRecord.ActivityStatus = "Completed";
                    await _activityRepo.UpdateRecordAsync(lastRecord, conn, tx, ct);
                }
            }

            if (lastRecord == null)
            {
                var intervals = await _activityRepo.GetRecordsBySessionGroupAsync(userId, timerState.SessionGroupId, conn, tx, ct);
                lastRecord = intervals.LastOrDefault();
            }

            if (lastRecord == null)
            {
                await _timerRepo.DeleteAsync(userId, conn, tx, ct);
                throw new InvalidOperationException("No recorded intervals found for completed timer session.");
            }

            if (!string.IsNullOrWhiteSpace(dto.SessionNotes) || dto.QuestionsAttempted > 0 || !string.IsNullOrWhiteSpace(dto.DifficultyLevel))
            {
                var detail = await _studyDetailRepo.GetByActivityIdAsync(lastRecord.Id, ct) ?? new StudySessionDetail
                {
                    ActivityId = lastRecord.Id,
                    TopicId = timerState.TopicId,
                    StudyMode = timerState.StudyMode
                };

                detail.SessionNotes = dto.SessionNotes?.Trim();
                detail.QuestionsAttempted = dto.QuestionsAttempted;
                detail.QuestionsCorrect = dto.QuestionsCorrect;
                if (!string.IsNullOrWhiteSpace(dto.DifficultyLevel))
                {
                    detail.DifficultyLevel = dto.DifficultyLevel.Trim();
                }

                await _studyDetailRepo.UpsertAsync(detail, conn, tx, ct);
            }

            await _timerRepo.DeleteAsync(userId, conn, tx, ct);
            _logger.LogInformation("User {UserId} completed timer session group {SessionGroupId}.", userId, timerState.SessionGroupId);

            return lastRecord.Id;
        }, ct);

        var finalRecord = await _activityRepo.GetRecordByIdAsync(lastRecordId, userId, null, null, ct);
        if (finalRecord == null)
        {
            throw new NotFoundException($"Completed activity record with ID {lastRecordId} not found.");
        }

        var cat = await _categoryRepo.GetCategoryByIdAsync(finalRecord.ActivityCategoryId, userId, ct);
        var finalDetail = await _studyDetailRepo.GetByActivityIdAsync(finalRecord.Id, ct);

        StudySessionDetailDto? studyDto = null;
        if (finalDetail != null)
        {
            var topic = finalDetail.TopicId.HasValue ? await _syllabusRepo.GetTopicByIdAsync(finalDetail.TopicId.Value, userId, ct) : null;
            studyDto = new StudySessionDetailDto
            {
                ActivityId = finalRecord.Id,
                TopicId = finalDetail.TopicId,
                TopicName = topic?.Name,
                StudyMode = finalDetail.StudyMode,
                DifficultyLevel = finalDetail.DifficultyLevel,
                QuestionsAttempted = finalDetail.QuestionsAttempted,
                QuestionsCorrect = finalDetail.QuestionsCorrect,
                SessionNotes = finalDetail.SessionNotes
            };
        }

        return new ActivityRecordDto
        {
            Id = finalRecord.Id,
            UserId = finalRecord.UserId,
            ActivityCategoryId = finalRecord.ActivityCategoryId,
            CategoryCode = cat?.CategoryCode ?? "OTHER",
            CategoryName = cat?.CategoryName ?? "Activity",
            CategoryGroup = cat?.CategoryGroup ?? "Other",
            Color = cat?.Color ?? "#A8BDA5",
            Icon = cat?.Icon ?? "activity",
            ActivityTitle = finalRecord.ActivityTitle,
            StartAtUtc = finalRecord.StartAtUtc,
            EndAtUtc = finalRecord.EndAtUtc,
            TimezoneId = finalRecord.TimezoneId,
            ActivityStatus = finalRecord.ActivityStatus,
            EntrySource = finalRecord.EntrySource,
            IsPlanned = finalRecord.IsPlanned,
            SessionGroupId = finalRecord.SessionGroupId,
            DurationMinutes = Math.Round(finalRecord.DurationMinutes, 1),
            StudyDetails = studyDto
        };
    }

    public async Task<bool> CancelTimerAsync(ulong userId, CancellationToken ct = default)
    {
        return await _userLockRepo.ExecuteWithUserLockAsync(userId, async (conn, tx) =>
        {
            var timerState = await _timerRepo.GetByUserIdAsync(userId, conn, tx, ct);
            if (timerState == null)
            {
                return true;
            }

            var intervals = await _activityRepo.GetRecordsBySessionGroupAsync(userId, timerState.SessionGroupId, conn, tx, ct);
            foreach (var interval in intervals)
            {
                interval.ActivityStatus = "Cancelled";
                interval.EndAtUtc = interval.EndAtUtc ?? DateTime.UtcNow;
                await _activityRepo.UpdateRecordAsync(interval, conn, tx, ct);
            }

            await _timerRepo.DeleteAsync(userId, conn, tx, ct);
            _logger.LogInformation("User {UserId} cancelled timer session group {SessionGroupId}.", userId, timerState.SessionGroupId);
            return true;
        }, ct);
    }
}
