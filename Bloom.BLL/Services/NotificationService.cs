using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Bloom.Core.Exceptions.ValidationException;

namespace Bloom.BLL.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IWebPushService _webPushService;
    private readonly IValidator<CreateNotificationRequest> _createValidator;
    private readonly IValidator<UpdateNotificationPreferenceRequest> _prefValidator;
    private readonly IValidator<PushSubscriptionRequest> _subValidator;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        IWebPushService webPushService,
        IValidator<CreateNotificationRequest> createValidator,
        IValidator<UpdateNotificationPreferenceRequest> prefValidator,
        IValidator<PushSubscriptionRequest> subValidator,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _webPushService = webPushService ?? throw new ArgumentNullException(nameof(webPushService));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _prefValidator = prefValidator ?? throw new ArgumentNullException(nameof(prefValidator));
        _subValidator = subValidator ?? throw new ArgumentNullException(nameof(subValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<NotificationCategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _notificationRepository.GetCategoriesAsync(ct);
        return categories.Select(c => new NotificationCategoryDto
        {
            Id = c.Id,
            CategoryCode = c.CategoryCode,
            CategoryName = c.CategoryName,
            Description = c.Description,
            Icon = c.Icon,
            Color = c.Color,
            DefaultEnabled = c.DefaultEnabled
        }).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<UserNotificationPreferenceDto>> GetPreferencesAsync(ulong userId, CancellationToken ct = default)
    {
        var categories = await _notificationRepository.GetCategoriesAsync(ct);
        var existingPrefs = (await _notificationRepository.GetPreferencesAsync(userId, ct)).ToDictionary(p => p.CategoryCode);

        var list = new List<UserNotificationPreferenceDto>();
        foreach (var cat in categories)
        {
            if (existingPrefs.TryGetValue(cat.CategoryCode, out var pref))
            {
                list.Add(new UserNotificationPreferenceDto
                {
                    Id = pref.Id,
                    UserId = pref.UserId,
                    CategoryCode = pref.CategoryCode,
                    IsEnabled = pref.IsEnabled,
                    PreferredTimeUtc = pref.PreferredTimeUtc,
                    QuietHoursStartUtc = pref.QuietHoursStartUtc,
                    QuietHoursEndUtc = pref.QuietHoursEndUtc,
                    Channels = pref.Channels,
                    MaxDailyAlerts = pref.MaxDailyAlerts
                });
            }
            else
            {
                list.Add(new UserNotificationPreferenceDto
                {
                    Id = 0,
                    UserId = userId,
                    CategoryCode = cat.CategoryCode,
                    IsEnabled = cat.DefaultEnabled,
                    PreferredTimeUtc = null,
                    QuietHoursStartUtc = new TimeSpan(23, 0, 0),
                    QuietHoursEndUtc = new TimeSpan(7, 0, 0),
                    Channels = "IN_APP,PUSH",
                    MaxDailyAlerts = 10
                });
            }
        }

        return list.AsReadOnly();
    }

    public async Task<bool> UpdatePreferenceAsync(ulong userId, string categoryCode, UpdateNotificationPreferenceRequest request, CancellationToken ct = default)
    {
        var validation = await _prefValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.ToDictionary());
        }

        var pref = new UserNotificationPreference
        {
            UserId = userId,
            CategoryCode = categoryCode.Trim().ToUpperInvariant(),
            IsEnabled = request.IsEnabled,
            PreferredTimeUtc = request.PreferredTimeUtc,
            QuietHoursStartUtc = request.QuietHoursStartUtc,
            QuietHoursEndUtc = request.QuietHoursEndUtc,
            Channels = request.Channels.Trim(),
            MaxDailyAlerts = request.MaxDailyAlerts,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        return await _notificationRepository.UpsertPreferenceAsync(pref, ct);
    }

    public async Task<IReadOnlyList<UserNotificationDto>> GetNotificationsAsync(ulong userId, int limit = 50, int offset = 0, string? status = null, CancellationToken ct = default)
    {
        var notifications = await _notificationRepository.GetNotificationsAsync(userId, limit, offset, status, ct);
        return notifications.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<int> GetUnreadCountAsync(ulong userId, CancellationToken ct = default)
    {
        return await _notificationRepository.GetUnreadCountAsync(userId, ct);
    }

    public async Task<UserNotificationDto> SendNotificationAsync(ulong userId, CreateNotificationRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.ToDictionary());
        }

        var pref = await _notificationRepository.GetPreferenceAsync(userId, request.CategoryCode, ct);
        if (pref != null && !pref.IsEnabled)
        {
            _logger.LogInformation("Notification {Category} skipped for user {UserId} because preference is disabled.", request.CategoryCode, userId);
            return new UserNotificationDto
            {
                UserId = userId,
                CategoryCode = request.CategoryCode,
                Title = request.Title,
                Message = request.Message,
                Status = "Suppressed"
            };
        }

        // Check daily count limit
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var dailyCount = await _notificationRepository.GetDailyAlertCountAsync(userId, request.CategoryCode, todayUtc, ct);
        var maxDaily = pref?.MaxDailyAlerts ?? 10;
        if (dailyCount >= maxDaily)
        {
            _logger.LogInformation("Daily notification limit reached for user {UserId} and category {Category}", userId, request.CategoryCode);
            return new UserNotificationDto
            {
                UserId = userId,
                CategoryCode = request.CategoryCode,
                Title = request.Title,
                Message = request.Message,
                Status = "SuppressedDailyLimit"
            };
        }

        var notification = new UserNotification
        {
            UserId = userId,
            CategoryCode = request.CategoryCode.Trim().ToUpperInvariant(),
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            DeepLink = request.DeepLink?.Trim(),
            Status = "Unread",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var id = await _notificationRepository.CreateNotificationAsync(notification, ct);
        notification.Id = id;

        // Check quiet hours before pushing
        var nowTime = DateTime.UtcNow.TimeOfDay;
        var inQuietHours = false;
        if (pref?.QuietHoursStartUtc.HasValue == true && pref.QuietHoursEndUtc.HasValue == true)
        {
            var start = pref.QuietHoursStartUtc.Value;
            var end = pref.QuietHoursEndUtc.Value;
            if (start <= end)
            {
                inQuietHours = nowTime >= start && nowTime <= end;
            }
            else
            {
                inQuietHours = nowTime >= start || nowTime <= end;
            }
        }

        // Dispatch web push if configured and not suppressed by quiet hours
        var channels = pref?.Channels ?? "IN_APP,PUSH";
        if (channels.Contains("PUSH", StringComparison.OrdinalIgnoreCase))
        {
            if (inQuietHours)
            {
                await _notificationRepository.RecordDeliveryAttemptAsync(new NotificationDeliveryAttempt
                {
                    NotificationId = id,
                    Channel = "PUSH",
                    Status = "SkippedQuietHours",
                    ErrorMessage = "Suppressed during user quiet hours"
                }, ct);
            }
            else
            {
                var subscriptions = await _notificationRepository.GetActiveSubscriptionsAsync(userId, ct);
                if (subscriptions != null)
                {
                    foreach (var sub in subscriptions)
                    {
                        var pushResult = await _webPushService.SendWebPushAsync(
                            sub.Endpoint, sub.P256dh, sub.Auth, notification.Title, notification.Message, notification.DeepLink, ct);

                        if (pushResult.IsExpiredOrUnregistered)
                        {
                            await _notificationRepository.DeactivateSubscriptionAsync(userId, sub.Endpoint, ct);
                            _logger.LogInformation("Deactivated expired/invalid push subscription for user {UserId}: {Endpoint}", userId, sub.Endpoint);
                        }

                        await _notificationRepository.RecordDeliveryAttemptAsync(new NotificationDeliveryAttempt
                        {
                            NotificationId = id,
                            Channel = "PUSH",
                            Status = pushResult.Success ? "Success" : (pushResult.IsExpiredOrUnregistered ? "ExpiredEndpoint" : "Failed"),
                            ErrorMessage = pushResult.Success ? null : (pushResult.ErrorMessage ?? "WebPush endpoint returned error")
                        }, ct);
                    }
                }
            }
        }

        _logger.LogInformation("Notification {NotificationId} created for user {UserId}", id, userId);
        return MapToDto(notification);
    }

    public async Task<bool> MarkAsReadAsync(ulong notificationId, ulong userId, CancellationToken ct = default)
    {
        return await _notificationRepository.MarkAsReadAsync(notificationId, userId, ct);
    }

    public async Task<int> MarkAllAsReadAsync(ulong userId, CancellationToken ct = default)
    {
        return await _notificationRepository.MarkAllAsReadAsync(userId, ct);
    }

    public async Task<bool> DismissNotificationAsync(ulong notificationId, ulong userId, CancellationToken ct = default)
    {
        return await _notificationRepository.DismissNotificationAsync(notificationId, userId, ct);
    }

    public async Task<bool> RegisterPushSubscriptionAsync(ulong userId, PushSubscriptionRequest request, CancellationToken ct = default)
    {
        var validation = await _subValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.ToDictionary());
        }

        var subscription = new NotificationSubscription
        {
            UserId = userId,
            Endpoint = request.Endpoint.Trim(),
            P256dh = request.P256dh.Trim(),
            Auth = request.Auth.Trim(),
            UserAgent = request.UserAgent?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        return await _notificationRepository.UpsertSubscriptionAsync(subscription, ct);
    }

    public async Task<bool> UnsubscribePushAsync(ulong userId, string endpoint, CancellationToken ct = default)
    {
        return await _notificationRepository.DeactivateSubscriptionAsync(userId, endpoint.Trim(), ct);
    }

    public async Task<NotificationOverviewDto> GetOverviewAsync(ulong userId, CancellationToken ct = default)
    {
        var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId, ct);
        var recent = await _notificationRepository.GetNotificationsAsync(userId, limit: 10, offset: 0, status: null, ct);

        return new NotificationOverviewDto
        {
            UnreadCount = unreadCount,
            RecentNotifications = recent.Select(MapToDto).ToList().AsReadOnly()
        };
    }

    public async Task<int> TriggerSmartNotificationsAsync(ulong userId, DateOnly userLocalDate, TimeSpan userLocalTime, CancellationToken ct = default)
    {
        var metrics = await _notificationRepository.GetSmartTriggerMetricsAsync(userId, userLocalDate, ct);
        var triggeredCount = 0;

        // 1. Due Revisions Alert
        if (metrics.DueRevisionsCount > 0)
        {
            var alreadyNotified = await _notificationRepository.GetDailyAlertCountAsync(userId, "REVISION_DUE", userLocalDate, ct);
            if (alreadyNotified == 0)
            {
                await SendNotificationAsync(userId, new CreateNotificationRequest
                {
                    CategoryCode = "REVISION_DUE",
                    Title = $"{metrics.DueRevisionsCount} CAT Topic{(metrics.DueRevisionsCount > 1 ? "s" : "")} Due for Revision",
                    Message = $"Stay ahead of the forgetting curve! You have {metrics.DueRevisionsCount} topic{(metrics.DueRevisionsCount > 1 ? "s" : "")} scheduled for spaced revision today.",
                    DeepLink = "/goals"
                }, ct);
                triggeredCount++;
            }
        }

        // 2. Morning Intention Reminder (e.g. between 06:00 and 11:00)
        if (userLocalTime >= new TimeSpan(6, 0, 0) && userLocalTime <= new TimeSpan(11, 0, 0))
        {
            if (!metrics.MorningReflectionLogged)
            {
                var alreadyAlerted = await _notificationRepository.GetDailyAlertCountAsync(userId, "MORNING_REFLECTION", userLocalDate, ct);
                if (alreadyAlerted == 0)
                {
                    await SendNotificationAsync(userId, new CreateNotificationRequest
                    {
                        CategoryCode = "MORNING_REFLECTION",
                        Title = "Set Your Daily Intention",
                        Message = "Start your day with clarity! Set your top 3 CAT tasks and mindset intention for today.",
                        DeepLink = "/reflections/morning"
                    }, ct);
                    triggeredCount++;
                }
            }
        }

        // 3. Evening Reflection Reminder (e.g. after 19:00)
        if (userLocalTime >= new TimeSpan(19, 0, 0))
        {
            if (!metrics.EveningReflectionLogged)
            {
                var alreadyAlerted = await _notificationRepository.GetDailyAlertCountAsync(userId, "EVENING_REFLECTION", userLocalDate, ct);
                if (alreadyAlerted == 0)
                {
                    await SendNotificationAsync(userId, new CreateNotificationRequest
                    {
                        CategoryCode = "EVENING_REFLECTION",
                        Title = "Evening Reflection & Gratitude",
                        Message = "Take 3 minutes to unwind, review your study breakthroughs, and log what went well today.",
                        DeepLink = "/reflections/evening"
                    }, ct);
                    triggeredCount++;
                }
            }
        }

        // 4. Incomplete Habit Reminder (e.g. after 18:00)
        if (userLocalTime >= new TimeSpan(18, 0, 0))
        {
            if (metrics.PendingHabitsCount > 0)
            {
                var alreadyAlerted = await _notificationRepository.GetDailyAlertCountAsync(userId, "HABIT_REMINDER", userLocalDate, ct);
                if (alreadyAlerted == 0)
                {
                    await SendNotificationAsync(userId, new CreateNotificationRequest
                    {
                        CategoryCode = "HABIT_REMINDER",
                        Title = $"{metrics.PendingHabitsCount} Habit{(metrics.PendingHabitsCount > 1 ? "s" : "")} Awaiting Check-in",
                        Message = $"Keep your streak alive! You have {metrics.PendingHabitsCount} daily habit{(metrics.PendingHabitsCount > 1 ? "s" : "")} remaining today.",
                        DeepLink = "/habits"
                    }, ct);
                    triggeredCount++;
                }
            }
        }

        return triggeredCount;
    }

    private static UserNotificationDto MapToDto(UserNotification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        CategoryCode = n.CategoryCode,
        Title = n.Title,
        Message = n.Message,
        DeepLink = n.DeepLink,
        Status = n.Status,
        ReadAtUtc = n.ReadAtUtc,
        CreatedAtUtc = n.CreatedAtUtc,
        UpdatedAtUtc = n.UpdatedAtUtc
    };
}
