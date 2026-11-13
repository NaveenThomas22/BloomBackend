namespace Bloom.Core.Enums;

/// <summary>
/// Status of an aspirant's learning journey on an individual syllabus topic.
/// </summary>
public enum LearningStatus
{
    NotStarted = 1,
    Learning = 2,
    Practicing = 3,
    Completed = 4,
    RevisionDue = 5,
    Mastered = 6
}

/// <summary>
/// Aspirant priority level assigned to a syllabus topic.
/// </summary>
public enum TopicPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

/// <summary>
/// Relative difficulty classification of a syllabus topic.
/// </summary>
public enum DifficultyLevel
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}

/// <summary>
/// Media or reference format of an attached study resource.
/// </summary>
public enum ResourceType
{
    Article = 1,
    Video = 2,
    Book = 3,
    FormulaSheet = 4,
    Notes = 5,
    Link = 6
}

