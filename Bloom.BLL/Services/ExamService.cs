using System.Text.Json;
using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Bloom.Core.Exceptions.ValidationException;

namespace Bloom.BLL.Services;

public class ExamService : IExamService
{
    private readonly IExamTemplateRepository _templateRepo;
    private readonly IExamAttemptRepository _attemptRepo;
    private readonly IQuestionRepository _questionRepo;
    private readonly IQuestionAttemptRepository _qAttemptRepo;
    private readonly IActivityCategoryRepository _categoryRepo;
    private readonly IActivityRecordRepository _activityRepo;
    private readonly IStudySessionDetailRepository _studyDetailRepo;
    private readonly IUserProfileRepository _userProfileRepo;
    private readonly IUserActivityLockRepository _userLockRepo;
    private readonly IValidator<SaveExamAnswerRequest> _saveAnswerValidator;
    private readonly ILogger<ExamService> _logger;

    public ExamService(
        IExamTemplateRepository templateRepo,
        IExamAttemptRepository attemptRepo,
        IQuestionRepository questionRepo,
        IQuestionAttemptRepository qAttemptRepo,
        IActivityCategoryRepository categoryRepo,
        IActivityRecordRepository activityRepo,
        IStudySessionDetailRepository studyDetailRepo,
        IUserProfileRepository userProfileRepo,
        IUserActivityLockRepository userLockRepo,
        IValidator<SaveExamAnswerRequest> saveAnswerValidator,
        ILogger<ExamService> logger)
    {
        _templateRepo = templateRepo;
        _attemptRepo = attemptRepo;
        _questionRepo = questionRepo;
        _qAttemptRepo = qAttemptRepo;
        _categoryRepo = categoryRepo;
        _activityRepo = activityRepo;
        _studyDetailRepo = studyDetailRepo;
        _userProfileRepo = userProfileRepo;
        _userLockRepo = userLockRepo;
        _saveAnswerValidator = saveAnswerValidator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExamTemplateDto>> GetExamTemplatesAsync(string? examType = null, CancellationToken ct = default)
    {
        var templates = await _templateRepo.GetTemplatesAsync(examType, true, ct);
        return templates.Select(MapTemplateDto).ToList();
    }

    public async Task<ExamTemplateDto?> GetExamTemplateByIdAsync(ulong templateId, CancellationToken ct = default)
    {
        var template = await _templateRepo.GetTemplateWithSectionsAsync(templateId, ct);
        return template == null ? null : MapTemplateDto(template);
    }

    public async Task<ActiveExamAttemptDto> StartExamAttemptAsync(ulong userId, ulong templateId, CancellationToken ct = default)
    {
        return await _userLockRepo.ExecuteWithUserLockAsync(userId, async (conn, tx) =>
        {
            // 1. Check if user already has an active attempt for this template
            var existing = await _attemptRepo.GetActiveAttemptForUserAsync(userId, templateId, conn, tx, ct);
            if (existing != null)
            {
                var now = DateTime.UtcNow;
                if (existing.DeadlineUtc > now)
                {
                    _logger.LogInformation("Resuming active exam attempt {AttemptId} for user {UserId}", existing.Id, userId);
                    return MapActiveAttempt(existing, now);
                }
            }

            // 2. Fetch template with configured sections & questions
            var template = await _templateRepo.GetTemplateWithSectionsAsync(templateId, ct);
            if (template == null || !template.IsActive || !template.IsPublished)
            {
                throw new NotFoundException($"Exam template {templateId} is not available.");
            }

            if (template.Sections.Count == 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["Sections"] = new[] { "This exam has no configured sections." }
                });
            }

            var nowUtc = DateTime.UtcNow;
            var totalDuration = template.TotalDurationMinutes > 0 ? template.TotalDurationMinutes : template.Sections.Sum(s => s.DurationMinutes);
            var deadlineUtc = nowUtc.AddMinutes(totalDuration);

            var firstSection = template.Sections.OrderBy(s => s.DisplayOrder).First();
            var sectionDeadlineUtc = nowUtc.AddMinutes(firstSection.DurationMinutes);

            var attempt = new ExamAttempt
            {
                UserId = userId,
                ExamTemplateId = templateId,
                Status = "InProgress",
                StartedAtUtc = nowUtc,
                DeadlineUtc = deadlineUtc,
                CurrentSectionId = firstSection.Id,
                SectionStartedAtUtc = nowUtc,
                SectionDeadlineUtc = sectionDeadlineUtc,
                CurrentQuestionIndex = 0,
                TotalQuestions = template.TotalQuestions,
                MaxPossibleScore = template.TotalMarks
            };

            // 3. Snapshot question content and scoring rules
            var attemptQuestions = new List<ExamAttemptQuestion>();

            foreach (var sec in template.Sections.OrderBy(s => s.DisplayOrder))
            {
                int order = 1;
                foreach (var sq in sec.SectionQuestions.OrderBy(q => q.DisplayOrder))
                {
                    var q = await _questionRepo.GetQuestionByIdAsync(sq.QuestionId, includeAnswerKey: true, ct);
                    if (q == null) continue;

                    var optionsJson = q.Options.Count > 0
                        ? JsonSerializer.Serialize(q.Options.Select(o => new { o.OptionKey, o.Content }))
                        : null;

                    var passageJson = q.Passage != null
                        ? JsonSerializer.Serialize(new { q.Passage.Title, q.Passage.Content, q.Passage.WordCount, q.Passage.Genre })
                        : null;

                    var setJson = q.QuestionSet != null
                        ? JsonSerializer.Serialize(new { q.QuestionSet.Title, q.QuestionSet.ContextScenario, q.QuestionSet.DataPayloadJson })
                        : null;

                    var marks = sq.MarksOverride ?? sec.CorrectMarks;
                    var penalty = sq.PenaltyOverride ?? (q.QuestionType == "TITA" ? 0m : sec.IncorrectPenalty);

                    attemptQuestions.Add(new ExamAttemptQuestion
                    {
                        ExamSectionId = sec.Id,
                        QuestionId = q.Id,
                        DisplayOrder = order++,
                        SnapshotQuestionType = q.QuestionType,
                        SnapshotContent = q.QuestionContent,
                        SnapshotOptionsJson = optionsJson,
                        SnapshotPassageJson = passageJson,
                        SnapshotSetJson = setJson,
                        SnapshotCorrectAnswer = q.CorrectAnswer,
                        SnapshotMarks = marks,
                        SnapshotPenalty = penalty,
                        SnapshotExplanation = q.Explanation,
                        SnapshotSolutionSteps = q.SolutionSteps,
                        AnswerStatus = "NotVisited",
                        TimeSpentSeconds = 0,
                        LastSavedAtUtc = nowUtc
                    });
                }
            }

            if (attemptQuestions.Count == 0)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["Questions"] = new[] { "Insufficient questions available for this exam template." }
                });
            }

            attempt.TotalQuestions = attemptQuestions.Count;

            var created = await _attemptRepo.CreateAttemptAsync(attempt, attemptQuestions, conn, tx, ct);
            created.Template = template;
            _logger.LogInformation("Started exam attempt {AttemptId} for user {UserId} (Template: {TemplateId})", created.Id, userId, templateId);

            return MapActiveAttempt(created, nowUtc);
        }, ct);
    }

    public async Task<ActiveExamAttemptDto?> GetActiveExamAttemptAsync(ulong userId, CancellationToken ct = default)
    {
        var attempt = await _attemptRepo.GetActiveAttemptForUserAsync(userId, ct: ct);
        if (attempt == null) return null;

        return await EvaluateAttemptTimingAndTransitionAsync(attempt, userId, ct);
    }

    public async Task<ActiveExamAttemptDto?> GetExamAttemptByIdAsync(ulong attemptId, ulong userId, CancellationToken ct = default)
    {
        var attempt = await _attemptRepo.GetAttemptByIdAsync(attemptId, userId, ct: ct);
        if (attempt == null) return null;

        if (attempt.Status == "InProgress")
        {
            return await EvaluateAttemptTimingAndTransitionAsync(attempt, userId, ct);
        }

        return MapActiveAttempt(attempt, DateTime.UtcNow);
    }

    public async Task<SaveExamAnswerResponseDto> SaveExamAnswerAsync(ulong attemptId, ulong userId, SaveExamAnswerRequest request, CancellationToken ct = default)
    {
        var val = await _saveAnswerValidator.ValidateAsync(request, ct);
        if (!val.IsValid)
        {
            var errors = val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var attempt = await _attemptRepo.GetAttemptByIdAsync(attemptId, userId, ct: ct);
        if (attempt == null) throw new NotFoundException($"Exam attempt {attemptId} not found.");
        if (attempt.Status != "InProgress") throw new ConflictException("Cannot save answers for an exam attempt that is not in progress.");

        var now = DateTime.UtcNow;
        if (attempt.DeadlineUtc <= now)
        {
            // Total time expired, automatically submit
            await SubmitExamAttemptAsync(attemptId, userId, isAutoSubmit: true, ct);
            throw new ConflictException("Exam deadline has expired. Your attempt has been automatically submitted.");
        }

        // 1. Verify question belongs to active section in CAT examination
        var questionInAttempt = attempt.AttemptQuestions.FirstOrDefault(q => q.QuestionId == request.QuestionId);
        if (questionInAttempt == null)
        {
            throw new NotFoundException($"Question {request.QuestionId} does not belong to this exam attempt.");
        }
        if (questionInAttempt.ExamSectionId != attempt.CurrentSectionId)
        {
            throw new ForbiddenException("Cannot answer questions outside the current active examination section.");
        }

        // 2. Verify section deadline
        if (attempt.SectionDeadlineUtc <= now)
        {
            var sections = attempt.Template?.Sections.OrderBy(s => s.DisplayOrder).ToList() ?? new List<ExamSection>();
            var curIndex = sections.FindIndex(s => s.Id == attempt.CurrentSectionId);
            if (curIndex >= 0 && curIndex < sections.Count - 1)
            {
                var nextSec = sections[curIndex + 1];
                var secDeadline = now.AddMinutes(nextSec.DurationMinutes);
                if (secDeadline > attempt.DeadlineUtc) secDeadline = attempt.DeadlineUtc;

                await _attemptRepo.TransitionSectionAsync(attempt.Id, nextSec.Id, now, secDeadline, ct);
                attempt.CurrentSectionId = nextSec.Id;
                attempt.SectionStartedAtUtc = now;
                attempt.SectionDeadlineUtc = secDeadline;
                throw new ConflictException($"Section time limit expired. You have been transitioned to {nextSec.SectionName}.");
            }
            else
            {
                await SubmitExamAttemptAsync(attemptId, userId, isAutoSubmit: true, ct);
                throw new ConflictException("Exam deadline has expired. Your attempt has been automatically submitted.");
            }
        }

        var expectedVersion = request.ExpectedVersion!.Value;
        var success = await _attemptRepo.SaveQuestionAnswerAsync(
            attemptId, request.QuestionId, request.UserAnswer?.Trim(), request.AnswerStatus, request.TimeSpentSeconds, expectedVersion, ct);

        if (!success)
        {
            throw new ConflictException("Stale answer rejected: A newer answer state has already been persisted for this question (expected version mismatch).");
        }

        return new SaveExamAnswerResponseDto
        {
            QuestionId = request.QuestionId,
            NewVersion = expectedVersion + 1,
            LastSavedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<ActiveExamAttemptDto> TransitionSectionAsync(ulong attemptId, ulong userId, TransitionSectionRequest request, CancellationToken ct = default)
    {
        return await _userLockRepo.ExecuteWithUserLockAsync(userId, async (conn, tx) =>
        {
            var attempt = await _attemptRepo.GetAttemptByIdAsync(attemptId, userId, conn, tx, ct);
            if (attempt == null) throw new NotFoundException($"Exam attempt {attemptId} not found.");
            if (attempt.Status != "InProgress") throw new ConflictException("Exam attempt is not in progress.");

            var now = DateTime.UtcNow;
            if (attempt.DeadlineUtc <= now)
            {
                await SubmitExamAttemptAsync(attemptId, userId, isAutoSubmit: true, ct);
                throw new ConflictException("Exam deadline has expired. Your attempt has been automatically submitted.");
            }

            var sections = attempt.Template?.Sections.OrderBy(s => s.DisplayOrder).ToList() ?? new List<ExamSection>();
            var currentSec = sections.FirstOrDefault(s => s.Id == attempt.CurrentSectionId);
            var targetSec = sections.FirstOrDefault(s => s.Id == request.NextSectionId);

            if (targetSec == null) throw new ValidationException(new Dictionary<string, string[]> { ["NextSectionId"] = new[] { "Target section does not exist in this exam." } });

            // Enforce CAT forward navigation rule: cannot go backward, cannot skip sections forward
            if (currentSec != null)
            {
                if (targetSec.DisplayOrder <= currentSec.DisplayOrder)
                {
                    throw new ForbiddenException("Backward navigation to previous sections is strictly prohibited in this examination.");
                }
                if (targetSec.DisplayOrder != currentSec.DisplayOrder + 1)
                {
                    throw new ForbiddenException("Direct skipping across sections is prohibited. Sections must be completed sequentially.");
                }
            }

            var sectionDeadline = now.AddMinutes(targetSec.DurationMinutes);
            if (sectionDeadline > attempt.DeadlineUtc)
            {
                sectionDeadline = attempt.DeadlineUtc;
            }

            await _attemptRepo.TransitionSectionAsync(attemptId, targetSec.Id, now, sectionDeadline, ct);
            attempt.CurrentSectionId = targetSec.Id;
            attempt.SectionStartedAtUtc = now;
            attempt.SectionDeadlineUtc = sectionDeadline;

            _logger.LogInformation("Exam attempt {AttemptId} transitioned to Section {SectionId} ({Name})", attemptId, targetSec.Id, targetSec.SectionName);
            return MapActiveAttempt(attempt, now);
        }, ct);
    }

    public async Task<ExamResultDto> SubmitExamAttemptAsync(ulong attemptId, ulong userId, bool isAutoSubmit = false, CancellationToken ct = default)
    {
        return await _userLockRepo.ExecuteWithUserLockAsync(userId, async (conn, tx) =>
        {
            var attempt = await _attemptRepo.GetAttemptByIdAsync(attemptId, userId, conn, tx, ct);
            if (attempt == null) throw new NotFoundException($"Exam attempt {attemptId} not found.");

            if (attempt.Status == "Submitted" || attempt.Status == "AutoSubmitted")
            {
                // Already submitted: return existing result idempotently
                return MapResultDto(attempt);
            }

            var nowUtc = DateTime.UtcNow;
            int attempted = 0;
            int correct = 0;
            int incorrect = 0;
            decimal totalScore = 0m;
            int totalSeconds = 0;

            var scoredQuestions = new List<ExamAttemptQuestion>();
            var attemptsBatch = new List<QuestionAttempt>();

            var sections = attempt.Template?.Sections.OrderBy(s => s.DisplayOrder).ToList() ?? new List<ExamSection>();
            var sectionResultMap = sections.ToDictionary(s => s.Id, s => new ExamAttemptSectionResult
            {
                ExamAttemptId = attempt.Id,
                ExamSectionId = s.Id,
                SectionName = s.SectionName,
                DisplayOrder = s.DisplayOrder,
                MaxScore = s.QuestionCount * s.CorrectMarks
            });

            foreach (var q in attempt.AttemptQuestions)
            {
                totalSeconds += q.TimeSpentSeconds;
                sectionResultMap.TryGetValue(q.ExamSectionId, out var secResult);

                if (secResult != null)
                {
                    secResult.TotalQuestions++;
                    secResult.TimeSpentSeconds += q.TimeSpentSeconds;
                }

                if (!string.IsNullOrWhiteSpace(q.UserAnswer))
                {
                    attempted++;
                    if (secResult != null) secResult.Attempted++;

                    bool isCorrect = EvaluateSnapshotAnswer(q.UserAnswer, q.SnapshotQuestionType, q.SnapshotCorrectAnswer);
                    q.IsCorrect = isCorrect;

                    if (isCorrect)
                    {
                        correct++;
                        totalScore += q.SnapshotMarks;
                        q.MarksAwarded = q.SnapshotMarks;
                        if (secResult != null)
                        {
                            secResult.Correct++;
                            secResult.Score += q.SnapshotMarks;
                        }
                    }
                    else
                    {
                        incorrect++;
                        totalScore -= q.SnapshotPenalty;
                        q.MarksAwarded = -q.SnapshotPenalty;
                        if (secResult != null)
                        {
                            secResult.Incorrect++;
                            secResult.Score -= q.SnapshotPenalty;
                        }
                    }

                    attemptsBatch.Add(new QuestionAttempt
                    {
                        UserId = userId,
                        QuestionId = q.QuestionId,
                        SourceType = "MockExam",
                        SourceId = attempt.Id,
                        SubmittedAnswer = q.UserAnswer,
                        IsCorrect = isCorrect,
                        TimeSpentSeconds = q.TimeSpentSeconds,
                        MarksObtained = q.MarksAwarded
                    });
                }
                else
                {
                    q.IsCorrect = false;
                    q.MarksAwarded = 0m;
                    if (secResult != null) secResult.Unanswered++;
                }

                scoredQuestions.Add(q);
            }

            int unanswered = attempt.TotalQuestions - attempted;
            decimal accuracy = attempted > 0 ? Math.Round(((decimal)correct / attempted) * 100m, 1) : 0m;

            foreach (var sr in sectionResultMap.Values)
            {
                sr.AccuracyPercentage = sr.Attempted > 0 ? Math.Round(((decimal)sr.Correct / sr.Attempted) * 100m, 1) : 0m;
                sr.Score = Math.Max(0m, sr.Score);
            }

            attempt.Status = isAutoSubmit ? "AutoSubmitted" : "Submitted";
            attempt.SubmittedAtUtc = nowUtc;
            attempt.TotalScore = Math.Max(0m, totalScore);
            attempt.TotalAttempted = attempted;
            attempt.TotalCorrect = correct;
            attempt.TotalIncorrect = incorrect;
            attempt.TotalUnanswered = unanswered;
            attempt.AccuracyPercentage = accuracy;
            attempt.TimeSpentSeconds = totalSeconds;

            // Optional Activity Logging in background
            try
            {
                var profile = await _userProfileRepo.GetByUserIdAsync(userId, ct);
                var tzId = profile?.Timezone ?? "Asia/Kolkata";
                var categories = await _categoryRepo.GetCategoriesAsync(userId, true, ct);
                var mockCat = categories.FirstOrDefault(c =>
                    string.Equals(c.CategoryCode, "MOCK_EXAM", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.CategoryCode, "CAT_STUDY", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.CategoryGroup, "CAT Study", StringComparison.OrdinalIgnoreCase))
                    ?? categories.FirstOrDefault();

                if (mockCat != null && totalSeconds >= 60)
                {
                    var durationMin = Math.Max(1, totalSeconds / 60);
                    var act = new ActivityRecord
                    {
                        UserId = userId,
                        ActivityCategoryId = mockCat.Id,
                        ActivityTitle = $"Mock Exam: {attempt.Template?.Title ?? "CAT Test"}",
                        Description = $"Completed examination. Score: {attempt.TotalScore}/{attempt.MaxPossibleScore}. Accuracy: {accuracy}%.",
                        StartAtUtc = attempt.StartedAtUtc,
                        EndAtUtc = nowUtc,
                        TimezoneId = tzId,
                        ActivityStatus = "Completed",
                        EntrySource = "MockExamEngine",
                        IsPlanned = false,
                        CreatedAtUtc = nowUtc,
                        UpdatedAtUtc = nowUtc
                    };

                    var createdAct = await _activityRepo.CreateRecordAsync(act, conn, tx, ct);
                    attempt.ActivityRecordId = createdAct.Id;

                    await _studyDetailRepo.UpsertAsync(new StudySessionDetail
                    {
                        ActivityId = createdAct.Id,
                        StudyMode = "MockExam",
                        DifficultyLevel = "Hard",
                        QuestionsAttempted = (uint)Math.Max(0, attempted),
                        QuestionsCorrect = (uint)Math.Max(0, correct),
                        SessionNotes = $"Score: {attempt.TotalScore}, Accuracy: {accuracy}%"
                    }, conn, tx, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log activity record for exam attempt {AttemptId}", attempt.Id);
            }

            await _attemptRepo.SubmitAttemptAsync(attempt, scoredQuestions, sectionResultMap.Values.ToList(), conn, tx, ct);

            if (attemptsBatch.Count > 0)
            {
                await _qAttemptRepo.RecordAttemptsBatchAsync(attemptsBatch, conn, tx, ct);
            }

            attempt.SectionResults = sectionResultMap.Values.ToList();
            _logger.LogInformation("Exam attempt {AttemptId} submitted by user {UserId}. Status={Status}, Score={Score}",
                attempt.Id, userId, attempt.Status, attempt.TotalScore);

            return MapResultDto(attempt);
        }, ct);
    }

    public async Task<ExamResultDto?> GetExamResultAsync(ulong attemptId, ulong userId, CancellationToken ct = default)
    {
        var attempt = await _attemptRepo.GetAttemptByIdAsync(attemptId, userId, ct: ct);
        if (attempt == null || (attempt.Status != "Submitted" && attempt.Status != "AutoSubmitted")) return null;
        return MapResultDto(attempt);
    }

    public async Task<ExamReviewDto?> GetExamReviewAsync(ulong attemptId, ulong userId, CancellationToken ct = default)
    {
        var attempt = await _attemptRepo.GetAttemptByIdAsync(attemptId, userId, ct: ct);
        if (attempt == null || (attempt.Status != "Submitted" && attempt.Status != "AutoSubmitted")) return null;

        var sectionNameMap = attempt.Template?.Sections.ToDictionary(s => s.Id, s => s.SectionName) ?? new();

        var reviewQuestions = attempt.AttemptQuestions.Select(q =>
        {
            sectionNameMap.TryGetValue(q.ExamSectionId, out var secName);

            var options = new List<QuestionOptionDto>();
            if (!string.IsNullOrWhiteSpace(q.SnapshotOptionsJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(q.SnapshotOptionsJson);
                    int order = 1;
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        var key = elem.GetProperty("OptionKey").GetString() ?? "";
                        var content = elem.GetProperty("Content").GetString() ?? "";
                        options.Add(new QuestionOptionDto
                        {
                            OptionKey = key,
                            Content = content,
                            DisplayOrder = order++,
                            IsCorrect = string.Equals(key, q.SnapshotCorrectAnswer, StringComparison.OrdinalIgnoreCase)
                        });
                    }
                }
                catch { }
            }

            QuestionPassageDto? passage = null;
            if (!string.IsNullOrWhiteSpace(q.SnapshotPassageJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(q.SnapshotPassageJson);
                    passage = new QuestionPassageDto
                    {
                        Title = doc.RootElement.GetProperty("Title").GetString() ?? "",
                        Content = doc.RootElement.GetProperty("Content").GetString() ?? "",
                        WordCount = doc.RootElement.TryGetProperty("WordCount", out var wc) ? wc.GetInt32() : 0,
                        Genre = doc.RootElement.TryGetProperty("Genre", out var g) ? g.GetString() : null
                    };
                }
                catch { }
            }

            QuestionSetDto? qset = null;
            if (!string.IsNullOrWhiteSpace(q.SnapshotSetJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(q.SnapshotSetJson);
                    qset = new QuestionSetDto
                    {
                        Title = doc.RootElement.GetProperty("Title").GetString() ?? "",
                        ContextScenario = doc.RootElement.GetProperty("ContextScenario").GetString() ?? "",
                        DataPayloadJson = doc.RootElement.TryGetProperty("DataPayloadJson", out var dp) ? dp.GetString() : null
                    };
                }
                catch { }
            }

            return new ExamReviewQuestionDto
            {
                QuestionId = q.QuestionId,
                ExamSectionId = q.ExamSectionId,
                SectionName = secName ?? "Section",
                DisplayOrder = q.DisplayOrder,
                QuestionType = q.SnapshotQuestionType,
                Content = q.SnapshotContent,
                Options = options,
                Passage = passage,
                QuestionSet = qset,
                UserAnswer = q.UserAnswer,
                CorrectAnswer = q.SnapshotCorrectAnswer,
                IsCorrect = q.IsCorrect,
                MarksAwarded = q.MarksAwarded,
                MaxMarks = q.SnapshotMarks,
                Penalty = q.SnapshotPenalty,
                Explanation = q.SnapshotExplanation,
                SolutionSteps = q.SnapshotSolutionSteps,
                TimeSpentSeconds = q.TimeSpentSeconds
            };
        }).ToList();

        return new ExamReviewDto
        {
            Summary = MapResultDto(attempt),
            Questions = reviewQuestions
        };
    }

    public async Task<IReadOnlyList<ExamAttempt>> GetRecentExamAttemptsAsync(ulong userId, int limit = 20, CancellationToken ct = default)
    {
        return await _attemptRepo.GetRecentAttemptsForUserAsync(userId, limit, ct);
    }

    public async Task<int> AutoSubmitExpiredAttemptsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expired = await _attemptRepo.GetExpiredInProgressAttemptsAsync(now, ct);
        int count = 0;

        foreach (var a in expired)
        {
            try
            {
                await SubmitExamAttemptAsync(a.Id, a.UserId, isAutoSubmit: true, ct);
                count++;
                _logger.LogInformation("Auto-submitted expired exam attempt {AttemptId} for user {UserId}", a.Id, a.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-submit expired attempt {AttemptId}", a.Id);
            }
        }

        return count;
    }

    private async Task<ActiveExamAttemptDto> EvaluateAttemptTimingAndTransitionAsync(ExamAttempt attempt, ulong userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // 1. Check total deadline
        if (attempt.DeadlineUtc <= now)
        {
            _logger.LogInformation("Exam attempt {AttemptId} reached total deadline. Auto-submitting.", attempt.Id);
            await SubmitExamAttemptAsync(attempt.Id, userId, isAutoSubmit: true, ct);
            return MapActiveAttempt(attempt, now);
        }

        // 2. Check section deadline (handle multi-section expiry if disconnected)
        while (attempt.SectionDeadlineUtc <= now)
        {
            var sections = attempt.Template?.Sections.OrderBy(s => s.DisplayOrder).ToList() ?? new List<ExamSection>();
            var curIndex = sections.FindIndex(s => s.Id == attempt.CurrentSectionId);

            if (curIndex >= 0 && curIndex < sections.Count - 1)
            {
                // Auto-transition to next section
                var nextSec = sections[curIndex + 1];
                var secDeadline = now.AddMinutes(nextSec.DurationMinutes);
                if (secDeadline > attempt.DeadlineUtc) secDeadline = attempt.DeadlineUtc;

                await _attemptRepo.TransitionSectionAsync(attempt.Id, nextSec.Id, now, secDeadline, ct);
                attempt.CurrentSectionId = nextSec.Id;
                attempt.SectionStartedAtUtc = now;
                attempt.SectionDeadlineUtc = secDeadline;
                _logger.LogInformation("Exam attempt {AttemptId} section timed out. Auto-transitioned to {NextSectionId}", attempt.Id, nextSec.Id);
            }
            else
            {
                // Final section timed out: auto-submit exam
                await SubmitExamAttemptAsync(attempt.Id, userId, isAutoSubmit: true, ct);
                return MapActiveAttempt(attempt, now);
            }
        }

        return MapActiveAttempt(attempt, now);
    }

    private static bool EvaluateSnapshotAnswer(string userAnswer, string questionType, string correctAnswer)
    {
        var cleanUser = userAnswer.Trim();
        var cleanCorrect = correctAnswer.Trim();

        if (string.Equals(questionType, "MCQ", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(cleanUser, cleanCorrect, StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(cleanUser, cleanCorrect, StringComparison.OrdinalIgnoreCase)) return true;

        if (decimal.TryParse(cleanUser, out var uVal) && decimal.TryParse(cleanCorrect, out var cVal))
        {
            return Math.Abs(uVal - cVal) < 0.0001m;
        }

        return false;
    }

    private static ExamTemplateDto MapTemplateDto(ExamTemplate t)
    {
        return new ExamTemplateDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            ExamType = t.ExamType,
            TotalDurationMinutes = t.TotalDurationMinutes,
            TotalQuestions = t.TotalQuestions,
            TotalMarks = t.TotalMarks,
            Instructions = t.Instructions,
            IsPublished = t.IsPublished,
            Sections = t.Sections.Select(s => new ExamSectionDto
            {
                Id = s.Id,
                ExamTemplateId = s.ExamTemplateId,
                SectionName = s.SectionName,
                SubjectId = s.SubjectId,
                DisplayOrder = s.DisplayOrder,
                DurationMinutes = s.DurationMinutes,
                QuestionCount = s.QuestionCount,
                CanNavigateBack = s.CanNavigateBack,
                CorrectMarks = s.CorrectMarks,
                IncorrectPenalty = s.IncorrectPenalty
            }).ToList()
        };
    }

    private static ActiveExamAttemptDto MapActiveAttempt(ExamAttempt a, DateTime nowUtc)
    {
        var sections = a.Template?.Sections.OrderBy(s => s.DisplayOrder).ToList() ?? new List<ExamSection>();
        var curSec = sections.FirstOrDefault(s => s.Id == a.CurrentSectionId) ?? sections.FirstOrDefault();
        var curIndex = curSec != null ? sections.IndexOf(curSec) : 0;

        var remTotal = Math.Max(0, (long)(a.DeadlineUtc - nowUtc).TotalSeconds);
        var remSec = Math.Max(0, (long)(a.SectionDeadlineUtc - nowUtc).TotalSeconds);

        var questions = a.AttemptQuestions.Select(q =>
        {
            var options = new List<QuestionOptionDto>();
            if (!string.IsNullOrWhiteSpace(q.SnapshotOptionsJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(q.SnapshotOptionsJson);
                    int oIndex = 1;
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        options.Add(new QuestionOptionDto
                        {
                            OptionKey = elem.GetProperty("OptionKey").GetString() ?? "",
                            Content = elem.GetProperty("Content").GetString() ?? "",
                            DisplayOrder = oIndex++
                        });
                    }
                }
                catch { }
            }

            QuestionPassageDto? passage = null;
            if (!string.IsNullOrWhiteSpace(q.SnapshotPassageJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(q.SnapshotPassageJson);
                    passage = new QuestionPassageDto
                    {
                        Title = doc.RootElement.GetProperty("Title").GetString() ?? "",
                        Content = doc.RootElement.GetProperty("Content").GetString() ?? "",
                        WordCount = doc.RootElement.TryGetProperty("WordCount", out var wc) ? wc.GetInt32() : 0,
                        Genre = doc.RootElement.TryGetProperty("Genre", out var g) ? g.GetString() : null
                    };
                }
                catch { }
            }

            QuestionSetDto? qset = null;
            if (!string.IsNullOrWhiteSpace(q.SnapshotSetJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(q.SnapshotSetJson);
                    qset = new QuestionSetDto
                    {
                        Title = doc.RootElement.GetProperty("Title").GetString() ?? "",
                        ContextScenario = doc.RootElement.GetProperty("ContextScenario").GetString() ?? "",
                        DataPayloadJson = doc.RootElement.TryGetProperty("DataPayloadJson", out var dp) ? dp.GetString() : null
                    };
                }
                catch { }
            }

            return new ExamAttemptQuestionItemDto
            {
                Id = q.Id,
                ExamSectionId = q.ExamSectionId,
                QuestionId = q.QuestionId,
                DisplayOrder = q.DisplayOrder,
                QuestionType = q.SnapshotQuestionType,
                Content = q.SnapshotContent,
                Options = options,
                Passage = passage,
                QuestionSet = qset,
                Marks = q.SnapshotMarks,
                Penalty = q.SnapshotPenalty,
                UserAnswer = q.UserAnswer,
                AnswerStatus = q.AnswerStatus,
                TimeSpentSeconds = q.TimeSpentSeconds,
                Version = q.Version
            };
        }).ToList();

        return new ActiveExamAttemptDto
        {
            Id = a.Id,
            ExamTemplateId = a.ExamTemplateId,
            TemplateTitle = a.Template?.Title ?? "CAT Examination",
            Status = a.Status,
            ServerTimeUtc = nowUtc,
            StartedAtUtc = a.StartedAtUtc,
            DeadlineUtc = a.DeadlineUtc,
            RemainingSecondsTotal = remTotal,
            CurrentSectionId = curSec?.Id ?? 0,
            CurrentSectionName = curSec?.SectionName ?? "Section",
            CurrentSectionIndex = curIndex,
            TotalSections = sections.Count,
            SectionStartedAtUtc = a.SectionStartedAtUtc,
            SectionDeadlineUtc = a.SectionDeadlineUtc,
            RemainingSecondsCurrentSection = remSec,
            CurrentQuestionIndex = a.CurrentQuestionIndex,
            Sections = sections.Select(s => new ExamSectionDto
            {
                Id = s.Id,
                ExamTemplateId = s.ExamTemplateId,
                SectionName = s.SectionName,
                SubjectId = s.SubjectId,
                DisplayOrder = s.DisplayOrder,
                DurationMinutes = s.DurationMinutes,
                QuestionCount = s.QuestionCount,
                CanNavigateBack = s.CanNavigateBack,
                CorrectMarks = s.CorrectMarks,
                IncorrectPenalty = s.IncorrectPenalty
            }).ToList(),
            Questions = questions
        };
    }

    private static ExamResultDto MapResultDto(ExamAttempt a)
    {
        return new ExamResultDto
        {
            AttemptId = a.Id,
            ExamTemplateId = a.ExamTemplateId,
            ExamTitle = a.Template?.Title ?? "CAT Examination",
            Status = a.Status,
            TotalScore = a.TotalScore,
            MaxPossibleScore = a.MaxPossibleScore,
            TotalQuestions = a.TotalQuestions,
            TotalAttempted = a.TotalAttempted,
            TotalCorrect = a.TotalCorrect,
            TotalIncorrect = a.TotalIncorrect,
            TotalUnanswered = a.TotalUnanswered,
            AccuracyPercentage = a.AccuracyPercentage,
            TimeSpentSeconds = a.TimeSpentSeconds,
            StartedAtUtc = a.StartedAtUtc,
            SubmittedAtUtc = a.SubmittedAtUtc ?? DateTime.UtcNow,
            SectionResults = a.SectionResults.Select(r => new ExamSectionResultDto
            {
                ExamSectionId = r.ExamSectionId,
                SectionName = r.SectionName,
                DisplayOrder = r.DisplayOrder,
                Score = r.Score,
                MaxScore = r.MaxScore,
                TotalQuestions = r.TotalQuestions,
                Attempted = r.Attempted,
                Correct = r.Correct,
                Incorrect = r.Incorrect,
                Unanswered = r.Unanswered,
                AccuracyPercentage = r.AccuracyPercentage,
                TimeSpentSeconds = r.TimeSpentSeconds
            }).ToList()
        };
    }
}
