using Bloom.BLL.DTOs;
using Bloom.Core.Enums;
using FluentValidation;

namespace Bloom.BLL.Validators;

public class CreateGoalDtoValidator : AbstractValidator<CreateGoalDto>
{
    private static readonly string[] ValidPeriods = Enum.GetNames<GoalPeriod>();
    private static readonly string[] ValidMetrics = Enum.GetNames<TargetMetric>();

    public CreateGoalDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Goal title is required.")
            .MaximumLength(200).WithMessage("Goal title must not exceed 200 characters.");

        RuleFor(x => x.GoalPeriod)
            .NotEmpty().WithMessage("Goal period is required.")
            .Must(p => ValidPeriods.Contains(p, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Goal period must be one of: {string.Join(", ", ValidPeriods)}.");

        RuleFor(x => x.TargetMetric)
            .NotEmpty().WithMessage("Target metric is required.")
            .Must(m => ValidMetrics.Contains(m, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Target metric must be one of: {string.Join(", ", ValidMetrics)}.");

        RuleFor(x => x.TargetValue)
            .GreaterThan(0).WithMessage("Target value must be greater than zero.");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit is required.")
            .MaximumLength(50).WithMessage("Unit must not exceed 50 characters.");

        RuleFor(x => x)
            .Must(x => x.StartDate <= x.EndDate)
            .WithMessage("Start date must be on or before end date.");
    }
}

public class UpdateGoalDtoValidator : AbstractValidator<UpdateGoalDto>
{
    private static readonly string[] ValidStatuses = Enum.GetNames<GoalStatus>();

    public UpdateGoalDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Goal title is required.")
            .MaximumLength(200).WithMessage("Goal title must not exceed 200 characters.");

        RuleFor(x => x.TargetValue)
            .GreaterThan(0).WithMessage("Target value must be greater than zero.");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit is required.")
            .MaximumLength(50).WithMessage("Unit must not exceed 50 characters.");

        RuleFor(x => x)
            .Must(x => x.StartDate <= x.EndDate)
            .WithMessage("Start date must be on or before end date.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => ValidStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}

public class CreateMilestoneDtoValidator : AbstractValidator<CreateMilestoneDto>
{
    private static readonly string[] ValidPriorities = Enum.GetNames<MilestonePriority>();

    public CreateMilestoneDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Milestone title is required.")
            .MaximumLength(200).WithMessage("Milestone title must not exceed 200 characters.");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Priority is required.")
            .Must(p => ValidPriorities.Contains(p, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Priority must be one of: {string.Join(", ", ValidPriorities)}.");
    }
}

public class UpdateMilestoneDtoValidator : AbstractValidator<UpdateMilestoneDto>
{
    private static readonly string[] ValidPriorities = Enum.GetNames<MilestonePriority>();
    private static readonly string[] ValidStatuses = Enum.GetNames<MilestoneStatus>();

    public UpdateMilestoneDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Milestone title is required.")
            .MaximumLength(200).WithMessage("Milestone title must not exceed 200 characters.");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Priority is required.")
            .Must(p => ValidPriorities.Contains(p, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Priority must be one of: {string.Join(", ", ValidPriorities)}.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => ValidStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}

public class CreateRevisionPolicyDtoValidator : AbstractValidator<CreateRevisionPolicyDto>
{
    public CreateRevisionPolicyDtoValidator()
    {
        RuleFor(x => x.PolicyName)
            .NotEmpty().WithMessage("Policy name is required.")
            .MaximumLength(100).WithMessage("Policy name must not exceed 100 characters.");

        RuleFor(x => x.IntervalsCsv)
            .NotEmpty().WithMessage("Intervals are required.")
            .Must(ValidateIntervals)
            .WithMessage("Intervals must be a comma-separated list of positive integers (e.g., '1,3,7,14,30').");
    }

    private static bool ValidateIntervals(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return false;
        var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;
        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var val) || val <= 0) return false;
        }
        return true;
    }
}

public class UpdateRevisionPolicyDtoValidator : AbstractValidator<UpdateRevisionPolicyDto>
{
    public UpdateRevisionPolicyDtoValidator()
    {
        RuleFor(x => x.PolicyName)
            .NotEmpty().WithMessage("Policy name is required.")
            .MaximumLength(100).WithMessage("Policy name must not exceed 100 characters.");

        RuleFor(x => x.IntervalsCsv)
            .NotEmpty().WithMessage("Intervals are required.")
            .Must(ValidateIntervals)
            .WithMessage("Intervals must be a comma-separated list of positive integers (e.g., '1,3,7,14,30').");
    }

    private static bool ValidateIntervals(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return false;
        var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;
        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var val) || val <= 0) return false;
        }
        return true;
    }
}

public class RecordRevisionAttemptRequestValidator : AbstractValidator<RecordRevisionAttemptRequest>
{
    private static readonly string[] ValidUnderstanding = Enum.GetNames<UnderstandingLevel>();

    public RecordRevisionAttemptRequestValidator()
    {
        RuleFor(x => x.UnderstandingLevel)
            .NotEmpty().WithMessage("Understanding level is required.")
            .Must(u => ValidUnderstanding.Contains(u, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Understanding level must be one of: {string.Join(", ", ValidUnderstanding)}.");

        RuleFor(x => x.ConfidenceRating)
            .InclusiveBetween(1, 5).WithMessage("Confidence rating must be between 1 and 5.");

        When(x => x.MinutesSpent.HasValue, () =>
        {
            RuleFor(x => x.MinutesSpent!.Value)
                .GreaterThan(0).WithMessage("Minutes spent must be greater than zero.")
                .LessThanOrEqualTo(1440).WithMessage("Minutes spent cannot exceed 1440 (24 hours).");
        });
    }
}

public class RescheduleRevisionRequestValidator : AbstractValidator<RescheduleRevisionRequest>
{
    public RescheduleRevisionRequestValidator()
    {
        RuleFor(x => x.NewDate)
            .NotEmpty().WithMessage("New date is required.");
    }
}

