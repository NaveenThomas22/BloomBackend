using Bloom.Core.Interfaces;

namespace Bloom.Core.Entities;

public class JournalCategory : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong? UserId { get; set; } // Null for system defaults
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = "book";
    public string Color { get; set; } = "#A8BDA5";
    public bool IsActive { get; set; } = true;
    public ushort DisplayOrder { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class JournalEntry : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public bool IsDraft { get; set; }
    public uint WordCount { get; set; }
    public uint Version { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }

    // Joined category properties
    public string? CategoryName { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryIcon { get; set; }
    public string? CategoryColor { get; set; }
}

public class ReflectionMood
{
    public ulong Id { get; set; }
    public string MoodCode { get; set; } = string.Empty;
    public string MoodName { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string Color { get; set; } = "#A8BDA5";
    public string? Description { get; set; }
    public ushort DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DailyReflection : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public DateOnly ReflectionDate { get; set; }

    // Morning Reflection Fields
    public ulong? MorningMoodId { get; set; }
    public byte? MorningMoodIntensity { get; set; }
    public byte? MorningEnergyLevel { get; set; }
    public string? TodayIntention { get; set; }
    public string? MainPriority { get; set; }
    public string? TaskOne { get; set; }
    public string? TaskTwo { get; set; }
    public string? TaskThree { get; set; }
    public uint? StudyTargetMinutes { get; set; }
    public string? PersonalCommitment { get; set; }
    public string? MorningNotes { get; set; }
    public DateTime? MorningCompletedAtUtc { get; set; }

    // Evening Reflection Fields
    public ulong? EveningMoodId { get; set; }
    public byte? EveningMoodIntensity { get; set; }
    public string? HowWasToday { get; set; }
    public string? WhatAccomplished { get; set; }
    public string? WhatWentWell { get; set; }
    public string? CouldHaveBeenBetter { get; set; }
    public string? WhatLearnedToday { get; set; }
    public string? WhatChallengedMe { get; set; }
    public byte? SatisfactionRating { get; set; }
    public string? ImproveTomorrow { get; set; }
    public string? GratitudeEntry { get; set; }
    public DateTime? EveningCompletedAtUtc { get; set; }

    // Timestamps
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Joined Mood details
    public string? MorningMoodName { get; set; }
    public string? MorningMoodEmoji { get; set; }
    public string? MorningMoodColor { get; set; }
    public string? EveningMoodName { get; set; }
    public string? EveningMoodEmoji { get; set; }
    public string? EveningMoodColor { get; set; }
}

