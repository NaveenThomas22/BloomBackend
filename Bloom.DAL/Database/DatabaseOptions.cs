namespace Bloom.DAL.Database;

public class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    /// <summary>
    /// The MySQL connection string for Bloom.
    /// </summary>
    public string BloomConnection { get; set; } = string.Empty;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Command timeout in seconds for queries.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
}

