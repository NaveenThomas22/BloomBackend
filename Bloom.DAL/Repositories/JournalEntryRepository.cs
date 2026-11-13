using System.Text;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class JournalEntryRepository : IJournalEntryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public JournalEntryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<JournalEntry?> GetByIdAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT e.id, e.user_id AS UserId, e.category_id AS CategoryId, e.title, e.content,
                   e.entry_date AS EntryDate, e.is_draft AS IsDraft, e.word_count AS WordCount,
                   e.version AS Version, e.created_at_utc AS CreatedAtUtc, e.updated_at_utc AS UpdatedAtUtc,
                   e.deleted_at_utc AS DeletedAtUtc,
                   c.category_name AS CategoryName, c.category_code AS CategoryCode, c.icon AS CategoryIcon, c.color AS CategoryColor
            FROM journal_entries e
            INNER JOIN journal_categories c ON e.category_id = c.id
            WHERE e.id = @id AND e.user_id = @userId AND e.deleted_at_utc IS NULL;";

        return await connection.QuerySingleOrDefaultAsync<JournalEntry>(
            new CommandDefinition(sql, new { id, userId }, cancellationToken: ct));
    }

    public async Task<ulong> CreateAsync(JournalEntry entry, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO journal_entries (user_id, category_id, title, content, entry_date, is_draft, word_count, created_at_utc, updated_at_utc)
            VALUES (@UserId, @CategoryId, @Title, @Content, @EntryDate, @IsDraft, @WordCount, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        return await connection.ExecuteScalarAsync<ulong>(
            new CommandDefinition(sql, new
            {
                entry.UserId,
                entry.CategoryId,
                entry.Title,
                entry.Content,
                EntryDate = entry.EntryDate.ToDateTime(TimeOnly.MinValue),
                entry.IsDraft,
                entry.WordCount
            }, cancellationToken: ct));
    }

    public async Task<bool> UpdateAsync(JournalEntry entry, uint expectedVersion, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE journal_entries
            SET category_id = @CategoryId,
                title = @Title,
                content = @Content,
                entry_date = @EntryDate,
                is_draft = @IsDraft,
                word_count = @WordCount,
                version = version + 1,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id AND user_id = @UserId AND deleted_at_utc IS NULL
              AND version = @ExpectedVersion;";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                entry.Id,
                entry.UserId,
                entry.CategoryId,
                entry.Title,
                entry.Content,
                EntryDate = entry.EntryDate.ToDateTime(TimeOnly.MinValue),
                entry.IsDraft,
                entry.WordCount,
                ExpectedVersion = expectedVersion
            }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<bool> SoftDeleteAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE journal_entries
            SET deleted_at_utc = UTC_TIMESTAMP()
            WHERE id = @id AND user_id = @userId AND deleted_at_utc IS NULL;";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { id, userId }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<IReadOnlyList<JournalEntry>> SearchAndFilterAsync(
        ulong userId,
        string? searchTerm,
        ulong? categoryId,
        DateOnly? fromDate,
        DateOnly? toDate,
        bool? isDraft,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        var sqlBuilder = new StringBuilder(@"
            SELECT e.id, e.user_id AS UserId, e.category_id AS CategoryId, e.title, e.content,
                   e.entry_date AS EntryDate, e.is_draft AS IsDraft, e.word_count AS WordCount,
                   e.version AS Version,
                   e.created_at_utc AS CreatedAtUtc, e.updated_at_utc AS UpdatedAtUtc, e.deleted_at_utc AS DeletedAtUtc,
                   c.category_name AS CategoryName, c.category_code AS CategoryCode, c.icon AS CategoryIcon, c.color AS CategoryColor
            FROM journal_entries e
            INNER JOIN journal_categories c ON e.category_id = c.id
            WHERE e.user_id = @userId AND e.deleted_at_utc IS NULL");

        var parameters = new DynamicParameters();
        parameters.Add("userId", userId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            sqlBuilder.Append(" AND (e.title LIKE @searchTerm OR e.content LIKE @searchTerm)");
            parameters.Add("searchTerm", $"%{searchTerm.Trim()}%");
        }

        if (categoryId.HasValue)
        {
            sqlBuilder.Append(" AND e.category_id = @categoryId");
            parameters.Add("categoryId", categoryId.Value);
        }

        if (fromDate.HasValue)
        {
            sqlBuilder.Append(" AND e.entry_date >= @fromDate");
            parameters.Add("fromDate", fromDate.Value.ToDateTime(TimeOnly.MinValue));
        }

        if (toDate.HasValue)
        {
            sqlBuilder.Append(" AND e.entry_date <= @toDate");
            parameters.Add("toDate", toDate.Value.ToDateTime(TimeOnly.MinValue));
        }

        if (isDraft.HasValue)
        {
            sqlBuilder.Append(" AND e.is_draft = @isDraft");
            parameters.Add("isDraft", isDraft.Value);
        }

        sqlBuilder.Append(" ORDER BY e.entry_date DESC, e.created_at_utc DESC");
        sqlBuilder.Append(" LIMIT @limit OFFSET @offset;");

        parameters.Add("limit", Math.Max(1, pageSize));
        parameters.Add("offset", Math.Max(0, (page - 1) * pageSize));

        var results = await connection.QueryAsync<JournalEntry>(
            new CommandDefinition(sqlBuilder.ToString(), parameters, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<int> GetTotalCountAsync(
        ulong userId,
        string? searchTerm,
        ulong? categoryId,
        DateOnly? fromDate,
        DateOnly? toDate,
        bool? isDraft,
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        var sqlBuilder = new StringBuilder(@"
            SELECT COUNT(*)
            FROM journal_entries e
            WHERE e.user_id = @userId AND e.deleted_at_utc IS NULL");

        var parameters = new DynamicParameters();
        parameters.Add("userId", userId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            sqlBuilder.Append(" AND (e.title LIKE @searchTerm OR e.content LIKE @searchTerm)");
            parameters.Add("searchTerm", $"%{searchTerm.Trim()}%");
        }

        if (categoryId.HasValue)
        {
            sqlBuilder.Append(" AND e.category_id = @categoryId");
            parameters.Add("categoryId", categoryId.Value);
        }

        if (fromDate.HasValue)
        {
            sqlBuilder.Append(" AND e.entry_date >= @fromDate");
            parameters.Add("fromDate", fromDate.Value.ToDateTime(TimeOnly.MinValue));
        }

        if (toDate.HasValue)
        {
            sqlBuilder.Append(" AND e.entry_date <= @toDate");
            parameters.Add("toDate", toDate.Value.ToDateTime(TimeOnly.MinValue));
        }

        if (isDraft.HasValue)
        {
            sqlBuilder.Append(" AND e.is_draft = @isDraft");
            parameters.Add("isDraft", isDraft.Value);
        }

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlBuilder.ToString(), parameters, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<DateOnly>> GetCalendarDatesWithEntriesAsync(
        ulong userId,
        int year,
        int month,
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT DISTINCT entry_date
            FROM journal_entries
            WHERE user_id = @userId
              AND deleted_at_utc IS NULL
              AND YEAR(entry_date) = @year
              AND MONTH(entry_date) = @month
            ORDER BY entry_date ASC;";

        var dates = await connection.QueryAsync<DateTime>(
            new CommandDefinition(sql, new { userId, year, month }, cancellationToken: ct));

        return dates.Select(DateOnly.FromDateTime).ToList();
    }
}

