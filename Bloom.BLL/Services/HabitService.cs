using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Bloom.Core.Exceptions.ValidationException;

namespace Bloom.BLL.Services;

public class HabitService : IHabitService
{
    private readonly IHabitRepository _habitRepository;
    private readonly IValidator<CreateHabitRequest> _createValidator;
    private readonly IValidator<UpdateHabitRequest> _updateValidator;
    private readonly IValidator<RecordHabitCompletionRequest> _recordValidator;
    private readonly ILogger<HabitService> _logger;

    public HabitService(
        IHabitRepository habitRepository,
        IValidator<CreateHabitRequest> createValidator,
        IValidator<UpdateHabitRequest> updateValidator,
        IValidator<RecordHabitCompletionRequest> recordValidator,
        ILogger<HabitService> logger)
    {
        _habitRepository = habitRepository ?? throw new ArgumentNullException(nameof(habitRepository));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _recordValidator = recordValidator ?? throw new ArgumentNullException(nameof(recordValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<HabitCategoryDto>> GetCategoriesAsync(ulong userId, CancellationToken ct = default)
    {
        var categories = await _habitRepository.GetCategoriesAsync(userId, ct);
        return categories.Select(c => new HabitCategoryDto
        {
            Id = c.Id,
            UserId = c.UserId,
            CategoryCode = c.CategoryCode,
            CategoryName = c.CategoryName,
            Description = c.Description,
            Icon = c.Icon,
            Color = c.Color,
            DisplayOrder = c.DisplayOrder,
            IsActive = c.IsActive
        }).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<UserHabitDto>> GetHabitsAsync(ulong userId, bool activeOnly = true, CancellationToken ct = default)
    {
        var habits = await _habitRepository.GetHabitsAsync(userId, activeOnly, ct);
        return habits.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<UserHabitDto?> GetHabitByIdAsync(ulong habitId, ulong userId, CancellationToken ct = default)
    {
        var habit = await _habitRepository.GetHabitByIdAsync(habitId, userId, ct);
        return habit != null ? MapToDto(habit) : null;
    }

    public async Task<UserHabitDto> CreateHabitAsync(ulong userId, CreateHabitRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.ToDictionary());
        }

        var category = await _habitRepository.GetCategoryByIdAsync(request.CategoryId, userId, ct);
        if (category == null)
        {
            throw new NotFoundException($"Habit category with ID {request.CategoryId} not found.");
        }

        var habit = new UserHabit
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Frequency = request.Frequency.Trim(),
            TargetFrequencyDays = request.TargetFrequencyDays,
            TargetValue = request.TargetValue,
            TargetUnit = request.TargetUnit.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true,
            DisplayOrder = request.DisplayOrder,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var id = await _habitRepository.CreateHabitAsync(habit, ct);
        habit.Id = id;
        habit.CategoryName = category.CategoryName;
        habit.CategoryCode = category.CategoryCode;
        habit.CategoryIcon = category.Icon;
        habit.CategoryColor = category.Color;

        _logger.LogInformation("Habit {HabitId} '{HabitName}' created for user {UserId}", id, habit.Name, userId);
        return MapToDto(habit);
    }

    public async Task<UserHabitDto?> UpdateHabitAsync(ulong habitId, ulong userId, UpdateHabitRequest request, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.ToDictionary());
        }

        var existing = await _habitRepository.GetHabitByIdAsync(habitId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Habit with ID {habitId} not found.");
        }

        var category = await _habitRepository.GetCategoryByIdAsync(request.CategoryId, userId, ct);
        if (category == null)
        {
            throw new NotFoundException($"Habit category with ID {request.CategoryId} not found.");
        }

        existing.CategoryId = request.CategoryId;
        existing.Name = request.Name.Trim();
        existing.Description = request.Description?.Trim();
        existing.Frequency = request.Frequency.Trim();
        existing.TargetFrequencyDays = request.TargetFrequencyDays;
        existing.TargetValue = request.TargetValue;
        existing.TargetUnit = request.TargetUnit.Trim();
        existing.StartDate = request.StartDate;
        existing.EndDate = request.EndDate;
        existing.IsActive = request.IsActive;
        existing.DisplayOrder = request.DisplayOrder;
        existing.CategoryName = category.CategoryName;
        existing.CategoryCode = category.CategoryCode;
        existing.CategoryIcon = category.Icon;
        existing.CategoryColor = category.Color;

        var success = await _habitRepository.UpdateHabitAsync(existing, ct);
        if (!success)
        {
            throw new InvalidOperationException("Failed to update habit.");
        }

        _logger.LogInformation("Habit {HabitId} updated for user {UserId}", habitId, userId);
        return MapToDto(existing);
    }

    public async Task<bool> DeleteHabitAsync(ulong habitId, ulong userId, CancellationToken ct = default)
    {
        var existing = await _habitRepository.GetHabitByIdAsync(habitId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Habit with ID {habitId} not found.");
        }

        var success = await _habitRepository.DeleteHabitAsync(habitId, userId, ct);
        _logger.LogInformation("Habit {HabitId} deleted for user {UserId}", habitId, userId);
        return success;
    }

    public async Task<IReadOnlyList<HabitCompletionDto>> GetCompletionsForDateAsync(ulong userId, DateOnly date, CancellationToken ct = default)
    {
        var completions = await _habitRepository.GetCompletionsForDateAsync(userId, date, ct);
        return completions.Select(MapCompletionToDto).ToList().AsReadOnly();
    }

    public async Task<HabitCompletionDto> RecordCompletionAsync(ulong userId, ulong habitId, RecordHabitCompletionRequest request, CancellationToken ct = default)
    {
        var validation = await _recordValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.ToDictionary());
        }

        var habit = await _habitRepository.GetHabitByIdAsync(habitId, userId, ct);
        if (habit == null)
        {
            throw new NotFoundException($"Habit with ID {habitId} not found.");
        }

        var completion = new HabitCompletion
        {
            UserId = userId,
            HabitId = habitId,
            CompletionDate = request.CompletionDate,
            Status = request.Status.Trim(),
            ActualValue = request.ActualValue,
            SkipReason = request.SkipReason?.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var success = await _habitRepository.RecordCompletionAsync(completion, ct);
        if (!success)
        {
            throw new InvalidOperationException("Failed to record habit completion.");
        }

        _logger.LogInformation("Completion recorded for habit {HabitId} on {Date} by user {UserId}", habitId, request.CompletionDate, userId);
        return MapCompletionToDto(completion);
    }

    public async Task<bool> DeleteCompletionAsync(ulong userId, ulong habitId, DateOnly date, CancellationToken ct = default)
    {
        var habit = await _habitRepository.GetHabitByIdAsync(habitId, userId, ct);
        if (habit == null)
        {
            throw new NotFoundException($"Habit with ID {habitId} not found.");
        }

        return await _habitRepository.DeleteCompletionAsync(userId, habitId, date, ct);
    }

    public async Task<HabitOverviewDto> GetOverviewAsync(ulong userId, DateOnly? referenceDate = null, CancellationToken ct = default)
    {
        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var habits = await _habitRepository.GetHabitsAsync(userId, activeOnly: true, ct);
        var completionsToday = await _habitRepository.GetCompletionsForDateAsync(userId, today, ct);
        var streaks = await _habitRepository.GetStreaksAsync(userId, ct);

        var completionMap = completionsToday.ToDictionary(c => c.HabitId);
        var streakMap = streaks.ToDictionary(s => s.HabitId);

        var habitDtos = habits.Select(h =>
        {
            var dto = MapToDto(h);
            if (completionMap.TryGetValue(h.Id, out var comp))
            {
                dto.TodayCompletion = MapCompletionToDto(comp);
            }
            if (streakMap.TryGetValue(h.Id, out var strk))
            {
                dto.CurrentStreak = strk.CurrentStreak;
            }
            return dto;
        }).ToList();

        var completedCount = habitDtos.Count(h => h.TodayCompletion != null && string.Equals(h.TodayCompletion.Status, "Completed", StringComparison.OrdinalIgnoreCase));
        var rate = habitDtos.Count > 0 ? Math.Round((double)completedCount / habitDtos.Count * 100.0, 1) : 0;
        var bestStreak = streaks.Count > 0 ? streaks.Max(s => s.CurrentStreak) : 0;

        return new HabitOverviewDto
        {
            ReferenceDate = today,
            Habits = habitDtos.AsReadOnly(),
            Streaks = streaks.Select(s => new HabitStreakDto
            {
                HabitId = s.HabitId,
                HabitName = s.HabitName,
                CurrentStreak = s.CurrentStreak,
                LongestStreak = s.LongestStreak,
                TotalCompletions = s.TotalCompletions,
                CompletionRate30Days = s.CompletionRate30Days,
                LastCompletedDate = s.LastCompletedDate
            }).ToList().AsReadOnly(),
            TotalHabits = habitDtos.Count,
            CompletedToday = completedCount,
            TodayCompletionRate = rate,
            CurrentBestStreak = bestStreak
        };
    }

    private static UserHabitDto MapToDto(UserHabit h) => new()
    {
        Id = h.Id,
        UserId = h.UserId,
        CategoryId = h.CategoryId,
        Name = h.Name,
        Description = h.Description,
        Frequency = h.Frequency,
        TargetFrequencyDays = h.TargetFrequencyDays,
        TargetValue = h.TargetValue,
        TargetUnit = h.TargetUnit,
        StartDate = h.StartDate,
        EndDate = h.EndDate,
        IsActive = h.IsActive,
        DisplayOrder = h.DisplayOrder,
        CreatedAtUtc = h.CreatedAtUtc,
        UpdatedAtUtc = h.UpdatedAtUtc,
        CategoryName = h.CategoryName,
        CategoryCode = h.CategoryCode,
        CategoryIcon = h.CategoryIcon,
        CategoryColor = h.CategoryColor
    };

    private static HabitCompletionDto MapCompletionToDto(HabitCompletion c) => new()
    {
        Id = c.Id,
        UserId = c.UserId,
        HabitId = c.HabitId,
        CompletionDate = c.CompletionDate,
        Status = c.Status,
        ActualValue = c.ActualValue,
        SkipReason = c.SkipReason,
        Notes = c.Notes,
        CreatedAtUtc = c.CreatedAtUtc,
        UpdatedAtUtc = c.UpdatedAtUtc
    };
}

