namespace Bloom.Core.Entities;

public class PracticeSession
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PracticeMode { get; set; } = "Topic"; // 'Topic', 'Subject', 'Difficulty', 'Custom', 'DILRSet', 'VARCPassage'
    public ulong? SubjectId { get; set; }
    public ulong? TopicId { get; set; }
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
    public string Status { get; set; } = "InProgress"; // 'InProgress', 'Completed', 'Abandoned'
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public ulong? ActivityRecordId { get; set; }

    public List<PracticeSessionQuestion> Questions { get; set; } = new();
    public string? SubjectName { get; set; }
    public string? TopicName { get; set; }
}

public class PracticeSessionQuestion
{
    public ulong Id { get; set; }
    public ulong PracticeSessionId { get; set; }
    public ulong QuestionId { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public string? UserAnswer { get; set; }
    public bool IsAnswered { get; set; }
    public bool? IsCorrect { get; set; }
    public bool IsMarkedForReview { get; set; }
    public int TimeSpentSeconds { get; set; }
    public DateTime? AnsweredAtUtc { get; set; }

    public Question? Question { get; set; }
}

public class QuestionAttempt
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong QuestionId { get; set; }
    public string SourceType { get; set; } = "Practice"; // 'Practice', 'MockExam'
    public ulong SourceId { get; set; }
    public string? SubmittedAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public int TimeSpentSeconds { get; set; }
    public decimal MarksObtained { get; set; }
    public ulong? TopicId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

