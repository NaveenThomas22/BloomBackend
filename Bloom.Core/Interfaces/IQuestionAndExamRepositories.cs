using System.Data;
using Bloom.Core.Entities;

namespace Bloom.Core.Interfaces;

public record QuestionFilter(
    ulong? SubjectId = null,
    ulong? ModuleId = null,
    ulong? TopicId = null,
    ulong? SubtopicId = null,
    string? QuestionType = null,
    string? DifficultyLevel = null,
    string? Status = "Published",
    string? SearchTerm = null
);

public interface IQuestionRepository
{
    Task<Question?> GetQuestionByIdAsync(ulong id, bool includeAnswerKey = false, CancellationToken ct = default);
    Task<(IReadOnlyList<Question> Questions, int TotalCount)> GetQuestionsPagedAsync(QuestionFilter filter, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<IReadOnlyList<Question>> GetQuestionsForPracticeAsync(ulong? subjectId, ulong? topicId, string? difficulty, string? questionType, int count, CancellationToken ct = default);
    Task<IReadOnlyList<Question>> GetQuestionsByPassageIdAsync(ulong passageId, CancellationToken ct = default);
    Task<IReadOnlyList<Question>> GetQuestionsBySetIdAsync(ulong setId, CancellationToken ct = default);
    Task<Question> CreateQuestionAsync(Question question, List<QuestionOption> options, CancellationToken ct = default);
    Task<bool> UpdateQuestionAsync(Question question, List<QuestionOption>? options = null, CancellationToken ct = default);
    Task<bool> UpdateQuestionStatusAsync(ulong questionId, string status, CancellationToken ct = default);
}

public interface IQuestionPassageRepository
{
    Task<QuestionPassage?> GetPassageByIdAsync(ulong id, CancellationToken ct = default);
    Task<IReadOnlyList<QuestionPassage>> GetPassagesAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<QuestionPassage> CreatePassageAsync(QuestionPassage passage, CancellationToken ct = default);
    Task<bool> UpdatePassageAsync(QuestionPassage passage, CancellationToken ct = default);
}

public interface IQuestionSetRepository
{
    Task<QuestionSet?> GetSetByIdAsync(ulong id, CancellationToken ct = default);
    Task<IReadOnlyList<QuestionSet>> GetSetsAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<QuestionSet> CreateSetAsync(QuestionSet set, CancellationToken ct = default);
    Task<bool> UpdateSetAsync(QuestionSet set, CancellationToken ct = default);
}

public interface IUserQuestionBookmarkRepository
{
    Task<(IReadOnlyList<Question> Questions, int TotalCount)> GetBookmarksAsync(ulong userId, ulong? subjectId = null, ulong? topicId = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<bool> IsBookmarkedAsync(ulong userId, ulong questionId, CancellationToken ct = default);
    Task<bool> AddBookmarkAsync(ulong userId, ulong questionId, string? note = null, CancellationToken ct = default);
    Task<bool> RemoveBookmarkAsync(ulong userId, ulong questionId, CancellationToken ct = default);
    Task<HashSet<ulong>> GetBookmarkedQuestionIdsAsync(ulong userId, IEnumerable<ulong> questionIds, CancellationToken ct = default);
}

public interface IPracticeSessionRepository
{
    Task<PracticeSession> CreateSessionAsync(PracticeSession session, List<PracticeSessionQuestion> sessionQuestions, CancellationToken ct = default);
    Task<PracticeSession?> GetSessionByIdAsync(ulong sessionId, ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<PracticeSession>> GetRecentSessionsAsync(ulong userId, int limit = 20, CancellationToken ct = default);
    Task<bool> SaveSessionAnswerAsync(ulong sessionId, ulong questionId, string? answer, bool isMarkedForReview, int timeSpentSeconds, CancellationToken ct = default);
    Task<bool> CompleteSessionAsync(PracticeSession session, List<PracticeSessionQuestion> updatedQuestions, CancellationToken ct = default);
}

public interface IQuestionAttemptRepository
{
    Task RecordAttemptsBatchAsync(IEnumerable<QuestionAttempt> attempts, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<IReadOnlyList<QuestionAttempt>> GetAttemptsByUserIdAsync(ulong userId, int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<QuestionAttempt>> GetAttemptsForQuestionAsync(ulong userId, ulong questionId, CancellationToken ct = default);
    Task<(int TotalAttempts, int CorrectAttempts, decimal AccuracyPct)> GetTopicAccuracySummaryAsync(ulong userId, ulong topicId, CancellationToken ct = default);
}

public interface IExamTemplateRepository
{
    Task<IReadOnlyList<ExamTemplate>> GetTemplatesAsync(string? examType = null, bool activeOnly = true, CancellationToken ct = default);
    Task<ExamTemplate?> GetTemplateWithSectionsAsync(ulong templateId, CancellationToken ct = default);
    Task<ExamTemplate> CreateTemplateAsync(ExamTemplate template, List<ExamSection> sections, CancellationToken ct = default);
    Task<bool> UpdateTemplateAsync(ExamTemplate template, CancellationToken ct = default);
}

public interface IExamAttemptRepository
{
    Task<ExamAttempt> CreateAttemptAsync(ExamAttempt attempt, List<ExamAttemptQuestion> questions, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<ExamAttempt?> GetAttemptByIdAsync(ulong attemptId, ulong userId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<ExamAttempt?> GetActiveAttemptForUserAsync(ulong userId, ulong? templateId = null, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<IReadOnlyList<ExamAttempt>> GetRecentAttemptsForUserAsync(ulong userId, int limit = 20, CancellationToken ct = default);
    Task<bool> SaveQuestionAnswerAsync(ulong attemptId, ulong questionId, string? answer, string answerStatus, int timeSpentSeconds, uint expectedVersion, CancellationToken ct = default);
    Task<bool> TransitionSectionAsync(ulong attemptId, ulong nextSectionId, DateTime sectionStartTime, DateTime sectionDeadline, CancellationToken ct = default);
    Task<bool> SubmitAttemptAsync(ExamAttempt attempt, List<ExamAttemptQuestion> scoredQuestions, List<ExamAttemptSectionResult> sectionResults, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<IReadOnlyList<ExamAttempt>> GetExpiredInProgressAttemptsAsync(DateTime nowUtc, CancellationToken ct = default);
}

