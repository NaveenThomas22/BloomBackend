using Bloom.BLL.DTOs;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;

namespace Bloom.BLL.Interfaces;

public interface IQuestionService
{
    Task<QuestionDto?> GetQuestionByIdAsync(ulong id, ulong userId, bool isEditor = false, CancellationToken ct = default);
    Task<(IReadOnlyList<QuestionDto> Questions, int TotalCount)> GetQuestionsPagedAsync(QuestionFilter filter, ulong userId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<QuestionDto> CreateQuestionAsync(CreateQuestionRequest request, CancellationToken ct = default);
    Task<QuestionDto> UpdateQuestionAsync(ulong id, UpdateQuestionRequest request, CancellationToken ct = default);
    Task<bool> PublishQuestionAsync(ulong id, CancellationToken ct = default);
    Task<bool> ArchiveQuestionAsync(ulong id, CancellationToken ct = default);
}

public interface IQuestionPassageService
{
    Task<QuestionPassageDto?> GetPassageByIdAsync(ulong id, CancellationToken ct = default);
    Task<IReadOnlyList<QuestionPassageDto>> GetPassagesAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<QuestionPassageDto> CreatePassageAsync(CreatePassageRequest request, CancellationToken ct = default);
}

public interface IQuestionSetService
{
    Task<QuestionSetDto?> GetSetByIdAsync(ulong id, CancellationToken ct = default);
    Task<IReadOnlyList<QuestionSetDto>> GetSetsAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<QuestionSetDto> CreateSetAsync(CreateQuestionSetRequest request, CancellationToken ct = default);
}

public interface IBookmarkService
{
    Task<(IReadOnlyList<QuestionDto> Questions, int TotalCount)> GetBookmarksAsync(ulong userId, ulong? subjectId = null, ulong? topicId = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<bool> AddBookmarkAsync(ulong userId, ulong questionId, AddBookmarkRequest request, CancellationToken ct = default);
    Task<bool> RemoveBookmarkAsync(ulong userId, ulong questionId, CancellationToken ct = default);
}

public interface IPracticeSessionService
{
    Task<PracticeSessionDto> StartSessionAsync(ulong userId, StartPracticeSessionRequest request, CancellationToken ct = default);
    Task<PracticeSessionDto?> GetSessionByIdAsync(ulong sessionId, ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<PracticeSessionDto>> GetRecentSessionsAsync(ulong userId, int limit = 20, CancellationToken ct = default);
    Task<bool> SaveAnswerAsync(ulong sessionId, ulong userId, SavePracticeAnswerRequest request, CancellationToken ct = default);
    Task<SubmitPracticeSessionResponse> SubmitSessionAsync(ulong sessionId, ulong userId, CancellationToken ct = default);
}

public interface IExamService
{
    Task<IReadOnlyList<ExamTemplateDto>> GetExamTemplatesAsync(string? examType = null, CancellationToken ct = default);
    Task<ExamTemplateDto?> GetExamTemplateByIdAsync(ulong templateId, CancellationToken ct = default);
    Task<ActiveExamAttemptDto> StartExamAttemptAsync(ulong userId, ulong templateId, CancellationToken ct = default);
    Task<ActiveExamAttemptDto?> GetActiveExamAttemptAsync(ulong userId, CancellationToken ct = default);
    Task<ActiveExamAttemptDto?> GetExamAttemptByIdAsync(ulong attemptId, ulong userId, CancellationToken ct = default);
    Task<SaveExamAnswerResponseDto> SaveExamAnswerAsync(ulong attemptId, ulong userId, SaveExamAnswerRequest request, CancellationToken ct = default);
    Task<ActiveExamAttemptDto> TransitionSectionAsync(ulong attemptId, ulong userId, TransitionSectionRequest request, CancellationToken ct = default);
    Task<ExamResultDto> SubmitExamAttemptAsync(ulong attemptId, ulong userId, bool isAutoSubmit = false, CancellationToken ct = default);
    Task<ExamResultDto?> GetExamResultAsync(ulong attemptId, ulong userId, CancellationToken ct = default);
    Task<ExamReviewDto?> GetExamReviewAsync(ulong attemptId, ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<ExamAttempt>> GetRecentExamAttemptsAsync(ulong userId, int limit = 20, CancellationToken ct = default);
    Task<int> AutoSubmitExpiredAttemptsAsync(CancellationToken ct = default);
}
