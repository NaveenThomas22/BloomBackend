namespace Bloom.Core.Entities;

public class ExamTemplate
{
    public ulong Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ExamType { get; set; } = "FullMock"; // 'FullMock', 'SectionalMock', 'Custom'
    public int TotalDurationMinutes { get; set; } = 120;
    public int TotalQuestions { get; set; } = 66;
    public decimal TotalMarks { get; set; } = 198.00m;
    public string? Instructions { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<ExamSection> Sections { get; set; } = new();
}

public class ExamSection
{
    public ulong Id { get; set; }
    public ulong ExamTemplateId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public ulong? SubjectId { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public int DurationMinutes { get; set; } = 40;
    public int QuestionCount { get; set; } = 22;
    public bool CanNavigateBack { get; set; }
    public decimal CorrectMarks { get; set; } = 3.00m;
    public decimal IncorrectPenalty { get; set; } = 1.00m;
    public DateTime CreatedAtUtc { get; set; }

    public List<ExamSectionQuestion> SectionQuestions { get; set; } = new();
}

public class ExamSectionQuestion
{
    public ulong Id { get; set; }
    public ulong ExamSectionId { get; set; }
    public ulong QuestionId { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public decimal? MarksOverride { get; set; }
    public decimal? PenaltyOverride { get; set; }
}

public class ExamAttempt
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong ExamTemplateId { get; set; }
    public string Status { get; set; } = "InProgress"; // 'Created', 'InProgress', 'Submitted', 'AutoSubmitted', 'Expired', 'Abandoned'
    public DateTime StartedAtUtc { get; set; }
    public DateTime DeadlineUtc { get; set; }
    public ulong? CurrentSectionId { get; set; }
    public DateTime SectionStartedAtUtc { get; set; }
    public DateTime SectionDeadlineUtc { get; set; }
    public int CurrentQuestionIndex { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public decimal TotalScore { get; set; }
    public decimal MaxPossibleScore { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalAttempted { get; set; }
    public int TotalCorrect { get; set; }
    public int TotalIncorrect { get; set; }
    public int TotalUnanswered { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public int TimeSpentSeconds { get; set; }
    public ulong? ActivityRecordId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ExamTemplate? Template { get; set; }
    public List<ExamAttemptQuestion> AttemptQuestions { get; set; } = new();
    public List<ExamAttemptSectionResult> SectionResults { get; set; } = new();
}

public class ExamAttemptQuestion
{
    public ulong Id { get; set; }
    public ulong ExamAttemptId { get; set; }
    public ulong ExamSectionId { get; set; }
    public ulong QuestionId { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public string SnapshotQuestionType { get; set; } = "MCQ";
    public string SnapshotContent { get; set; } = string.Empty;
    public string? SnapshotOptionsJson { get; set; }
    public string? SnapshotPassageJson { get; set; }
    public string? SnapshotSetJson { get; set; }
    public string SnapshotCorrectAnswer { get; set; } = string.Empty;
    public decimal SnapshotMarks { get; set; } = 3.00m;
    public decimal SnapshotPenalty { get; set; } = 1.00m;
    public string? SnapshotExplanation { get; set; }
    public string? SnapshotSolutionSteps { get; set; }
    public string? UserAnswer { get; set; }
    public string AnswerStatus { get; set; } = "NotVisited"; // 'NotVisited', 'NotAnswered', 'Answered', 'MarkedForReview', 'AnsweredAndMarkedForReview'
    public bool? IsCorrect { get; set; }
    public decimal MarksAwarded { get; set; }
    public int TimeSpentSeconds { get; set; }
    public DateTime? AnsweredAtUtc { get; set; }
    public uint Version { get; set; } = 1;
    public DateTime LastSavedAtUtc { get; set; }
}

public class ExamAttemptSectionResult
{
    public ulong Id { get; set; }
    public ulong ExamAttemptId { get; set; }
    public ulong ExamSectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 1;
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

