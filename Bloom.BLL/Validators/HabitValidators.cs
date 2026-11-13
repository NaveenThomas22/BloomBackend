using Bloom.BLL.DTOs;
using FluentValidation;

namespace Bloom.BLL.Validators;

public class CreateHabitRequestValidator : AbstractValidator<CreateHabitRequest>
{
    private static readonly HashSet<string> ValidFrequencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "Daily", "Weekdays", "Weekends", "CustomDays"
    };

    public CreateHabitRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0ul).WithMessage("A valid habit category must be selected.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Habit name is required.")
            .MaximumLength(150).WithMessage("Habit name must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.Frequency)
            .NotEmpty().WithMessage("Frequency is required.")
            .Must(f => ValidFrequencies.Contains(f))
            .WithMessage("Frequency must be one of: Daily, Weekdays, Weekends, CustomDays.");

        RuleFor(x => x.TargetFrequencyDays)
            .InclusiveBetween(1u, 7u).WithMessage("Target frequency days must be between 1 and 7.");

        RuleFor(x => x.TargetValue)
            .GreaterThan(0).WithMessage("Target value must be greater than 0.");

        RuleFor(x => x.TargetUnit)
            .NotEmpty().WithMessage("Target unit is required.")
            .MaximumLength(50).WithMessage("Target unit must not exceed 50 characters.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be on or after start date.");
    }
}

public class UpdateHabitRequestValidator : AbstractValidator<UpdateHabitRequest>
{
    private static readonly HashSet<string> ValidFrequencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "Daily", "Weekdays", "Weekends", "CustomDays"
    };

    public UpdateHabitRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0ul).WithMessage("A valid habit category must be selected.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Habit name is required.")
            .MaximumLength(150).WithMessage("Habit name must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.Frequency)
            .NotEmpty().WithMessage("Frequency is required.")
            .Must(f => ValidFrequencies.Contains(f))
            .WithMessage("Frequency must be one of: Daily, Weekdays, Weekends, CustomDays.");

        RuleFor(x => x.TargetFrequencyDays)
            .InclusiveBetween(1u, 7u).WithMessage("Target frequency days must be between 1 and 7.");

        RuleFor(x => x.TargetValue)
            .GreaterThan(0).WithMessage("Target value must be greater than 0.");

        RuleFor(x => x.TargetUnit)
            .NotEmpty().WithMessage("Target unit is required.")
            .MaximumLength(50).WithMessage("Target unit must not exceed 50 characters.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be on or after start date.");
    }
}

public class RecordHabitCompletionRequestValidator : AbstractValidator<RecordHabitCompletionRequest>
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Completed", "Partial", "Skipped"
    };

    public RecordHabitCompletionRequestValidator()
    {
        RuleFor(x => x.CompletionDate)
            .NotEmpty().WithMessage("Completion date is required.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Status must be Completed, Partial, or Skipped.");

        RuleFor(x => x.ActualValue)
            .GreaterThanOrEqualTo(0).WithMessage("Actual value cannot be negative.");

        RuleFor(x => x.SkipReason)
            .MaximumLength(255).WithMessage("Skip reason must not exceed 255 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.");
    }
}

public class CreateMilestoneRequestValidator : AbstractValidator<CreateMilestoneRequest>
{
    public CreateMilestoneRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.TargetValue)
            .GreaterThan(0).WithMessage("Target value must be greater than 0.");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit is required.")
            .MaximumLength(50).WithMessage("Unit must not exceed 50 characters.");
    }
}

public class UpdateMilestoneRequestValidator : AbstractValidator<UpdateMilestoneRequest>
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "InProgress", "Achieved", "Abandoned"
    };

    public UpdateMilestoneRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.TargetValue)
            .GreaterThan(0).WithMessage("Target value must be greater than 0.");

        RuleFor(x => x.CurrentValue)
            .GreaterThanOrEqualTo(0).WithMessage("Current value cannot be negative.");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit is required.")
            .MaximumLength(50).WithMessage("Unit must not exceed 50 characters.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Status must be InProgress, Achieved, or Abandoned.");
    }
}

