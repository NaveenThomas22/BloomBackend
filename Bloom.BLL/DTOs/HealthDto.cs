namespace Bloom.BLL.DTOs;

public class HealthStatusDto
{
    public string Status { get; set; } = "Healthy";
    public string Application { get; set; } = "Bloom - My CAT Journey";
    public string Version { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Development";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public class DatabaseHealthDto
{
    public string Status { get; set; } = "Unknown";
    public bool IsConnected { get; set; }
    public long ResponseTimeMs { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
}

