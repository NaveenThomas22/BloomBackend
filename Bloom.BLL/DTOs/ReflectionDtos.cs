namespace Bloom.BLL.DTOs;

public class ReflectionMoodDto
{
    public ulong Id { get; set; }
    public string MoodCode { get; set; } = string.Empty;
    public string MoodName { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string Color { get; set; } = "#A8BDA5";
    public string? Description { get; set; }
    public ushort DisplayOrder { get; set; }
}

public class MorningReflectionDto
{
    public ulong? MoodId { get; set; }
    public string? MoodName { get; set; }
    public string? MoodEmoji { get; set; }
    public string? MoodColor { get; set; }
    public byte? MoodIntensity { get; set; }
    public byte? EnergyLevel { get; set; }
    public string? TodayIntention { get; set; }
    public string? MainPriority { get; set; }
    public string? TaskOne { get; set; }
    public string? TaskTwo { get; set; }
    public string? TaskThree { get; set; }
    public uint? StudyTargetMinutes { get; set; }
    public string? PersonalCommitment { get; set; }
    public string? MorningNotes { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool IsCompleted => CompletedAtUtc.HasValue;
}

public class EveningReflectionDto
{
    public ulong? MoodId { get; set; }
    public string? MoodName { get; set; }
    public string? MoodEmoji { get; set; }
    public string? MoodColor { get; set; }
    public byte? MoodIntensity { get; set; }
    public string? HowWasToday { get; set; }
    public string? WhatAccomplished { get; set; }
    public string? WhatWentWell { get; set; }
    public string? CouldHaveBeenBetter { get; set; }
    public string? WhatLearnedToday { get; set; }
    public string? WhatChallengedMe { get; set; }
    public byte? SatisfactionRating { get; set; }
    public string? ImproveTomorrow { get; set; }
    public string? GratitudeEntry { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool IsCompleted => CompletedAtUtc.HasValue;
}

public class DailyReflectionDto
{
    public ulong Id { get; set; }
    public DateOnly ReflectionDate { get; set; }
    public MorningReflectionDto Morning { get; set; } = new();
    public EveningReflectionDto Evening { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class SaveMorningReflectionRequest
{
    public DateOnly ReflectionDate { get; set; }
    public ulong? MoodId { get; set; }
    public byte? MoodIntensity { get; set; }
    public byte? EnergyLevel { get; set; }
    public string? TodayIntention { get; set; }
    public string? MainPriority { get; set; }
    public string? TaskOne { get; set; }
    public string? TaskTwo { get; set; }
    public string? TaskThree { get; set; }
    public uint? StudyTargetMinutes { get; set; }
    public string? PersonalCommitment { get; set; }
    public string? MorningNotes { get; set; }
}

public class SaveEveningReflectionRequest
{
    public DateOnly ReflectionDate { get; set; }
    public ulong? MoodId { get; set; }
    public byte? MoodIntensity { get; set; }
    public string? HowWasToday { get; set; }
    public string? WhatAccomplished { get; set; }
    public string? WhatWentWell { get; set; }
    public string? CouldHaveBeenBetter { get; set; }
    public string? WhatLearnedToday { get; set; }
    public string? WhatChallengedMe { get; set; }
    public byte? SatisfactionRating { get; set; }
    public string? ImproveTomorrow { get; set; }
    public string? GratitudeEntry { get; set; }
}

public class MoodDistributionItemDto
{
    public string MoodName { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class WeeklyReflectionSummaryDto
{
    public DateOnly StartOfWeek { get; set; }
    public DateOnly EndOfWeek { get; set; }
    public int TotalDaysWithReflections { get; set; }
    public int MorningCompletionCount { get; set; }
    public int EveningCompletionCount { get; set; }
    public double? AverageSatisfactionRating { get; set; }
    public IReadOnlyList<MoodDistributionItemDto> MoodDistribution { get; set; } = Array.Empty<MoodDistributionItemDto>();
    public IReadOnlyList<DailyReflectionDto> Reflections { get; set; } = Array.Empty<DailyReflectionDto>();
}

