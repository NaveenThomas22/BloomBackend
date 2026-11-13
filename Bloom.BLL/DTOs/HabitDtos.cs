namespace Bloom.BLL.DTOs;

public class HabitCategoryDto
{
    public ulong Id { get; set; }
    public ulong? UserId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = "check-circle";
    public string Color { get; set; } = "#A8BDA5";
    public ushort DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class UserHabitDto
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Frequency { get; set; } = "Daily";
    public uint TargetFrequencyDays { get; set; } = 7;
    public decimal TargetValue { get; set; } = 1.00m;
    public string TargetUnit { get; set; } = "times";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public ushort DisplayOrder { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    // Category details
    public string? CategoryName { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryIcon { get; set; }
    public string? CategoryColor { get; set; }

    // Today's completion status if loaded in context
    public HabitCompletionDto? TodayCompletion { get; set; }
    public int CurrentStreak { get; set; }
}

public class CreateHabitRequest
{
    public ulong CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Frequency { get; set; } = "Daily";
    public uint TargetFrequencyDays { get; set; } = 7;
    public decimal TargetValue { get; set; } = 1.00m;
    public string TargetUnit { get; set; } = "times";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ushort DisplayOrder { get; set; } = 1;
}

public class UpdateHabitRequest
{
    public ulong CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Frequency { get; set; } = "Daily";
    public uint TargetFrequencyDays { get; set; } = 7;
    public decimal TargetValue { get; set; } = 1.00m;
    public string TargetUnit { get; set; } = "times";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public ushort DisplayOrder { get; set; } = 1;
}

public class HabitCompletionDto
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong HabitId { get; set; }
    public DateOnly CompletionDate { get; set; }
    public string Status { get; set; } = "Completed"; // Completed, Partial, Skipped
    public decimal ActualValue { get; set; } = 1.00m;
    public string? SkipReason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class RecordHabitCompletionRequest
{
    public DateOnly CompletionDate { get; set; }
    public string Status { get; set; } = "Completed"; // Completed, Partial, Skipped
    public decimal ActualValue { get; set; } = 1.00m;
    public string? SkipReason { get; set; }
    public string? Notes { get; set; }
}

public class HabitStreakDto
{
    public ulong HabitId { get; set; }
    public string HabitName { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalCompletions { get; set; }
    public double CompletionRate30Days { get; set; }
    public DateOnly? LastCompletedDate { get; set; }
}

public class HabitOverviewDto
{
    public DateOnly ReferenceDate { get; set; }
    public IReadOnlyList<UserHabitDto> Habits { get; set; } = Array.Empty<UserHabitDto>();
    public IReadOnlyList<HabitStreakDto> Streaks { get; set; } = Array.Empty<HabitStreakDto>();
    public int TotalHabits { get; set; }
    public int CompletedToday { get; set; }
    public double TodayCompletionRate { get; set; }
    public int CurrentBestStreak { get; set; }
}

