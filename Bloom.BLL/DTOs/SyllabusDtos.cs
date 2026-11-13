namespace Bloom.BLL.DTOs;

#region Output Response DTOs

public class SyllabusOverviewDto
{
    public int TotalTopics { get; set; }
    public int CompletedTopics { get; set; }
    public int MasteredTopics { get; set; }
    public int InProgressTopics { get; set; }
    public int RevisionDueTopics { get; set; }
    public int NotStartedTopics { get; set; }
    public double OverallProgressPercentage { get; set; }
    public decimal EstimatedTotalHours { get; set; }
    public decimal EstimatedRemainingHours { get; set; }
    public IReadOnlyList<SubjectSummaryDto> Subjects { get; set; } = Array.Empty<SubjectSummaryDto>();
}

public class SubjectSummaryDto
{
    public ulong Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCustom { get; set; }
    public int ModuleCount { get; set; }
    public int TopicCount { get; set; }
    public int CompletedTopicCount { get; set; }
    public double ProgressPercentage { get; set; }
    public uint DisplayOrder { get; set; }
}

public class SubjectDetailDto
{
    public SubjectSummaryDto Subject { get; set; } = null!;
    public IReadOnlyList<ModuleDetailDto> Modules { get; set; } = Array.Empty<ModuleDetailDto>();
}

public class ModuleDetailDto
{
    public ulong Id { get; set; }
    public ulong SubjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCustom { get; set; }
    public int TopicCount { get; set; }
    public int CompletedTopicCount { get; set; }
    public double ProgressPercentage { get; set; }
    public uint DisplayOrder { get; set; }
    public IReadOnlyList<TopicSummaryDto> Topics { get; set; } = Array.Empty<TopicSummaryDto>();
}

public class TopicSummaryDto
{
    public ulong Id { get; set; }
    public ulong ModuleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal EstimatedStudyHours { get; set; }
    public string DifficultyLevel { get; set; } = "Medium";
    public string ImportanceLevel { get; set; } = "High";
    public uint DisplayOrder { get; set; }
    public bool IsCustom { get; set; }
    
    // User progress fields
    public string Status { get; set; } = "NotStarted";
    public string Priority { get; set; } = "Medium";
    public DateTime? TargetDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public byte ConfidenceRating { get; set; }
    public string? PersonalNotes { get; set; }
    public bool IsCustomExcluded { get; set; }
    public int SubtopicCount { get; set; }
    public int CompletedSubtopicCount { get; set; }
}

public class TopicDetailDto
{
    public TopicSummaryDto Topic { get; set; } = null!;
    public string ModuleName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public IReadOnlyList<SubtopicDetailDto> Subtopics { get; set; } = Array.Empty<SubtopicDetailDto>();
    public IReadOnlyList<SyllabusResourceDto> Resources { get; set; } = Array.Empty<SyllabusResourceDto>();
}

public class SubtopicDetailDto
{
    public ulong Id { get; set; }
    public ulong TopicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public uint DisplayOrder { get; set; }
    public bool IsCustom { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public class SyllabusResourceDto
{
    public ulong Id { get; set; }
    public ulong TopicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "Article";
    public string? Url { get; set; }
    public string? Notes { get; set; }
    public bool IsCustom { get; set; }
    public uint DisplayOrder { get; set; }
}

#endregion

#region Request Input DTOs

public class CreateSubjectDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public uint DisplayOrder { get; set; } = 1;
}

public class UpdateSubjectDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public uint DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class CreateModuleDto
{
    public ulong SubjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public uint DisplayOrder { get; set; } = 1;
}

public class UpdateModuleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public uint DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class CreateTopicDto
{
    public ulong ModuleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal EstimatedStudyHours { get; set; } = 4.00m;
    public string DifficultyLevel { get; set; } = "Medium";
    public string ImportanceLevel { get; set; } = "High";
    public uint DisplayOrder { get; set; } = 1;
}

public class UpdateTopicDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal EstimatedStudyHours { get; set; } = 4.00m;
    public string DifficultyLevel { get; set; } = "Medium";
    public string ImportanceLevel { get; set; } = "High";
    public uint DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class CreateSubtopicDto
{
    public ulong TopicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public uint DisplayOrder { get; set; } = 1;
}

public class UpdateSubtopicDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public uint DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class UpdateTopicProgressDto
{
    public string Status { get; set; } = "NotStarted";
    public string Priority { get; set; } = "Medium";
    public DateTime? TargetDate { get; set; }
    public byte ConfidenceRating { get; set; }
    public string? PersonalNotes { get; set; }
    public bool IsCustomExcluded { get; set; }
}

public class UpdateSubtopicProgressDto
{
    public bool IsCompleted { get; set; }
    public string? Notes { get; set; }
}

public class CreateResourceDto
{
    public string Title { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "Article";
    public string? Url { get; set; }
    public string? Notes { get; set; }
    public uint DisplayOrder { get; set; } = 1;
}

#endregion

