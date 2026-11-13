using Bloom.BLL.DTOs;

namespace Bloom.BLL.Interfaces;

public interface IJournalService
{
    Task<IReadOnlyList<JournalCategoryDto>> GetCategoriesAsync(ulong userId, CancellationToken ct = default);
    Task<JournalCategoryDto> CreateUserCategoryAsync(ulong userId, CreateJournalCategoryRequest request, CancellationToken ct = default);
    Task<JournalEntryDto> GetEntryByIdAsync(ulong userId, ulong entryId, CancellationToken ct = default);
    Task<JournalEntryDto> CreateEntryAsync(ulong userId, CreateJournalEntryRequest request, CancellationToken ct = default);
    Task<JournalEntryDto> UpdateEntryAsync(ulong userId, ulong entryId, UpdateJournalEntryRequest request, CancellationToken ct = default);
    Task<bool> DeleteEntryAsync(ulong userId, ulong entryId, CancellationToken ct = default);
    Task<PagedJournalResultDto> QueryEntriesAsync(ulong userId, JournalQueryParameters parameters, CancellationToken ct = default);
    Task<IReadOnlyList<DateOnly>> GetCalendarDatesWithEntriesAsync(ulong userId, int year, int month, CancellationToken ct = default);
}

