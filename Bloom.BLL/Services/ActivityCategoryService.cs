using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = Bloom.Core.Exceptions.ValidationException;

namespace Bloom.BLL.Services;

public class ActivityCategoryService : IActivityCategoryService
{
    private readonly IActivityCategoryRepository _categoryRepo;
    private readonly IValidator<CreateCategoryDto> _createValidator;
    private readonly IValidator<UpdateCategoryDto> _updateValidator;
    private readonly ILogger<ActivityCategoryService> _logger;

    public ActivityCategoryService(
        IActivityCategoryRepository categoryRepo,
        IValidator<CreateCategoryDto> createValidator,
        IValidator<UpdateCategoryDto> updateValidator,
        ILogger<ActivityCategoryService> logger)
    {
        _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<ActivityCategoryDto>> GetCategoriesAsync(ulong userId, bool includeInactive = false, CancellationToken ct = default)
    {
        var categories = await _categoryRepo.GetCategoriesAsync(userId, includeInactive, ct);
        return categories.Select(MapToDto).ToList();
    }

    public async Task<ActivityCategoryDto> GetCategoryByIdAsync(ulong userId, ulong categoryId, CancellationToken ct = default)
    {
        var category = await _categoryRepo.GetCategoryByIdAsync(categoryId, userId, ct);
        if (category == null)
        {
            throw new NotFoundException($"Activity category with ID {categoryId} not found.");
        }
        return MapToDto(category);
    }

    public async Task<ActivityCategoryDto> CreateCustomCategoryAsync(ulong userId, CreateCategoryDto dto, CancellationToken ct = default)
    {
        var valResult = await _createValidator.ValidateAsync(dto, ct);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var code = dto.CategoryCode.Trim().ToUpperInvariant();
        var existing = await _categoryRepo.GetCategoryByCodeAsync(code, userId, ct);
        if (existing != null && existing.UserId == userId)
        {
            throw new ConflictException($"Category code '{code}' already exists for your account.");
        }

        var category = new ActivityCategory
        {
            UserId = userId,
            CategoryCode = code,
            CategoryName = dto.CategoryName.Trim(),
            Description = dto.Description?.Trim(),
            Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "activity" : dto.Icon.Trim(),
            Color = dto.Color.Trim(),
            CategoryGroup = dto.CategoryGroup.Trim(),
            IsActive = true,
            DisplayOrder = dto.DisplayOrder
        };

        var created = await _categoryRepo.CreateCategoryAsync(category, ct);
        _logger.LogInformation("User {UserId} created custom activity category {CategoryId} ({Code}).", userId, created.Id, created.CategoryCode);

        return MapToDto(created);
    }

    public async Task<ActivityCategoryDto> UpdateCategoryAsync(ulong userId, ulong categoryId, UpdateCategoryDto dto, CancellationToken ct = default)
    {
        var valResult = await _updateValidator.ValidateAsync(dto, ct);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var existing = await _categoryRepo.GetCategoryByIdAsync(categoryId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Activity category with ID {categoryId} not found.");
        }

        if (!existing.UserId.HasValue || existing.UserId.Value != userId)
        {
            throw new ForbiddenException("Cannot modify shared system activity categories.");
        }

        existing.CategoryName = dto.CategoryName.Trim();
        existing.Description = dto.Description?.Trim();
        existing.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? existing.Icon : dto.Icon.Trim();
        existing.Color = dto.Color.Trim();
        existing.CategoryGroup = dto.CategoryGroup.Trim();
        existing.IsActive = dto.IsActive;
        existing.DisplayOrder = dto.DisplayOrder;

        await _categoryRepo.UpdateCategoryAsync(existing, ct);
        return MapToDto(existing);
    }

    public async Task<bool> DeleteCategoryAsync(ulong userId, ulong categoryId, CancellationToken ct = default)
    {
        var existing = await _categoryRepo.GetCategoryByIdAsync(categoryId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Activity category with ID {categoryId} not found.");
        }

        if (!existing.UserId.HasValue || existing.UserId.Value != userId)
        {
            throw new ForbiddenException("Cannot delete shared system activity categories.");
        }

        var isUsed = await _categoryRepo.IsCategoryUsedAsync(categoryId, userId, ct);
        if (isUsed)
        {
            throw new ConflictException("Cannot delete category because it is referenced by existing activity records. Consider deactivating it instead.");
        }

        return await _categoryRepo.DeleteCategoryAsync(categoryId, userId, ct);
    }

    private static ActivityCategoryDto MapToDto(ActivityCategory c)
    {
        return new ActivityCategoryDto
        {
            Id = c.Id,
            UserId = c.UserId,
            CategoryCode = c.CategoryCode,
            CategoryName = c.CategoryName,
            Description = c.Description,
            Icon = c.Icon,
            Color = c.Color,
            CategoryGroup = c.CategoryGroup,
            IsActive = c.IsActive,
            DisplayOrder = c.DisplayOrder,
            IsCustom = c.UserId.HasValue
        };
    }
}
