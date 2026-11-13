using Bloom.Core.Entities;

namespace Bloom.Core.Interfaces;

public interface IJournalCategoryRepository
{
    Task<JournalCategory?> GetByIdAsync(ulong id, CancellationToken ct = default);
    Task<IReadOnlyList<JournalCategory>> GetCategoriesForUserAsync(ulong userId, CancellationToken ct = default);
    Task<ulong> CreateUserCategoryAsync(JournalCategory category, CancellationToken ct = default);
}

public interface IJournalEntryRepository
{
    Task<JournalEntry?> GetByIdAsync(ulong id, ulong userId, CancellationToken ct = default);
    Task<ulong> CreateAsync(JournalEntry entry, CancellationToken ct = default);
    Task<bool> UpdateAsync(JournalEntry entry, uint expectedVersion, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(ulong id, ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<JournalEntry>> SearchAndFilterAsync(
        ulong userId,
        string? searchTerm,
        ulong? categoryId,
        DateOnly? fromDate,
        DateOnly? toDate,
        bool? isDraft,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<int> GetTotalCountAsync(
        ulong userId,
        string? searchTerm,
        ulong? categoryId,
        DateOnly? fromDate,
        DateOnly? toDate,
        bool? isDraft,
        CancellationToken ct = default);
    Task<IReadOnlyList<DateOnly>> GetCalendarDatesWithEntriesAsync(
        ulong userId,
        int year,
        int month,
        CancellationToken ct = default);
}

public interface IReflectionRepository
{
    Task<IReadOnlyList<ReflectionMood>> GetActiveMoodsAsync(CancellationToken ct = default);
    Task<ReflectionMood?> GetMoodByIdAsync(ulong id, CancellationToken ct = default);
    Task<DailyReflection?> GetByDateAsync(ulong userId, DateOnly reflectionDate, CancellationToken ct = default);
    Task<ulong> UpsertMorningReflectionAsync(DailyReflection reflection, CancellationToken ct = default);
    Task<ulong> UpsertEveningReflectionAsync(DailyReflection reflection, CancellationToken ct = default);
    Task<IReadOnlyList<DailyReflection>> GetRangeAsync(
        ulong userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default);
}

