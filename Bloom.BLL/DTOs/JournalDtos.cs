namespace Bloom.BLL.DTOs;

public class JournalCategoryDto
{
    public ulong Id { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = "book";
    public string Color { get; set; } = "#A8BDA5";
    public bool IsCustom { get; set; }
    public ushort DisplayOrder { get; set; }
}

public class CreateJournalCategoryRequest
{
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = "book";
    public string Color { get; set; } = "#A8BDA5";
}

public class JournalEntryDto
{
    public ulong Id { get; set; }
    public ulong CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = "book";
    public string CategoryColor { get; set; } = "#A8BDA5";
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public bool IsDraft { get; set; }
    public uint WordCount { get; set; }
    public uint Version { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class JournalEntrySummaryDto
{
    public ulong Id { get; set; }
    public ulong CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = "book";
    public string CategoryColor { get; set; } = "#A8BDA5";
    public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public bool IsDraft { get; set; }
    public uint WordCount { get; set; }
    public uint Version { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateJournalEntryRequest
{
    public ulong CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public bool IsDraft { get; set; }
}

public class UpdateJournalEntryRequest
{
    public ulong CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public bool IsDraft { get; set; }
    public uint ExpectedVersion { get; set; }
}

public class JournalQueryParameters
{
    public string? SearchTerm { get; set; }
    public ulong? CategoryId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public bool? IsDraft { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedJournalResultDto
{
    public IReadOnlyList<JournalEntrySummaryDto> Items { get; set; } = Array.Empty<JournalEntrySummaryDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

