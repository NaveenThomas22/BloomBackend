namespace Bloom.BLL.DTOs;

public class PersonalMilestoneDto
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "GENERAL"; // STUDY_HOURS, QUESTIONS_SOLVED, TOPICS_MASTERED, MOCK_TESTS, STREAK, GENERAL
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public double ProgressPercentage { get; set; }
    public string Unit { get; set; } = "count";
    public DateOnly? TargetDate { get; set; }
    public string Status { get; set; } = "InProgress"; // InProgress, Achieved, Abandoned
    public DateTime? AchievedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateMilestoneRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "GENERAL";
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; } = 0.00m;
    public string Unit { get; set; } = "count";
    public DateOnly? TargetDate { get; set; }
}

public class UpdateMilestoneRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "GENERAL";
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public string Unit { get; set; } = "count";
    public DateOnly? TargetDate { get; set; }
    public string Status { get; set; } = "InProgress";
}

public class MilestoneOverviewDto
{
    public IReadOnlyList<PersonalMilestoneDto> Milestones { get; set; } = Array.Empty<PersonalMilestoneDto>();
    public int TotalMilestones { get; set; }
    public int AchievedCount { get; set; }
    public int InProgressCount { get; set; }
    public double OverallProgressPercentage { get; set; }
}

