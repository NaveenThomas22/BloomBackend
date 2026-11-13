using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/journal")]
public class JournalController : BaseApiController
{
    private readonly IJournalService _journalService;
    private readonly ICurrentUserService _currentUserService;

    public JournalController(
        IJournalService journalService,
        ICurrentUserService currentUserService)
    {
        _journalService = journalService ?? throw new ArgumentNullException(nameof(journalService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedJournalResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedJournalResultDto>>> QueryEntries(
        [FromQuery] JournalQueryParameters parameters,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _journalService.QueryEntriesAsync(userId, parameters, ct);
        return Success(result, "Journal entries retrieved successfully.");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<JournalEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<JournalEntryDto>>> GetById(
        [FromRoute] ulong id,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var entry = await _journalService.GetEntryByIdAsync(userId, id, ct);
        return Success(entry, "Journal entry retrieved successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<JournalEntryDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<JournalEntryDto>>> CreateEntry(
        [FromBody] CreateJournalEntryRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var entry = await _journalService.CreateEntryAsync(userId, request, ct);
        return CreatedSuccess(entry, "Journal entry created successfully.");
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<JournalEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<JournalEntryDto>>> UpdateEntry(
        [FromRoute] ulong id,
        [FromBody] UpdateJournalEntryRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var entry = await _journalService.UpdateEntryAsync(userId, id, request, ct);
        return Success(entry, "Journal entry updated successfully.");
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteEntry(
        [FromRoute] ulong id,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await _journalService.DeleteEntryAsync(userId, id, ct);
        return Success("Journal entry deleted successfully.");
    }

    [HttpGet("calendar")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DateOnly>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DateOnly>>>> GetCalendarDates(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var dates = await _journalService.GetCalendarDatesWithEntriesAsync(userId, year, month, ct);
        return Success(dates, "Journal entry dates retrieved successfully.");
    }
}

