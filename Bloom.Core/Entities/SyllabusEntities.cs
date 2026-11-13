using Bloom.Core.Interfaces;

namespace Bloom.Core.Entities;

public class SyllabusSubject : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong? UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public uint DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class SyllabusModule : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong SubjectId { get; set; }
    public ulong? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public uint DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class SyllabusTopic : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong ModuleId { get; set; }
    public ulong? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal EstimatedStudyHours { get; set; } = 4.00m;
    public string DifficultyLevel { get; set; } = "Medium";
    public string ImportanceLevel { get; set; } = "High";
    public uint DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class SyllabusSubtopic : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong TopicId { get; set; }
    public ulong? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public uint DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class UserTopicProgress : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong TopicId { get; set; }
    public string Status { get; set; } = "NotStarted";
    public string Priority { get; set; } = "Medium";
    public DateTime? TargetDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public byte ConfidenceRating { get; set; }
    public string? PersonalNotes { get; set; }
    public bool IsCustomExcluded { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class UserSubtopicProgress : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong SubtopicId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class SyllabusResource : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong TopicId { get; set; }
    public ulong? UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "Article";
    public string? Url { get; set; }
    public string? Notes { get; set; }
    public uint DisplayOrder { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
