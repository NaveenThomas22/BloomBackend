using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Bloom.BLL.Services;

public class PracticeSessionService : IPracticeSessionService
{
    private readonly IPracticeSessionRepository _sessionRepo;
    private readonly IQuestionRepository _questionRepo;
    private readonly IQuestionAttemptRepository _attemptRepo;
    private readonly IActivityCategoryRepository _categoryRepo;
    private readonly IActivityRecordRepository _activityRepo;
    private readonly IStudySessionDetailRepository _studyDetailRepo;
    private readonly IUserProfileRepository _userProfileRepo;
    private readonly IValidator<StartPracticeSessionRequest> _startValidator;
    private readonly IValidator<SavePracticeAnswerRequest> _saveAnswerValidator;
    private readonly ILogger<PracticeSessionService> _logger;

    public PracticeSessionService(
        IPracticeSessionRepository sessionRepo,
        IQuestionRepository questionRepo,
        IQuestionAttemptRepository attemptRepo,
        IActivityCategoryRepository categoryRepo,
        IActivityRecordRepository activityRepo,
        IStudySessionDetailRepository studyDetailRepo,
        IUserProfileRepository userProfileRepo,
        IValidator<StartPracticeSessionRequest> startValidator,
        IValidator<SavePracticeAnswerRequest> saveAnswerValidator,
        ILogger<PracticeSessionService> logger)
    {
        _sessionRepo = sessionRepo;
        _questionRepo = questionRepo;
        _attemptRepo = attemptRepo;
        _categoryRepo = categoryRepo;
        _activityRepo = activityRepo;
        _studyDetailRepo = studyDetailRepo;
        _userProfileRepo = userProfileRepo;
        _startValidator = startValidator;
        _saveAnswerValidator = saveAnswerValidator;
        _logger = logger;
    }

    public async Task<PracticeSessionDto> StartSessionAsync(
        ulong userId, StartPracticeSessionRequest request, CancellationToken ct = default)
    {
        var val = await _startValidator.ValidateAsync(request, ct);
        if (!val.IsValid)
        {
            var errors = val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Bloom.Core.Exceptions.ValidationException(errors);
        }

        IReadOnlyList<Question> questions;

        if (string.Equals(request.PracticeMode, "DILRSet", StringComparison.OrdinalIgnoreCase) && request.QuestionSetId.HasValue)
        {
            questions = await _questionRepo.GetQuestionsBySetIdAsync(request.QuestionSetId.Value, ct);
        }
        else if (string.Equals(request.PracticeMode, "VARCPassage", StringComparison.OrdinalIgnoreCase) && request.PassageId.HasValue)
        {
            questions = await _questionRepo.GetQuestionsByPassageIdAsync(request.PassageId.Value, ct);
        }
        else
        {
            questions = await _questionRepo.GetQuestionsForPracticeAsync(
                request.SubjectId, request.TopicId, request.DifficultyLevel, request.QuestionType, request.QuestionCount, ct);
        }

        if (questions.Count == 0)
        {
            throw new Bloom.Core.Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["Questions"] = new[] { "No eligible published questions found matching your practice criteria." }
            });
        }

        var session = new PracticeSession
        {
            UserId = userId,
            Title = request.Title.Trim(),
            PracticeMode = request.PracticeMode,
            SubjectId = request.SubjectId ?? questions[0].SubjectId,
            TopicId = request.TopicId ?? questions[0].TopicId,
            DifficultyLevel = request.DifficultyLevel,
            TotalQuestions = questions.Count,
            TimeLimitSeconds = request.TimeLimitMinutes.HasValue ? request.TimeLimitMinutes.Value * 60 : null,
            Status = "InProgress",
            StartedAtUtc = DateTime.UtcNow
        };

        var sessionQuestions = questions.Select((q, index) => new PracticeSessionQuestion
        {
            QuestionId = q.Id,
            DisplayOrder = index + 1,
            IsAnswered = false,
            IsMarkedForReview = false,
            TimeSpentSeconds = 0,
            Question = q
        }).ToList();

        var created = await _sessionRepo.CreateSessionAsync(session, sessionQuestions, ct);
        _logger.LogInformation("User {UserId} started practice session {SessionId} with {Count} questions", userId, created.Id, created.TotalQuestions);

        return MapToDto(created);
    }

    public async Task<PracticeSessionDto?> GetSessionByIdAsync(ulong sessionId, ulong userId, CancellationToken ct = default)
    {
        var session = await _sessionRepo.GetSessionByIdAsync(sessionId, userId, ct);
        return session == null ? null : MapToDto(session);
    }

    public async Task<IReadOnlyList<PracticeSessionDto>> GetRecentSessionsAsync(ulong userId, int limit = 20, CancellationToken ct = default)
    {
        var list = await _sessionRepo.GetRecentSessionsAsync(userId, limit, ct);
        return list.Select(MapToDto).ToList();
    }

    public async Task<bool> SaveAnswerAsync(
        ulong sessionId, ulong userId, SavePracticeAnswerRequest request, CancellationToken ct = default)
    {
        var val = await _saveAnswerValidator.ValidateAsync(request, ct);
        if (!val.IsValid)
        {
            var errors = val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Bloom.Core.Exceptions.ValidationException(errors);
        }

        var session = await _sessionRepo.GetSessionByIdAsync(sessionId, userId, ct);
        if (session == null) throw new NotFoundException($"Practice session {sessionId} not found.");
        if (session.Status == "Completed") throw new ConflictException("Cannot modify answers for a completed practice session.");

        return await _sessionRepo.SaveSessionAnswerAsync(
            sessionId, request.QuestionId, request.UserAnswer?.Trim(), request.IsMarkedForReview, request.TimeSpentSeconds, ct);
    }

    public async Task<SubmitPracticeSessionResponse> SubmitSessionAsync(ulong sessionId, ulong userId, CancellationToken ct = default)
    {
        var session = await _sessionRepo.GetSessionByIdAsync(sessionId, userId, ct);
        if (session == null) throw new NotFoundException($"Practice session {sessionId} not found.");

        if (session.Status == "Completed")
        {
            // Already submitted, return existing result idempotently
            return MapToSubmitResponse(session);
        }

        // Evaluate answers against real questions
        int attempted = 0;
        int correct = 0;
        int incorrect = 0;
        decimal totalScore = 0m;
        int totalSeconds = 0;

        var attemptsToRecord = new List<QuestionAttempt>();

        foreach (var sq in session.Questions)
        {
            totalSeconds += sq.TimeSpentSeconds;
            var q = await _questionRepo.GetQuestionByIdAsync(sq.QuestionId, includeAnswerKey: true, ct);
            if (q == null) continue;

            if (string.IsNullOrWhiteSpace(sq.UserAnswer))
            {
                sq.IsAnswered = false;
                sq.IsCorrect = false;
            }
            else
            {
                sq.IsAnswered = true;
                attempted++;

                bool isCorrect = EvaluateAnswer(sq.UserAnswer, q);
                sq.IsCorrect = isCorrect;

                if (isCorrect)
                {
                    correct++;
                    totalScore += q.DefaultMarks;
                }
                else
                {
                    incorrect++;
                    totalScore -= q.DefaultPenalty;
                }

                attemptsToRecord.Add(new QuestionAttempt
                {
                    UserId = userId,
                    QuestionId = q.Id,
                    SourceType = "Practice",
                    SourceId = session.Id,
                    SubmittedAnswer = sq.UserAnswer,
                    IsCorrect = isCorrect,
                    TimeSpentSeconds = sq.TimeSpentSeconds,
                    MarksObtained = isCorrect ? q.DefaultMarks : -q.DefaultPenalty,
                    TopicId = q.TopicId
                });
            }

            // Populate question for response review
            sq.Question = q;
        }

        int unanswered = session.TotalQuestions - attempted;
        decimal accuracy = attempted > 0 ? Math.Round(((decimal)correct / attempted) * 100m, 1) : 0m;

        session.AttemptedQuestions = attempted;
        session.CorrectAnswers = correct;
        session.IncorrectAnswers = incorrect;
        session.UnansweredQuestions = unanswered;
        session.Score = Math.Max(0m, totalScore);
        session.AccuracyPercentage = accuracy;
        session.TimeSpentSeconds = totalSeconds;
        session.Status = "Completed";

        // Optional Activity Recording in background / integration
        try
        {
            var profile = await _userProfileRepo.GetByUserIdAsync(userId, ct);
            var tzId = profile?.Timezone ?? "Asia/Kolkata";
            var categories = await _categoryRepo.GetCategoriesAsync(userId, true, ct);
            var practiceCat = categories.FirstOrDefault(c =>
                string.Equals(c.CategoryCode, "QUESTION_PRACTICE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.CategoryCode, "CAT_STUDY", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.CategoryGroup, "CAT Study", StringComparison.OrdinalIgnoreCase))
                ?? categories.FirstOrDefault();

            if (practiceCat != null && totalSeconds >= 60)
            {
                var now = DateTime.UtcNow;
                var durationMinutes = Math.Max(1, totalSeconds / 60);
                var actRecord = new ActivityRecord
                {
                    UserId = userId,
                    ActivityCategoryId = practiceCat.Id,
                    ActivityTitle = $"Practice: {session.Title}",
                    Description = $"Practiced {attempted}/{session.TotalQuestions} questions. Score: {session.Score}, Accuracy: {accuracy}%.",
                    StartAtUtc = now.AddMinutes(-durationMinutes),
                    EndAtUtc = now,
                    TimezoneId = tzId,
                    ActivityStatus = "Completed",
                    EntrySource = "PracticeEngine",
                    IsPlanned = false,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };

                var createdAct = await _activityRepo.CreateRecordAsync(actRecord, ct: ct);
                session.ActivityRecordId = createdAct.Id;

                if (session.TopicId.HasValue)
                {
                    await _studyDetailRepo.UpsertAsync(new StudySessionDetail
                    {
                        ActivityId = createdAct.Id,
                        TopicId = session.TopicId.Value,
                        StudyMode = "QuestionPractice",
                        DifficultyLevel = session.DifficultyLevel ?? "Medium",
                        QuestionsAttempted = (uint)Math.Max(0, attempted),
                        QuestionsCorrect = (uint)Math.Max(0, correct),
                        SessionNotes = $"Accuracy: {accuracy}%"
                    }, ct: ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create automatic activity record for practice session {SessionId}", session.Id);
        }

        await _sessionRepo.CompleteSessionAsync(session, session.Questions, ct);

        if (attemptsToRecord.Count > 0)
        {
            await _attemptRepo.RecordAttemptsBatchAsync(attemptsToRecord, ct: ct);
        }

        _logger.LogInformation("Practice session {SessionId} submitted by user {UserId}. Score={Score}, Accuracy={Accuracy}%",
            session.Id, userId, session.Score, accuracy);

        return MapToSubmitResponse(session);
    }

    public static bool EvaluateAnswer(string userAnswer, Question q)
    {
        var cleanUser = userAnswer.Trim();
        var cleanCorrect = q.CorrectAnswer.Trim();

        if (string.Equals(q.QuestionType, "MCQ", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(cleanUser, cleanCorrect, StringComparison.OrdinalIgnoreCase);
        }

        // TITA Evaluation: Check exact string or numerical equivalence
        if (string.Equals(cleanUser, cleanCorrect, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (decimal.TryParse(cleanUser, out var userVal) && decimal.TryParse(cleanCorrect, out var correctVal))
        {
            if (q.TitaNumericRangeMin.HasValue && q.TitaNumericRangeMax.HasValue)
            {
                return userVal >= q.TitaNumericRangeMin.Value && userVal <= q.TitaNumericRangeMax.Value;
            }
            return Math.Abs(userVal - correctVal) < 0.0001m;
        }

        return false;
    }

    private static PracticeSessionDto MapToDto(PracticeSession s)
    {
        return new PracticeSessionDto
        {
            Id = s.Id,
            UserId = s.UserId,
            Title = s.Title,
            PracticeMode = s.PracticeMode,
            SubjectId = s.SubjectId,
            SubjectName = s.SubjectName,
            TopicId = s.TopicId,
            TopicName = s.TopicName,
            DifficultyLevel = s.DifficultyLevel,
            TotalQuestions = s.TotalQuestions,
            AttemptedQuestions = s.AttemptedQuestions,
            CorrectAnswers = s.CorrectAnswers,
            IncorrectAnswers = s.IncorrectAnswers,
            UnansweredQuestions = s.UnansweredQuestions,
            Score = s.Score,
            AccuracyPercentage = s.AccuracyPercentage,
            TimeSpentSeconds = s.TimeSpentSeconds,
            TimeLimitSeconds = s.TimeLimitSeconds,
            Status = s.Status,
            StartedAtUtc = s.StartedAtUtc,
            CompletedAtUtc = s.CompletedAtUtc,
            Questions = s.Questions.Select(q => new PracticeQuestionItemDto
            {
                Id = q.Id,
                QuestionId = q.QuestionId,
                DisplayOrder = q.DisplayOrder,
                UserAnswer = q.UserAnswer,
                IsAnswered = q.IsAnswered,
                IsCorrect = q.IsCorrect,
                IsMarkedForReview = q.IsMarkedForReview,
                TimeSpentSeconds = q.TimeSpentSeconds,
                Question = MapQuestionDto(q.Question, s.Status == "Completed")
            }).ToList()
        };
    }

    private static SubmitPracticeSessionResponse MapToSubmitResponse(PracticeSession s)
    {
        return new SubmitPracticeSessionResponse
        {
            SessionId = s.Id,
            Status = s.Status,
            TotalQuestions = s.TotalQuestions,
            AttemptedQuestions = s.AttemptedQuestions,
            CorrectAnswers = s.CorrectAnswers,
            IncorrectAnswers = s.IncorrectAnswers,
            UnansweredQuestions = s.UnansweredQuestions,
            Score = s.Score,
            AccuracyPercentage = s.AccuracyPercentage,
            TotalTimeSpentSeconds = s.TimeSpentSeconds,
            Questions = s.Questions.Select(q => new PracticeQuestionItemDto
            {
                Id = q.Id,
                QuestionId = q.QuestionId,
                DisplayOrder = q.DisplayOrder,
                UserAnswer = q.UserAnswer,
                IsAnswered = q.IsAnswered,
                IsCorrect = q.IsCorrect,
                IsMarkedForReview = q.IsMarkedForReview,
                TimeSpentSeconds = q.TimeSpentSeconds,
                Question = MapQuestionDto(q.Question, true)
            }).ToList()
        };
    }

    private static QuestionDto MapQuestionDto(Question? q, bool includeProtectedData)
    {
        if (q == null) return new QuestionDto();
        return new QuestionDto
        {
            Id = q.Id,
            SubjectId = q.SubjectId,
            ModuleId = q.ModuleId,
            TopicId = q.TopicId,
            SubtopicId = q.SubtopicId,
            QuestionSetId = q.QuestionSetId,
            PassageId = q.PassageId,
            QuestionType = q.QuestionType,
            DifficultyLevel = q.DifficultyLevel,
            QuestionContent = q.QuestionContent,
            QuestionImageUrl = q.QuestionImageUrl,
            EstimatedSeconds = q.EstimatedSeconds,
            DefaultMarks = q.DefaultMarks,
            DefaultPenalty = q.DefaultPenalty,
            Tags = q.Tags,
            Status = q.Status,
            CorrectAnswer = includeProtectedData ? q.CorrectAnswer : string.Empty,
            Explanation = includeProtectedData ? q.Explanation : null,
            SolutionSteps = includeProtectedData ? q.SolutionSteps : null,
            Options = q.Options.Select(o => new QuestionOptionDto
            {
                Id = o.Id,
                OptionKey = o.OptionKey,
                Content = o.Content,
                DisplayOrder = o.DisplayOrder,
                IsCorrect = includeProtectedData ? o.IsCorrect : null
            }).ToList(),
            Passage = q.Passage == null ? null : new QuestionPassageDto
            {
                Id = q.Passage.Id,
                Title = q.Passage.Title,
                Content = q.Passage.Content,
                WordCount = q.Passage.WordCount,
                Genre = q.Passage.Genre,
                SourceAttribution = q.Passage.SourceAttribution,
                IsActive = q.Passage.IsActive
            },
            QuestionSet = q.QuestionSet == null ? null : new QuestionSetDto
            {
                Id = q.QuestionSet.Id,
                Title = q.QuestionSet.Title,
                ContextScenario = q.QuestionSet.ContextScenario,
                DataPayloadJson = q.QuestionSet.DataPayloadJson,
                SetType = q.QuestionSet.SetType,
                SourceAttribution = q.QuestionSet.SourceAttribution,
                IsActive = q.QuestionSet.IsActive
            }
        };
    }
}
