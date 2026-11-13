using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Bloom.Core.Exceptions.ValidationException;

namespace Bloom.BLL.Services;

public class ReflectionService : IReflectionService
{
    private readonly IReflectionRepository _reflectionRepository;
    private readonly IUserProfileRepository _userProfileRepo;
    private readonly IValidator<SaveMorningReflectionRequest> _morningValidator;
    private readonly IValidator<SaveEveningReflectionRequest> _eveningValidator;
    private readonly ILogger<ReflectionService> _logger;

    public ReflectionService(
        IReflectionRepository reflectionRepository,
        IUserProfileRepository userProfileRepo,
        IValidator<SaveMorningReflectionRequest> morningValidator,
        IValidator<SaveEveningReflectionRequest> eveningValidator,
        ILogger<ReflectionService> logger)
    {
        _reflectionRepository = reflectionRepository ?? throw new ArgumentNullException(nameof(reflectionRepository));
        _userProfileRepo = userProfileRepo ?? throw new ArgumentNullException(nameof(userProfileRepo));
        _morningValidator = morningValidator ?? throw new ArgumentNullException(nameof(morningValidator));
        _eveningValidator = eveningValidator ?? throw new ArgumentNullException(nameof(eveningValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<ReflectionMoodDto>> GetMoodsAsync(CancellationToken ct = default)
    {
        var moods = await _reflectionRepository.GetActiveMoodsAsync(ct);
        return moods.Select(m => new ReflectionMoodDto
        {
            Id = m.Id,
            MoodCode = m.MoodCode,
            MoodName = m.MoodName,
            Emoji = m.Emoji,
            Color = m.Color,
            Description = m.Description,
            DisplayOrder = m.DisplayOrder
        }).ToList();
    }

    public async Task<DailyReflectionDto> GetReflectionByDateAsync(ulong userId, DateOnly? reflectionDate, CancellationToken ct = default)
    {
        var targetDate = reflectionDate.HasValue && reflectionDate.Value != default
            ? reflectionDate.Value
            : await GetUserLocalDateAsync(userId, ct);

        var reflection = await _reflectionRepository.GetByDateAsync(userId, targetDate, ct);
        if (reflection == null)
        {
            return new DailyReflectionDto
            {
                Id = 0,
                ReflectionDate = targetDate,
                Morning = new MorningReflectionDto(),
                Evening = new EveningReflectionDto(),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        return MapToDto(reflection);
    }

    public async Task<DailyReflectionDto> SaveMorningReflectionAsync(ulong userId, SaveMorningReflectionRequest request, CancellationToken ct = default)
    {
        if (request.ReflectionDate == default)
        {
            request.ReflectionDate = await GetUserLocalDateAsync(userId, ct);
        }

        var validation = await _morningValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.ToDictionary());
        }

        if (request.MoodId.HasValue)
        {
            var mood = await _reflectionRepository.GetMoodByIdAsync(request.MoodId.Value, ct);
            if (mood == null || !mood.IsActive)
            {
                throw new ValidationException("MoodId", "Selected mood is invalid or inactive.");
            }
        }

        var entity = new DailyReflection
        {
            UserId = userId,
            ReflectionDate = request.ReflectionDate,
            MorningMoodId = request.MoodId,
            MorningMoodIntensity = request.MoodIntensity,
            MorningEnergyLevel = request.EnergyLevel,
            TodayIntention = request.TodayIntention?.Trim(),
            MainPriority = request.MainPriority?.Trim(),
            TaskOne = request.TaskOne?.Trim(),
            TaskTwo = request.TaskTwo?.Trim(),
            TaskThree = request.TaskThree?.Trim(),
            StudyTargetMinutes = request.StudyTargetMinutes,
            PersonalCommitment = request.PersonalCommitment?.Trim(),
            MorningNotes = request.MorningNotes?.Trim(),
            MorningCompletedAtUtc = DateTime.UtcNow
        };

        var id = await _reflectionRepository.UpsertMorningReflectionAsync(entity, ct);
        _logger.LogInformation("Morning reflection saved for user {UserId} on date {Date} (Id: {Id})", userId, request.ReflectionDate, id);

        var refreshed = await _reflectionRepository.GetByDateAsync(userId, request.ReflectionDate, ct);
        return MapToDto(refreshed ?? entity);
    }

    public async Task<DailyReflectionDto> SaveEveningReflectionAsync(ulong userId, SaveEveningReflectionRequest request, CancellationToken ct = default)
    {
        if (request.ReflectionDate == default)
        {
            request.ReflectionDate = await GetUserLocalDateAsync(userId, ct);
        }

        var validation = await _eveningValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.ToDictionary());
        }

        if (request.MoodId.HasValue)
        {
            var mood = await _reflectionRepository.GetMoodByIdAsync(request.MoodId.Value, ct);
            if (mood == null || !mood.IsActive)
            {
                throw new ValidationException("MoodId", "Selected mood is invalid or inactive.");
            }
        }

        var entity = new DailyReflection
        {
            UserId = userId,
            ReflectionDate = request.ReflectionDate,
            EveningMoodId = request.MoodId,
            EveningMoodIntensity = request.MoodIntensity,
            HowWasToday = request.HowWasToday?.Trim(),
            WhatAccomplished = request.WhatAccomplished?.Trim(),
            WhatWentWell = request.WhatWentWell?.Trim(),
            CouldHaveBeenBetter = request.CouldHaveBeenBetter?.Trim(),
            WhatLearnedToday = request.WhatLearnedToday?.Trim(),
            WhatChallengedMe = request.WhatChallengedMe?.Trim(),
            SatisfactionRating = request.SatisfactionRating,
            ImproveTomorrow = request.ImproveTomorrow?.Trim(),
            GratitudeEntry = request.GratitudeEntry?.Trim(),
            EveningCompletedAtUtc = DateTime.UtcNow
        };

        var id = await _reflectionRepository.UpsertEveningReflectionAsync(entity, ct);
        _logger.LogInformation("Evening reflection saved for user {UserId} on date {Date} (Id: {Id})", userId, request.ReflectionDate, id);

        var refreshed = await _reflectionRepository.GetByDateAsync(userId, request.ReflectionDate, ct);
        return MapToDto(refreshed ?? entity);
    }

    public async Task<WeeklyReflectionSummaryDto> GetWeeklySummaryAsync(ulong userId, DateOnly? startOfWeek, DateOnly? endOfWeek, CancellationToken ct = default)
    {
        var today = await GetUserLocalDateAsync(userId, ct);
        var end = endOfWeek.HasValue && endOfWeek.Value != default ? endOfWeek.Value : today;
        var start = startOfWeek.HasValue && startOfWeek.Value != default ? startOfWeek.Value : end.AddDays(-6);

        if (start > end)
        {
            throw new ValidationException("DateRange", "Start date must precede or equal end date.");
        }

        if (end.DayNumber - start.DayNumber > 31)
        {
            throw new ValidationException("DateRange", "Reflection summary range cannot exceed 31 days.");
        }

        var reflections = await _reflectionRepository.GetRangeAsync(userId, start, end, ct);
        var mappedList = reflections.Select(MapToDto).ToList();

        var morningCount = reflections.Count(r => r.MorningCompletedAtUtc.HasValue);
        var eveningCount = reflections.Count(r => r.EveningCompletedAtUtc.HasValue);
        var totalDays = reflections.Count(r => r.MorningCompletedAtUtc.HasValue || r.EveningCompletedAtUtc.HasValue);

        var satisfactionScores = reflections
            .Where(r => r.SatisfactionRating.HasValue)
            .Select(r => (double)r.SatisfactionRating!.Value)
            .ToList();

        double? averageSatisfaction = satisfactionScores.Count > 0
            ? Math.Round(satisfactionScores.Average(), 1)
            : null;

        // Collect mood frequency across morning and evening
        var moodCounts = new Dictionary<string, (string Emoji, string Color, int Count)>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in reflections)
        {
            if (!string.IsNullOrWhiteSpace(r.MorningMoodName))
            {
                var name = r.MorningMoodName;
                if (!moodCounts.ContainsKey(name))
                {
                    moodCounts[name] = (r.MorningMoodEmoji ?? "😊", r.MorningMoodColor ?? "#A8BDA5", 0);
                }
                var current = moodCounts[name];
                moodCounts[name] = (current.Emoji, current.Color, current.Count + 1);
            }

            if (!string.IsNullOrWhiteSpace(r.EveningMoodName))
            {
                var name = r.EveningMoodName;
                if (!moodCounts.ContainsKey(name))
                {
                    moodCounts[name] = (r.EveningMoodEmoji ?? "😊", r.EveningMoodColor ?? "#A8BDA5", 0);
                }
                var current = moodCounts[name];
                moodCounts[name] = (current.Emoji, current.Color, current.Count + 1);
            }
        }

        var totalMoodEntries = moodCounts.Values.Sum(v => v.Count);
        var moodDistribution = moodCounts
            .OrderByDescending(kv => kv.Value.Count)
            .Select(kv => new MoodDistributionItemDto
            {
                MoodName = kv.Key,
                Emoji = kv.Value.Emoji,
                Color = kv.Value.Color,
                Count = kv.Value.Count,
                Percentage = totalMoodEntries > 0 ? Math.Round((double)kv.Value.Count / totalMoodEntries * 100.0, 1) : 0
            })
            .ToList();

        return new WeeklyReflectionSummaryDto
        {
            StartOfWeek = start,
            EndOfWeek = end,
            TotalDaysWithReflections = totalDays,
            MorningCompletionCount = morningCount,
            EveningCompletionCount = eveningCount,
            AverageSatisfactionRating = averageSatisfaction,
            MoodDistribution = moodDistribution,
            Reflections = mappedList
        };
    }

    private static DailyReflectionDto MapToDto(DailyReflection entity)
    {
        return new DailyReflectionDto
        {
            Id = entity.Id,
            ReflectionDate = entity.ReflectionDate,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            Morning = new MorningReflectionDto
            {
                MoodId = entity.MorningMoodId,
                MoodName = entity.MorningMoodName,
                MoodEmoji = entity.MorningMoodEmoji,
                MoodColor = entity.MorningMoodColor,
                MoodIntensity = entity.MorningMoodIntensity,
                EnergyLevel = entity.MorningEnergyLevel,
                TodayIntention = entity.TodayIntention,
                MainPriority = entity.MainPriority,
                TaskOne = entity.TaskOne,
                TaskTwo = entity.TaskTwo,
                TaskThree = entity.TaskThree,
                StudyTargetMinutes = entity.StudyTargetMinutes,
                PersonalCommitment = entity.PersonalCommitment,
                MorningNotes = entity.MorningNotes,
                CompletedAtUtc = entity.MorningCompletedAtUtc
            },
            Evening = new EveningReflectionDto
            {
                MoodId = entity.EveningMoodId,
                MoodName = entity.EveningMoodName,
                MoodEmoji = entity.EveningMoodEmoji,
                MoodColor = entity.EveningMoodColor,
                MoodIntensity = entity.EveningMoodIntensity,
                HowWasToday = entity.HowWasToday,
                WhatAccomplished = entity.WhatAccomplished,
                WhatWentWell = entity.WhatWentWell,
                CouldHaveBeenBetter = entity.CouldHaveBeenBetter,
                WhatLearnedToday = entity.WhatLearnedToday,
                WhatChallengedMe = entity.WhatChallengedMe,
                SatisfactionRating = entity.SatisfactionRating,
                ImproveTomorrow = entity.ImproveTomorrow,
                GratitudeEntry = entity.GratitudeEntry,
                CompletedAtUtc = entity.EveningCompletedAtUtc
            }
        };
    }

    public async Task<DateOnly> GetUserLocalDateAsync(ulong userId, CancellationToken ct = default)
    {
        var tz = await ResolveTimeZoneAsync(userId, ct);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
    }

    private async Task<TimeZoneInfo> ResolveTimeZoneAsync(ulong userId, CancellationToken ct)
    {
        try
        {
            var profile = await _userProfileRepo.GetByUserIdAsync(userId, ct);
            if (!string.IsNullOrWhiteSpace(profile?.Timezone))
            {
                return TimeZoneInfo.FindSystemTimeZoneById(profile.Timezone);
            }
        }
        catch
        {
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}

