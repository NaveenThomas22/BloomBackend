using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Bloom.Core.Exceptions.ValidationException;

namespace Bloom.BLL.Services;

public class RevisionPolicyService : IRevisionPolicyService
{
    private readonly IRevisionPolicyRepository _policyRepo;
    private readonly IValidator<CreateRevisionPolicyDto> _createValidator;
    private readonly IValidator<UpdateRevisionPolicyDto> _updateValidator;
    private readonly ILogger<RevisionPolicyService> _logger;

    public RevisionPolicyService(
        IRevisionPolicyRepository policyRepo,
        IValidator<CreateRevisionPolicyDto> createValidator,
        IValidator<UpdateRevisionPolicyDto> updateValidator,
        ILogger<RevisionPolicyService> logger)
    {
        _policyRepo = policyRepo ?? throw new ArgumentNullException(nameof(policyRepo));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<RevisionPolicyDto>> GetPoliciesAsync(ulong userId, CancellationToken ct = default)
    {
        var policies = await _policyRepo.GetPoliciesAsync(userId, includeInactive: false, ct);
        return policies.Select(MapToDto).ToList();
    }

    public async Task<RevisionPolicyDto> GetPolicyByIdAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        var policy = await _policyRepo.GetPolicyByIdAsync(id, userId, ct);
        if (policy == null)
        {
            throw new NotFoundException($"Revision policy with ID {id} not found.");
        }
        return MapToDto(policy);
    }

    public async Task<RevisionPolicyDto> CreatePolicyAsync(ulong userId, CreateRevisionPolicyDto dto, CancellationToken ct = default)
    {
        var valResult = await _createValidator.ValidateAsync(dto, ct);
        if (!valResult.IsValid)
        {
            var errors = valResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var policy = new RevisionPolicy
        {
            UserId = userId,
            PolicyName = dto.PolicyName.Trim(),
            Description = dto.Description?.Trim(),
            IntervalsCsv = dto.IntervalsCsv.Trim(),
            IsSystem = false,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var created = await _policyRepo.CreatePolicyAsync(policy, ct);
        _logger.LogInformation("User {UserId} created custom revision policy {PolicyId} ({PolicyName})", userId, created.Id, created.PolicyName);
        return MapToDto(created);
    }

    public async Task<RevisionPolicyDto> UpdatePolicyAsync(ulong id, ulong userId, UpdateRevisionPolicyDto dto, CancellationToken ct = default)
    {
        var valResult = await _updateValidator.ValidateAsync(dto, ct);
        if (!valResult.IsValid)
        {
            var errors = valResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var existing = await _policyRepo.GetPolicyByIdAsync(id, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Revision policy with ID {id} not found.");
        }

        if (existing.IsSystem)
        {
            throw new ForbiddenException("System revision policies cannot be modified.");
        }

        if (existing.UserId != userId)
        {
            throw new ForbiddenException("You cannot modify another user's revision policy.");
        }

        existing.PolicyName = dto.PolicyName.Trim();
        existing.Description = dto.Description?.Trim();
        existing.IntervalsCsv = dto.IntervalsCsv.Trim();
        existing.IsActive = dto.IsActive;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await _policyRepo.UpdatePolicyAsync(existing, ct);
        _logger.LogInformation("User {UserId} updated custom revision policy {PolicyId}", userId, id);
        return MapToDto(existing);
    }

    public async Task DeletePolicyAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        var existing = await _policyRepo.GetPolicyByIdAsync(id, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Revision policy with ID {id} not found.");
        }

        if (existing.IsSystem)
        {
            throw new ForbiddenException("System revision policies cannot be deleted.");
        }

        if (existing.UserId != userId)
        {
            throw new ForbiddenException("You cannot delete another user's revision policy.");
        }

        await _policyRepo.DeletePolicyAsync(id, userId, ct);
        _logger.LogInformation("User {UserId} deleted custom revision policy {PolicyId}", userId, id);
    }

    private static RevisionPolicyDto MapToDto(RevisionPolicy entity)
    {
        return new RevisionPolicyDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            PolicyName = entity.PolicyName,
            Description = entity.Description,
            IntervalsCsv = entity.IntervalsCsv,
            Intervals = entity.GetIntervals(),
            IsSystem = entity.IsSystem,
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }
}

