using Bloom.BLL.DTOs;
using FluentValidation;

namespace Bloom.BLL.Validators;

public class AnalyticsFilterRequestValidator : AbstractValidator<AnalyticsFilterRequest>
{
    private static readonly HashSet<string> ValidTimeRanges = new(StringComparer.OrdinalIgnoreCase)
    {
        "Today",
        "Last7Days",
        "Last30Days",
        "ThisMonth",
        "LastMonth",
        "Custom",
        "AllTime"
    };

    public AnalyticsFilterRequestValidator()
    {
        RuleFor(x => x.TimeRange)
            .NotEmpty().WithMessage("TimeRange is required.")
            .Must(tr => ValidTimeRanges.Contains(tr))
            .WithMessage("TimeRange must be one of: Today, Last7Days, Last30Days, ThisMonth, LastMonth, Custom, AllTime.");

        When(x => string.Equals(x.TimeRange, "Custom", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.StartDate)
                .NotNull().WithMessage("StartDate is required when TimeRange is Custom.");

            RuleFor(x => x.EndDate)
                .NotNull().WithMessage("EndDate is required when TimeRange is Custom.");

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate!.Value)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("EndDate must be greater than or equal to StartDate.");
        });

        RuleFor(x => x.TimezoneId)
            .MaximumLength(100).WithMessage("TimezoneId must not exceed 100 characters.");
    }
}
