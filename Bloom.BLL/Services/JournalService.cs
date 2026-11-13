using System.Text.RegularExpressions;
using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Bloom.Core.Exceptions.ValidationException;

namespace Bloom.BLL.Services;

public partial class JournalService : IJournalService
{
    private readonly IJournalEntryRepository _entryRepository;
    private readonly IJournalCategoryRepository _categoryRepository;
    private readonly IUserProfileRepository _userProfileRepo;
    private readonly IValidator<CreateJournalCategoryRequest> _createCategoryValidator;
    private readonly IValidator<CreateJournalEntryRequest> _createEntryValidator;
    private readonly IValidator<UpdateJournalEntryRequest> _updateEntryValidator;
    private readonly IValidator<JournalQueryParameters> _queryValidator;
    private readonly ILogger<JournalService> _logger;

    public JournalService(
        IJournalEntryRepository entryRepository,
        IJournalCategoryRepository categoryRepository,
        IUserProfileRepository userProfileRepo,
        IValidator<CreateJournalCategoryRequest> createCategoryValidator,
        IValidator<CreateJournalEntryRequest> createEntryValidator,
        IValidator<UpdateJournalEntryRequest> updateEntryValidator,
        IValidator<JournalQueryParameters> queryValidator,
        ILogger<JournalService> logger)
    {
        _entryRepository = entryRepository ?? throw new ArgumentNullException(nameof(entryRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _userProfileRepo = userProfileRepo ?? throw new ArgumentNullException(nameof(userProfileRepo));
        _createCategoryValidator = createCategoryValidator ?? throw new ArgumentNullException(nameof(createCategoryValidator));
        _createEntryValidator = createEntryValidator ?? throw new ArgumentNullException(nameof(createEntryValidator));
        _updateEntryValidator = updateEntryValidator ?? throw new ArgumentNullException(nameof(updateEntryValidator));
        _queryValidator = queryValidator ?? throw new ArgumentNullException(nameof(queryValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<JournalCategoryDto>> GetCategoriesAsync(ulong userId, CancellationToken ct = default)
    {
        var categories = await _categoryRepository.GetCategoriesForUserAsync(userId, ct);
        return categories.Select(c => new JournalCategoryDto
        {
            Id = c.Id,
            CategoryCode = c.CategoryCode,
            CategoryName = c.CategoryName,
            Description = c.Description,
            Icon = c.Icon,
            Color = c.Color,
            IsCustom = c.UserId.HasValue,
            DisplayOrder = c.DisplayOrder
        }).ToList();
    }

    public async Task<JournalCategoryDto> CreateUserCategoryAsync(ulong userId, CreateJournalCategoryRequest request, CancellationToken ct = default)
    {
        var validation = await _createCategoryValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.ToDictionary());
        }

        var existingCategories = await _categoryRepository.GetCategoriesForUserAsync(userId, ct);
        if (existingCategories.Any(c => string.Equals(c.CategoryCode, request.CategoryCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException($"Journal category code '{request.CategoryCode}' already exists for this user.");
        }

        var category = new JournalCategory
        {
            UserId = userId,
            CategoryCode = request.CategoryCode.Trim().ToUpperInvariant(),
            CategoryName = request.CategoryName.Trim(),
            Description = request.Description?.Trim(),
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? "book" : request.Icon.Trim(),
            Color = string.IsNullOrWhiteSpace(request.Color) ? "#A8BDA5" : request.Color.Trim(),
            IsActive = true,
            DisplayOrder = (ushort)(existingCategories.Count + 1),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var id = await _categoryRepository.CreateUserCategoryAsync(category, ct);
        category.Id = id;

        _logger.LogInformation("Created custom journal category {CategoryId} for user {UserId}", id, userId);

        return new JournalCategoryDto
        {
            Id = category.Id,
            CategoryCode = category.CategoryCode,
            CategoryName = category.CategoryName,
            Description = category.Description,
            Icon = category.Icon,
            Color = category.Color,
            IsCustom = true,
            DisplayOrder = category.DisplayOrder
        };
    }

    public async Task<JournalEntryDto> GetEntryByIdAsync(ulong userId, ulong entryId, CancellationToken ct = default)
    {
        var entry = await _entryRepository.GetByIdAsync(entryId, userId, ct);
        if (entry == null)
        {
            throw new NotFoundException("JournalEntry", entryId);
        }

        return MapToDto(entry);
    }

    public async Task<JournalEntryDto> CreateEntryAsync(ulong userId, CreateJournalEntryRequest request, CancellationToken ct = default)
    {
        var validation = await _createEntryValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.ToDictionary());
        }

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, ct);
        if (category == null || !category.IsActive || (category.UserId.HasValue && category.UserId.Value != userId))
        {
            throw new ValidationException("CategoryId", "The selected category does not exist, is inactive, or is not accessible.");
        }

        var entryDate = request.EntryDate != default
            ? request.EntryDate
            : await GetUserLocalDateAsync(userId, ct);

        var wordCount = CalculateWordCount(request.Content);

        var entry = new JournalEntry
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Content = request.Content ?? string.Empty,
            EntryDate = entryDate,
            IsDraft = request.IsDraft,
            WordCount = wordCount,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var id = await _entryRepository.CreateAsync(entry, ct);
        entry.Id = id;

        _logger.LogInformation("Journal entry {EntryId} created by user {UserId} (isDraft: {IsDraft}, words: {WordCount})",
            id, userId, entry.IsDraft, wordCount);

        var persisted = await _entryRepository.GetByIdAsync(id, userId, ct);
        return MapToDto(persisted ?? entry);
    }

    public async Task<JournalEntryDto> UpdateEntryAsync(ulong userId, ulong entryId, UpdateJournalEntryRequest request, CancellationToken ct = default)
    {
        var validation = await _updateEntryValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.ToDictionary());
        }

        var existing = await _entryRepository.GetByIdAsync(entryId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException("JournalEntry", entryId);
        }

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, ct);
        if (category == null || !category.IsActive || (category.UserId.HasValue && category.UserId.Value != userId))
        {
            throw new ValidationException("CategoryId", "The selected category does not exist, is inactive, or is not accessible.");
        }

        var entryDate = request.EntryDate != default
            ? request.EntryDate
            : existing.EntryDate;

        var wordCount = CalculateWordCount(request.Content);

        existing.CategoryId = request.CategoryId;
        existing.Title = request.Title.Trim();
        existing.Content = request.Content ?? string.Empty;
        existing.EntryDate = entryDate;
        existing.IsDraft = request.IsDraft;
        existing.WordCount = wordCount;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await _entryRepository.UpdateAsync(existing, request.ExpectedVersion, ct);
        if (!updated)
        {
            var current = await _entryRepository.GetByIdAsync(entryId, userId, ct);
            if (current == null)
            {
                throw new NotFoundException("JournalEntry", entryId);
            }
            throw new ConflictException("Journal entry was modified by another request. Please reload the latest version to prevent data loss.");
        }

        _logger.LogInformation("Journal entry {EntryId} updated by user {UserId} (isDraft: {IsDraft}, words: {WordCount}, version: {Version})",
            entryId, userId, existing.IsDraft, wordCount, existing.Version + 1);

        var refreshed = await _entryRepository.GetByIdAsync(entryId, userId, ct);
        return MapToDto(refreshed ?? existing);
    }

    public async Task<bool> DeleteEntryAsync(ulong userId, ulong entryId, CancellationToken ct = default)
    {
        var existing = await _entryRepository.GetByIdAsync(entryId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException("JournalEntry", entryId);
        }

        var deleted = await _entryRepository.SoftDeleteAsync(entryId, userId, ct);
        if (deleted)
        {
            _logger.LogInformation("Journal entry {EntryId} soft-deleted by user {UserId}", entryId, userId);
        }

        return deleted;
    }

    public async Task<PagedJournalResultDto> QueryEntriesAsync(ulong userId, JournalQueryParameters parameters, CancellationToken ct = default)
    {
        var validation = await _queryValidator.ValidateAsync(parameters, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.ToDictionary());
        }

        var totalCount = await _entryRepository.GetTotalCountAsync(
            userId,
            parameters.SearchTerm,
            parameters.CategoryId,
            parameters.FromDate,
            parameters.ToDate,
            parameters.IsDraft,
            ct);

        var entries = await _entryRepository.SearchAndFilterAsync(
            userId,
            parameters.SearchTerm,
            parameters.CategoryId,
            parameters.FromDate,
            parameters.ToDate,
            parameters.IsDraft,
            parameters.Page,
            parameters.PageSize,
            ct);

        var summaries = entries.Select(e => new JournalEntrySummaryDto
        {
            Id = e.Id,
            CategoryId = e.CategoryId,
            CategoryName = e.CategoryName ?? "General",
            CategoryCode = e.CategoryCode ?? "GENERAL",
            CategoryIcon = e.CategoryIcon ?? "book",
            CategoryColor = e.CategoryColor ?? "#A8BDA5",
            Title = e.Title,
            Excerpt = GenerateExcerpt(e.Content),
            EntryDate = e.EntryDate,
            IsDraft = e.IsDraft,
            WordCount = e.WordCount,
            Version = e.Version,
            CreatedAtUtc = e.CreatedAtUtc,
            UpdatedAtUtc = e.UpdatedAtUtc
        }).ToList();

        return new PagedJournalResultDto
        {
            Items = summaries,
            TotalCount = totalCount,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    public async Task<IReadOnlyList<DateOnly>> GetCalendarDatesWithEntriesAsync(ulong userId, int year, int month, CancellationToken ct = default)
    {
        if (year < 2000 || year > 2100)
        {
            throw new ValidationException("Year", "Year must be between 2000 and 2100.");
        }

        if (month < 1 || month > 12)
        {
            throw new ValidationException("Month", "Month must be between 1 and 12.");
        }

        return await _entryRepository.GetCalendarDatesWithEntriesAsync(userId, year, month, ct);
    }

    public static uint CalculateWordCount(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        var matches = WordRegex().Matches(content);
        return (uint)matches.Count;
    }

    private static string GenerateExcerpt(string? content, int maxLength = 160)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var clean = WhitespaceRegex().Replace(content.Trim(), " ");
        if (clean.Length <= maxLength)
        {
            return clean;
        }

        return clean[..maxLength].TrimEnd() + "...";
    }

    private static JournalEntryDto MapToDto(JournalEntry entry)
    {
        return new JournalEntryDto
        {
            Id = entry.Id,
            CategoryId = entry.CategoryId,
            CategoryName = entry.CategoryName ?? "General",
            CategoryCode = entry.CategoryCode ?? "GENERAL",
            CategoryIcon = entry.CategoryIcon ?? "book",
            CategoryColor = entry.CategoryColor ?? "#A8BDA5",
            Title = entry.Title,
            Content = entry.Content,
            EntryDate = entry.EntryDate,
            IsDraft = entry.IsDraft,
            WordCount = entry.WordCount,
            Version = entry.Version,
            CreatedAtUtc = entry.CreatedAtUtc,
            UpdatedAtUtc = entry.UpdatedAtUtc
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

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.Compiled)]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}

