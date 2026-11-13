using Bloom.BLL.DTOs;

namespace Bloom.BLL.Interfaces;

public interface IHabitService
{
    Task<IReadOnlyList<HabitCategoryDto>> GetCategoriesAsync(ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserHabitDto>> GetHabitsAsync(ulong userId, bool activeOnly = true, CancellationToken ct = default);
    Task<UserHabitDto?> GetHabitByIdAsync(ulong habitId, ulong userId, CancellationToken ct = default);
    Task<UserHabitDto> CreateHabitAsync(ulong userId, CreateHabitRequest request, CancellationToken ct = default);
    Task<UserHabitDto?> UpdateHabitAsync(ulong habitId, ulong userId, UpdateHabitRequest request, CancellationToken ct = default);
    Task<bool> DeleteHabitAsync(ulong habitId, ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<HabitCompletionDto>> GetCompletionsForDateAsync(ulong userId, DateOnly date, CancellationToken ct = default);
    Task<HabitCompletionDto> RecordCompletionAsync(ulong userId, ulong habitId, RecordHabitCompletionRequest request, CancellationToken ct = default);
    Task<bool> DeleteCompletionAsync(ulong userId, ulong habitId, DateOnly date, CancellationToken ct = default);
    Task<HabitOverviewDto> GetOverviewAsync(ulong userId, DateOnly? referenceDate = null, CancellationToken ct = default);
}

public interface IPersonalMilestoneService
{
    Task<IReadOnlyList<PersonalMilestoneDto>> GetMilestonesAsync(ulong userId, string? status = null, CancellationToken ct = default);
    Task<PersonalMilestoneDto?> GetMilestoneByIdAsync(ulong milestoneId, ulong userId, CancellationToken ct = default);
    Task<PersonalMilestoneDto> CreateMilestoneAsync(ulong userId, CreateMilestoneRequest request, CancellationToken ct = default);
    Task<PersonalMilestoneDto?> UpdateMilestoneAsync(ulong milestoneId, ulong userId, UpdateMilestoneRequest request, CancellationToken ct = default);
    Task<bool> DeleteMilestoneAsync(ulong milestoneId, ulong userId, CancellationToken ct = default);
    Task<MilestoneOverviewDto> GetOverviewAsync(ulong userId, CancellationToken ct = default);
    Task<int> SyncMilestoneProgressAsync(ulong userId, CancellationToken ct = default);
}

public interface INotificationService
{
    Task<IReadOnlyList<NotificationCategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UserNotificationPreferenceDto>> GetPreferencesAsync(ulong userId, CancellationToken ct = default);
    Task<bool> UpdatePreferenceAsync(ulong userId, string categoryCode, UpdateNotificationPreferenceRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<UserNotificationDto>> GetNotificationsAsync(ulong userId, int limit = 50, int offset = 0, string? status = null, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(ulong userId, CancellationToken ct = default);
    Task<UserNotificationDto> SendNotificationAsync(ulong userId, CreateNotificationRequest request, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(ulong notificationId, ulong userId, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(ulong userId, CancellationToken ct = default);
    Task<bool> DismissNotificationAsync(ulong notificationId, ulong userId, CancellationToken ct = default);
    Task<bool> RegisterPushSubscriptionAsync(ulong userId, PushSubscriptionRequest request, CancellationToken ct = default);
    Task<bool> UnsubscribePushAsync(ulong userId, string endpoint, CancellationToken ct = default);
    Task<NotificationOverviewDto> GetOverviewAsync(ulong userId, CancellationToken ct = default);
    Task<int> TriggerSmartNotificationsAsync(ulong userId, DateOnly userLocalDate, TimeSpan userLocalTime, CancellationToken ct = default);
}

public class WebPushSendResult
{
    public bool Success { get; set; }
    public bool IsExpiredOrUnregistered { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IWebPushService
{
    Task<WebPushSendResult> SendWebPushAsync(string endpoint, string p256dh, string auth, string title, string message, string? deepLink, CancellationToken ct = default);
}

