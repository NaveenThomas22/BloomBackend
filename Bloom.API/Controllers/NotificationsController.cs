using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/notifications")]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public NotificationsController(
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserNotificationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserNotificationDto>>>> GetNotifications(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var notifications = await _notificationService.GetNotificationsAsync(userId, limit, offset, status, ct);
        return Success(notifications, "Notifications retrieved successfully.");
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<NotificationOverviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<NotificationOverviewDto>>> GetOverview(CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var overview = await _notificationService.GetOverviewAsync(userId, ct);
        return Success(overview, "Notification overview retrieved successfully.");
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationCategoryDto>>>> GetCategories(CancellationToken ct)
    {
        var categories = await _notificationService.GetCategoriesAsync(ct);
        return Success(categories, "Notification categories retrieved successfully.");
    }

    [HttpGet("preferences")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserNotificationPreferenceDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserNotificationPreferenceDto>>>> GetPreferences(CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var preferences = await _notificationService.GetPreferencesAsync(userId, ct);
        return Success(preferences, "Notification preferences retrieved successfully.");
    }

    [HttpPut("preferences/{categoryCode}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdatePreference(
        [FromRoute] string categoryCode,
        [FromBody] UpdateNotificationPreferenceRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var success = await _notificationService.UpdatePreferenceAsync(userId, categoryCode, request, ct);
        return Success(success, "Notification preference updated successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserNotificationDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<UserNotificationDto>>> CreateNotification(
        [FromBody] CreateNotificationRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var notification = await _notificationService.SendNotificationAsync(userId, request, ct);
        return CreatedSuccess(notification, "Notification created successfully.");
    }

    [HttpPut("{id:long}/read")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(
        [FromRoute] ulong id,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var success = await _notificationService.MarkAsReadAsync(id, userId, ct);
        return Success(success, "Notification marked as read.");
    }

    [HttpPut("read-all")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> MarkAllAsRead(CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var count = await _notificationService.MarkAllAsReadAsync(userId, ct);
        return Success(count, $"{count} notifications marked as read.");
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> DismissNotification(
        [FromRoute] ulong id,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var success = await _notificationService.DismissNotificationAsync(id, userId, ct);
        return Success(success, "Notification dismissed.");
    }

    [HttpPost("push-subscribe")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> RegisterPushSubscription(
        [FromBody] PushSubscriptionRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var success = await _notificationService.RegisterPushSubscriptionAsync(userId, request, ct);
        return Success(success, "Push subscription registered successfully.");
    }

    [HttpPost("push-unsubscribe")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> UnsubscribePush(
        [FromBody] string endpoint,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var success = await _notificationService.UnsubscribePushAsync(userId, endpoint, ct);
        return Success(success, "Push subscription deactivated.");
    }

    [HttpPost("trigger")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> TriggerNotifications(
        [FromQuery] DateOnly? date,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var localDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var localTime = DateTime.UtcNow.TimeOfDay;
        var count = await _notificationService.TriggerSmartNotificationsAsync(userId, localDate, localTime, ct);
        return Success(count, $"{count} smart notifications triggered.");
    }
}
