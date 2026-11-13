namespace Bloom.Core.Entities;

public class QuestionPassage
{
    public ulong Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public string? Genre { get; set; }
    public string? SourceAttribution { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class QuestionSet
{
    public ulong Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContextScenario { get; set; } = string.Empty;
    public string? DataPayloadJson { get; set; }
    public string SetType { get; set; } = "DataInterpretation";
    public string? SourceAttribution { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class Question
{
    public ulong Id { get; set; }
    public ulong SubjectId { get; set; }
    public ulong ModuleId { get; set; }
    public ulong TopicId { get; set; }
    public ulong? SubtopicId { get; set; }
    public ulong? QuestionSetId { get; set; }
    public ulong? PassageId { get; set; }
    public string QuestionType { get; set; } = "MCQ"; // 'MCQ', 'TITA'
    public string DifficultyLevel { get; set; } = "Medium"; // 'Easy', 'Medium', 'Hard'
    public string QuestionContent { get; set; } = string.Empty;
    public string? QuestionImageUrl { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public decimal? TitaNumericRangeMin { get; set; }
    public decimal? TitaNumericRangeMax { get; set; }
    public string? Explanation { get; set; }
    public string? SolutionSteps { get; set; }
    public int EstimatedSeconds { get; set; } = 120;
    public decimal DefaultMarks { get; set; } = 3.00m;
    public decimal DefaultPenalty { get; set; } = 1.00m;
    public string? SourceAttribution { get; set; }
    public string? Tags { get; set; }
    public string Status { get; set; } = "Published"; // 'Draft', 'Published', 'Archived'
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    // Navigation / Join helpers
    public List<QuestionOption> Options { get; set; } = new();
    public QuestionPassage? Passage { get; set; }
    public QuestionSet? QuestionSet { get; set; }
    public string? SubjectName { get; set; }
    public string? TopicName { get; set; }
}

public class QuestionOption
{
    public ulong Id { get; set; }
    public ulong QuestionId { get; set; }
    public string OptionKey { get; set; } = string.Empty; // 'A', 'B', 'C', 'D'
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; }
}

public class UserQuestionBookmark
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong QuestionId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

