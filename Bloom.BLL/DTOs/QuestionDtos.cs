namespace Bloom.BLL.DTOs;

public class QuestionOptionDto
{
    public ulong Id { get; set; }
    public string OptionKey { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    // Only populated in review / administrative mode
    public bool? IsCorrect { get; set; }
}

public class QuestionPassageDto
{
    public ulong Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public string? Genre { get; set; }
    public string? SourceAttribution { get; set; }
    public bool IsActive { get; set; }
}

public class QuestionSetDto
{
    public ulong Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContextScenario { get; set; } = string.Empty;
    public string? DataPayloadJson { get; set; }
    public string SetType { get; set; } = "DataInterpretation";
    public string? SourceAttribution { get; set; }
    public bool IsActive { get; set; }
}

public class QuestionDto
{
    public ulong Id { get; set; }
    public ulong SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public ulong ModuleId { get; set; }
    public ulong TopicId { get; set; }
    public string? TopicName { get; set; }
    public ulong? SubtopicId { get; set; }
    public ulong? QuestionSetId { get; set; }
    public ulong? PassageId { get; set; }
    public string QuestionType { get; set; } = "MCQ"; // 'MCQ', 'TITA'
    public string DifficultyLevel { get; set; } = "Medium";
    public string QuestionContent { get; set; } = string.Empty;
    public string? QuestionImageUrl { get; set; }
    public int EstimatedSeconds { get; set; }
    public decimal DefaultMarks { get; set; }
    public decimal DefaultPenalty { get; set; }
    public string? SourceAttribution { get; set; }
    public string? Tags { get; set; }
    public string Status { get; set; } = "Published";
    public bool IsBookmarked { get; set; }
    public List<QuestionOptionDto> Options { get; set; } = new();
    public QuestionPassageDto? Passage { get; set; }
    public QuestionSetDto? QuestionSet { get; set; }

    // Protected answer data (only populated when explicitly requested / permitted)
    public string? CorrectAnswer { get; set; }
    public string? Explanation { get; set; }
    public string? SolutionSteps { get; set; }
}

public class CreateQuestionOptionRequest
{
    public string OptionKey { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int DisplayOrder { get; set; } = 1;
}

public class CreateQuestionRequest
{
    public ulong SubjectId { get; set; }
    public ulong ModuleId { get; set; }
    public ulong TopicId { get; set; }
    public ulong? SubtopicId { get; set; }
    public ulong? QuestionSetId { get; set; }
    public ulong? PassageId { get; set; }
    public string QuestionType { get; set; } = "MCQ";
    public string DifficultyLevel { get; set; } = "Medium";
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
    public string Status { get; set; } = "Published";
    public List<CreateQuestionOptionRequest> Options { get; set; } = new();
}

public class UpdateQuestionRequest : CreateQuestionRequest
{
}

public class CreatePassageRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Genre { get; set; }
    public string? SourceAttribution { get; set; }
}

public class CreateQuestionSetRequest
{
    public string Title { get; set; } = string.Empty;
    public string ContextScenario { get; set; } = string.Empty;
    public string? DataPayloadJson { get; set; }
    public string SetType { get; set; } = "DataInterpretation";
    public string? SourceAttribution { get; set; }
}

public class BookmarkDto
{
    public ulong Id { get; set; }
    public ulong QuestionId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public QuestionDto? Question { get; set; }
}

public class AddBookmarkRequest
{
    public string? Note { get; set; }
}

