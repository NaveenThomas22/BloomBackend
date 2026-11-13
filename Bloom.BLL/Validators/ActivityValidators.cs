using Bloom.BLL.DTOs;
using FluentValidation;

namespace Bloom.BLL.Validators;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.CategoryCode)
            .NotEmpty().WithMessage("Category code is required.")
            .MaximumLength(50).WithMessage("Category code cannot exceed 50 characters.")
            .Matches("^[A-Z0-9_]+$").WithMessage("Category code must consist of uppercase alphanumeric characters and underscores.");

        RuleFor(x => x.CategoryName)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color hex code is required.")
            .Matches("^#(?:[0-9a-fA-F]{3}){1,2}$").WithMessage("Color must be a valid hex code (e.g. #A8BDA5).");

        RuleFor(x => x.CategoryGroup)
            .NotEmpty().WithMessage("Category group is required.")
            .Must(g => new[] { "Study", "RestAndSleep", "MealsAndBreaks", "Personal", "WorkAndCommute", "HealthAndFitness", "LeisureAndSocial", "Other" }.Contains(g))
            .WithMessage("Category group must be a valid activity group.");
    }
}

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.CategoryName)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color hex code is required.")
            .Matches("^#(?:[0-9a-fA-F]{3}){1,2}$").WithMessage("Color must be a valid hex code.");

        RuleFor(x => x.CategoryGroup)
            .NotEmpty().WithMessage("Category group is required.")
            .Must(g => new[] { "Study", "RestAndSleep", "MealsAndBreaks", "Personal", "WorkAndCommute", "HealthAndFitness", "LeisureAndSocial", "Other" }.Contains(g))
            .WithMessage("Category group must be a valid activity group.");
    }
}

public class CreateActivityRecordValidator : AbstractValidator<CreateActivityRecordDto>
{
    public CreateActivityRecordValidator()
    {
        RuleFor(x => x.ActivityCategoryId)
            .GreaterThan(0ul).WithMessage("Valid activity category is required.");

        RuleFor(x => x.ActivityTitle)
            .NotEmpty().WithMessage("Activity title is required.")
            .MaximumLength(255).WithMessage("Activity title cannot exceed 255 characters.");

        RuleFor(x => x.StartAtUtc)
            .NotEmpty().WithMessage("Start timestamp is required.");

        When(x => x.EndAtUtc.HasValue, () =>
        {
            RuleFor(x => x.EndAtUtc!.Value)
                .GreaterThan(x => x.StartAtUtc).WithMessage("End time must be after start time.")
                .Must((dto, end) => (end - dto.StartAtUtc).TotalHours <= 24)
                .WithMessage("A single continuous activity interval cannot exceed 24 hours.");
        });
    }
}

public class UpdateActivityRecordValidator : AbstractValidator<UpdateActivityRecordDto>
{
    public UpdateActivityRecordValidator()
    {
        RuleFor(x => x.ActivityCategoryId)
            .GreaterThan(0ul).WithMessage("Valid activity category is required.");

        RuleFor(x => x.ActivityTitle)
            .NotEmpty().WithMessage("Activity title is required.")
            .MaximumLength(255).WithMessage("Activity title cannot exceed 255 characters.");

        RuleFor(x => x.EndAtUtc)
            .GreaterThan(x => x.StartAtUtc).WithMessage("End time must be after start time.")
            .Must((dto, end) => (end - dto.StartAtUtc).TotalHours <= 24)
            .WithMessage("A single continuous activity interval cannot exceed 24 hours.");
    }
}

public class CreatePlannedActivityValidator : AbstractValidator<CreatePlannedActivityDto>
{
    public CreatePlannedActivityValidator()
    {
        RuleFor(x => x.ActivityCategoryId)
            .GreaterThan(0ul).WithMessage("Valid activity category is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(255).WithMessage("Title cannot exceed 255 characters.");

        RuleFor(x => x.PlannedEndAtUtc)
            .GreaterThan(x => x.PlannedStartAtUtc).WithMessage("Planned end time must be after planned start time.");
    }
}

public class UpdatePlannedActivityValidator : AbstractValidator<UpdatePlannedActivityDto>
{
    public UpdatePlannedActivityValidator()
    {
        RuleFor(x => x.ActivityCategoryId)
            .GreaterThan(0ul).WithMessage("Valid activity category is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(255).WithMessage("Title cannot exceed 255 characters.");

        RuleFor(x => x.PlannedEndAtUtc)
            .GreaterThan(x => x.PlannedStartAtUtc).WithMessage("Planned end time must be after planned start time.");
    }
}

public class StartTimerValidator : AbstractValidator<StartTimerDto>
{
    public StartTimerValidator()
    {
        RuleFor(x => x.ActivityCategoryId)
            .GreaterThan(0ul).WithMessage("Valid activity category is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(255).WithMessage("Title cannot exceed 255 characters.");
    }
}

