using Bloom.Core.Entities;

namespace Bloom.Core.Interfaces;

public interface IHabitRepository
{
    Task<IReadOnlyList<HabitCategory>> GetCategoriesAsync(ulong userId, CancellationToken ct = default);
    Task<HabitCategory?> GetCategoryByIdAsync(ulong categoryId, ulong userId, CancellationToken ct = default);
    Task<ulong> CreateCategoryAsync(HabitCategory category, CancellationToken ct = default);
    Task<IReadOnlyList<UserHabit>> GetHabitsAsync(ulong userId, bool activeOnly = true, CancellationToken ct = default);
    Task<UserHabit?> GetHabitByIdAsync(ulong habitId, ulong userId, CancellationToken ct = default);
    Task<ulong> CreateHabitAsync(UserHabit habit, CancellationToken ct = default);
    Task<bool> UpdateHabitAsync(UserHabit habit, CancellationToken ct = default);
    Task<bool> DeleteHabitAsync(ulong habitId, ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<HabitCompletion>> GetCompletionsForDateAsync(ulong userId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<HabitCompletion>> GetCompletionsForRangeAsync(ulong userId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
    Task<HabitCompletion?> GetCompletionAsync(ulong userId, ulong habitId, DateOnly date, CancellationToken ct = default);
    Task<bool> RecordCompletionAsync(HabitCompletion completion, CancellationToken ct = default);
    Task<bool> DeleteCompletionAsync(ulong userId, ulong habitId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<HabitStreakInfo>> GetStreaksAsync(ulong userId, CancellationToken ct = default);
}

public interface IPersonalMilestoneRepository
{
    Task<IReadOnlyList<PersonalMilestone>> GetMilestonesAsync(ulong userId, string? status = null, CancellationToken ct = default);
    Task<PersonalMilestone?> GetMilestoneByIdAsync(ulong milestoneId, ulong userId, CancellationToken ct = default);
    Task<ulong> CreateMilestoneAsync(PersonalMilestone milestone, CancellationToken ct = default);
    Task<bool> UpdateMilestoneAsync(PersonalMilestone milestone, CancellationToken ct = default);
    Task<bool> DeleteMilestoneAsync(ulong milestoneId, ulong userId, CancellationToken ct = default);
    Task<bool> UpdateProgressAsync(ulong milestoneId, ulong userId, decimal currentValue, string status, DateTime? achievedAtUtc, CancellationToken ct = default);
    Task<MilestoneSystemMetrics> GetUserSystemMetricsAsync(ulong userId, CancellationToken ct = default);
}

public interface INotificationRepository
{
    Task<IReadOnlyList<NotificationCategory>> GetCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UserNotificationPreference>> GetPreferencesAsync(ulong userId, CancellationToken ct = default);
    Task<UserNotificationPreference?> GetPreferenceAsync(ulong userId, string categoryCode, CancellationToken ct = default);
    Task<bool> UpsertPreferenceAsync(UserNotificationPreference preference, CancellationToken ct = default);
    Task<IReadOnlyList<UserNotification>> GetNotificationsAsync(ulong userId, int limit = 50, int offset = 0, string? status = null, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(ulong userId, CancellationToken ct = default);
    Task<UserNotification?> GetNotificationByIdAsync(ulong notificationId, ulong userId, CancellationToken ct = default);
    Task<ulong> CreateNotificationAsync(UserNotification notification, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(ulong notificationId, ulong userId, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(ulong userId, CancellationToken ct = default);
    Task<bool> DismissNotificationAsync(ulong notificationId, ulong userId, CancellationToken ct = default);
    Task<bool> UpsertSubscriptionAsync(NotificationSubscription subscription, CancellationToken ct = default);
    Task<bool> DeactivateSubscriptionAsync(ulong userId, string endpoint, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationSubscription>> GetActiveSubscriptionsAsync(ulong userId, CancellationToken ct = default);
    Task<ulong> RecordDeliveryAttemptAsync(NotificationDeliveryAttempt attempt, CancellationToken ct = default);
    Task<int> GetDailyAlertCountAsync(ulong userId, string categoryCode, DateOnly dateUtc, CancellationToken ct = default);
    Task<SmartNotificationTriggersData> GetSmartTriggerMetricsAsync(ulong userId, DateOnly localDate, CancellationToken ct = default);
}
