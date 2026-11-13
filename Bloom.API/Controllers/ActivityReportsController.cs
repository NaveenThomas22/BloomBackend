using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/activity-reports")]
public class ActivityReportsController : BaseApiController
{
    private readonly IActivityReportService _reportService;
    private readonly ICurrentUserService _currentUserService;

    public ActivityReportsController(
        IActivityReportService reportService,
        ICurrentUserService currentUserService)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet("daily")]
    [ProducesResponseType(typeof(ApiResponse<DailyTimeSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DailyTimeSummaryDto>>> GetDailyReport(
        [FromQuery] DateOnly? targetDate = null,
        [FromQuery] string? timezoneId = null,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var date = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await _reportService.GetDailyReportAsync(userId, date, timezoneId, ct);
        return Success(report, "Daily time accounting report retrieved successfully.");
    }

    [HttpGet("weekly")]
    [ProducesResponseType(typeof(ApiResponse<WeeklyReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<WeeklyReportDto>>> GetWeeklyReport(
        [FromQuery] DateOnly? referenceDate = null,
        [FromQuery] string? timezoneId = null,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var date = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await _reportService.GetWeeklyReportAsync(userId, date, timezoneId, ct);
        return Success(report, "Weekly activity report retrieved successfully.");
    }

    [HttpGet("monthly")]
    [ProducesResponseType(typeof(ApiResponse<MonthlyReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MonthlyReportDto>>> GetMonthlyReport(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] string? timezoneId = null,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var now = DateTime.UtcNow;
        int y = year ?? now.Year;
        int m = month ?? now.Month;
        var report = await _reportService.GetMonthlyReportAsync(userId, y, m, timezoneId, ct);
        return Success(report, "Monthly activity report retrieved successfully.");
    }
}

