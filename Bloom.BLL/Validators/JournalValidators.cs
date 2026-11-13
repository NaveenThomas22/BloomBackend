using System.Text.RegularExpressions;
using Bloom.BLL.DTOs;
using FluentValidation;

namespace Bloom.BLL.Validators;

public class CreateJournalCategoryRequestValidator : AbstractValidator<CreateJournalCategoryRequest>
{
    private static readonly Regex HexColorRegex = new(@"^#(?:[0-9a-fA-F]{3}){1,2}$", RegexOptions.Compiled);
    private static readonly Regex CodeRegex = new(@"^[A-Z0-9_]{2,50}$", RegexOptions.Compiled);

    public CreateJournalCategoryRequestValidator()
    {
        RuleFor(x => x.CategoryCode)
            .NotEmpty().WithMessage("Category code is required.")
            .MaximumLength(50).WithMessage("Category code cannot exceed 50 characters.")
            .Matches(CodeRegex).WithMessage("Category code must be uppercase alphanumeric and underscores (2-50 characters).");

        RuleFor(x => x.CategoryName)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required.")
            .Matches(HexColorRegex).WithMessage("Color must be a valid hex color code (e.g., #A8BDA5).");
    }
}

public class CreateJournalEntryRequestValidator : AbstractValidator<CreateJournalEntryRequest>
{
    public CreateJournalEntryRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0ul).WithMessage("A valid category must be selected.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Journal entry title is required.")
            .MaximumLength(255).WithMessage("Title cannot exceed 255 characters.");

        When(x => !x.IsDraft, () =>
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Journal content is required for published entries.");
        });

        RuleFor(x => x.Content)
            .MaximumLength(500_000).WithMessage("Content cannot exceed 500,000 characters.");

        RuleFor(x => x.EntryDate)
            .NotEmpty().WithMessage("Entry date is required.");
    }
}

public class UpdateJournalEntryRequestValidator : AbstractValidator<UpdateJournalEntryRequest>
{
    public UpdateJournalEntryRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0ul).WithMessage("A valid category must be selected.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Journal entry title is required.")
            .MaximumLength(255).WithMessage("Title cannot exceed 255 characters.");

        When(x => !x.IsDraft, () =>
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Journal content is required for published entries.");
        });

        RuleFor(x => x.Content)
            .MaximumLength(500_000).WithMessage("Content cannot exceed 500,000 characters.");

        RuleFor(x => x.EntryDate)
            .NotEmpty().WithMessage("Entry date is required.");

        RuleFor(x => x.ExpectedVersion)
            .GreaterThan(0u).WithMessage("Expected version is required and must be greater than 0.");
    }
}

public class JournalQueryParametersValidator : AbstractValidator<JournalQueryParameters>
{
    public JournalQueryParametersValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}

