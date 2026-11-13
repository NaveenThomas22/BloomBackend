namespace Bloom.BLL.DTOs;

public class StartPracticeSessionRequest
{
    public string Title { get; set; } = "Quick Practice Session";
    public string PracticeMode { get; set; } = "Topic"; // 'Topic', 'Subject', 'Difficulty', 'Custom', 'DILRSet', 'VARCPassage'
    public ulong? SubjectId { get; set; }
    public ulong? TopicId { get; set; }
    public ulong? QuestionSetId { get; set; }
    public ulong? PassageId { get; set; }
    public string? DifficultyLevel { get; set; }
    public string? QuestionType { get; set; }
    public int QuestionCount { get; set; } = 10;
    public int? TimeLimitMinutes { get; set; }
}

public class PracticeQuestionItemDto
{
    public ulong Id { get; set; }
    public ulong QuestionId { get; set; }
    public int DisplayOrder { get; set; }
    public string? UserAnswer { get; set; }
    public bool IsAnswered { get; set; }
    public bool? IsCorrect { get; set; }
    public bool IsMarkedForReview { get; set; }
    public int TimeSpentSeconds { get; set; }
    public QuestionDto Question { get; set; } = new();
}

public class PracticeSessionDto
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PracticeMode { get; set; } = "Topic";
    public ulong? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public ulong? TopicId { get; set; }
    public string? TopicName { get; set; }
    public string? DifficultyLevel { get; set; }
    public int TotalQuestions { get; set; }
    public int AttemptedQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int IncorrectAnswers { get; set; }
    public int UnansweredQuestions { get; set; }
    public decimal Score { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public int TimeSpentSeconds { get; set; }
    public int? TimeLimitSeconds { get; set; }
    public string Status { get; set; } = "InProgress";
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public List<PracticeQuestionItemDto> Questions { get; set; } = new();
}

public class SavePracticeAnswerRequest
{
    public ulong QuestionId { get; set; }
    public string? UserAnswer { get; set; }
    public bool IsMarkedForReview { get; set; }
    public int TimeSpentSeconds { get; set; }
}

public class SubmitPracticeSessionResponse
{
    public ulong SessionId { get; set; }
    public string Status { get; set; } = "Completed";
    public int TotalQuestions { get; set; }
    public int AttemptedQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int IncorrectAnswers { get; set; }
    public int UnansweredQuestions { get; set; }
    public decimal Score { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public int TotalTimeSpentSeconds { get; set; }
    public List<PracticeQuestionItemDto> Questions { get; set; } = new();
}

public class QuestionAttemptHistoryDto
{
    public ulong Id { get; set; }
    public ulong QuestionId { get; set; }
    public string QuestionContent { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "MCQ";
    public string DifficultyLevel { get; set; } = "Medium";
    public string? TopicName { get; set; }
    public string SourceType { get; set; } = "Practice";
    public string? SubmittedAnswer { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int TimeSpentSeconds { get; set; }
    public decimal MarksObtained { get; set; }
    public DateTime AttemptedAtUtc { get; set; }
}

