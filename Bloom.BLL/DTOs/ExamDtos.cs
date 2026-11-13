namespace Bloom.BLL.DTOs;

public class ExamSectionDto
{
    public ulong Id { get; set; }
    public ulong ExamTemplateId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public ulong? SubjectId { get; set; }
    public int DisplayOrder { get; set; }
    public int DurationMinutes { get; set; }
    public int QuestionCount { get; set; }
    public bool CanNavigateBack { get; set; }
    public decimal CorrectMarks { get; set; }
    public decimal IncorrectPenalty { get; set; }
}

public class ExamTemplateDto
{
    public ulong Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ExamType { get; set; } = "FullMock";
    public int TotalDurationMinutes { get; set; }
    public int TotalQuestions { get; set; }
    public decimal TotalMarks { get; set; }
    public string? Instructions { get; set; }
    public bool IsPublished { get; set; }
    public List<ExamSectionDto> Sections { get; set; } = new();
}

public class ExamAttemptQuestionItemDto
{
    public ulong Id { get; set; }
    public ulong ExamSectionId { get; set; }
    public ulong QuestionId { get; set; }
    public int DisplayOrder { get; set; }
    public string QuestionType { get; set; } = "MCQ";
    public string Content { get; set; } = string.Empty;
    public List<QuestionOptionDto> Options { get; set; } = new();
    public QuestionPassageDto? Passage { get; set; }
    public QuestionSetDto? QuestionSet { get; set; }
    public decimal Marks { get; set; }
    public decimal Penalty { get; set; }
    public string? UserAnswer { get; set; }
    public string AnswerStatus { get; set; } = "NotVisited"; // 'NotVisited', 'NotAnswered', 'Answered', 'MarkedForReview', 'AnsweredAndMarkedForReview'
    public int TimeSpentSeconds { get; set; }
    public uint Version { get; set; } = 1;
}

public class ActiveExamAttemptDto
{
    public ulong Id { get; set; }
    public ulong ExamTemplateId { get; set; }
    public string TemplateTitle { get; set; } = string.Empty;
    public string Status { get; set; } = "InProgress";
    public DateTime ServerTimeUtc { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime DeadlineUtc { get; set; }
    public long RemainingSecondsTotal { get; set; }
    public ulong CurrentSectionId { get; set; }
    public string CurrentSectionName { get; set; } = string.Empty;
    public int CurrentSectionIndex { get; set; }
    public int TotalSections { get; set; }
    public DateTime SectionStartedAtUtc { get; set; }
    public DateTime SectionDeadlineUtc { get; set; }
    public long RemainingSecondsCurrentSection { get; set; }
    public int CurrentQuestionIndex { get; set; }
    public List<ExamSectionDto> Sections { get; set; } = new();
    public List<ExamAttemptQuestionItemDto> Questions { get; set; } = new();
}

public class SaveExamAnswerRequest
{
    public ulong QuestionId { get; set; }
    public string? UserAnswer { get; set; }
    public string AnswerStatus { get; set; } = "Answered"; // 'NotAnswered', 'Answered', 'MarkedForReview', 'AnsweredAndMarkedForReview'
    public int TimeSpentSeconds { get; set; }
    public uint? ExpectedVersion { get; set; }
}

public class SaveExamAnswerResponseDto
{
    public ulong QuestionId { get; set; }
    public uint NewVersion { get; set; }
    public DateTime LastSavedAtUtc { get; set; }
}

public class TransitionSectionRequest
{
    public ulong NextSectionId { get; set; }
}

public class ExamSectionResultDto
{
    public ulong ExamSectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public int TotalQuestions { get; set; }
    public int Attempted { get; set; }
    public int Correct { get; set; }
    public int Incorrect { get; set; }
    public int Unanswered { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public int TimeSpentSeconds { get; set; }
}

public class ExamResultDto
{
    public ulong AttemptId { get; set; }
    public ulong ExamTemplateId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string Status { get; set; } = "Submitted";
    public decimal TotalScore { get; set; }
    public decimal MaxPossibleScore { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalAttempted { get; set; }
    public int TotalCorrect { get; set; }
    public int TotalIncorrect { get; set; }
    public int TotalUnanswered { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public int TimeSpentSeconds { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public List<ExamSectionResultDto> SectionResults { get; set; } = new();
}

public class ExamReviewQuestionDto
{
    public ulong QuestionId { get; set; }
    public ulong ExamSectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string QuestionType { get; set; } = "MCQ";
    public string Content { get; set; } = string.Empty;
    public List<QuestionOptionDto> Options { get; set; } = new();
    public QuestionPassageDto? Passage { get; set; }
    public QuestionSetDto? QuestionSet { get; set; }
    public string? UserAnswer { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public bool? IsCorrect { get; set; }
    public decimal MarksAwarded { get; set; }
    public decimal MaxMarks { get; set; }
    public decimal Penalty { get; set; }
    public string? Explanation { get; set; }
    public string? SolutionSteps { get; set; }
    public int TimeSpentSeconds { get; set; }
}

public class ExamReviewDto
{
    public ExamResultDto Summary { get; set; } = new();
    public List<ExamReviewQuestionDto> Questions { get; set; } = new();
}

