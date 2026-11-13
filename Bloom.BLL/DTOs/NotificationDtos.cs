namespace Bloom.BLL.DTOs;

public class NotificationCategoryDto
{
    public ulong Id { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = "bell";
    public string Color { get; set; } = "#A8BDA5";
    public bool DefaultEnabled { get; set; } = true;
}

public class UserNotificationPreferenceDto
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public TimeSpan? PreferredTimeUtc { get; set; }
    public TimeSpan? QuietHoursStartUtc { get; set; }
    public TimeSpan? QuietHoursEndUtc { get; set; }
    public string Channels { get; set; } = "IN_APP,PUSH";
    public uint MaxDailyAlerts { get; set; } = 10;
}

public class UpdateNotificationPreferenceRequest
{
    public bool IsEnabled { get; set; } = true;
    public TimeSpan? PreferredTimeUtc { get; set; }
    public TimeSpan? QuietHoursStartUtc { get; set; }
    public TimeSpan? QuietHoursEndUtc { get; set; }
    public string Channels { get; set; } = "IN_APP,PUSH";
    public uint MaxDailyAlerts { get; set; } = 10;
}

public class UserNotificationDto
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DeepLink { get; set; }
    public string Status { get; set; } = "Unread"; // Unread, Read, Dismissed
    public DateTime? ReadAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateNotificationRequest
{
    public string CategoryCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DeepLink { get; set; }
}

public class PushSubscriptionRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
}

public class NotificationOverviewDto
{
    public int UnreadCount { get; set; }
    public IReadOnlyList<UserNotificationDto> RecentNotifications { get; set; } = Array.Empty<UserNotificationDto>();
}

