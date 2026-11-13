using Bloom.BLL.DTOs;
using FluentValidation;

namespace Bloom.BLL.Validators;

public class CreateNotificationRequestValidator : AbstractValidator<CreateNotificationRequest>
{
    public CreateNotificationRequestValidator()
    {
        RuleFor(x => x.CategoryCode)
            .NotEmpty().WithMessage("Category code is required.")
            .MaximumLength(50).WithMessage("Category code must not exceed 50 characters.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(1000).WithMessage("Message must not exceed 1000 characters.");

        RuleFor(x => x.DeepLink)
            .MaximumLength(255).WithMessage("Deep link must not exceed 255 characters.");
    }
}

public class UpdateNotificationPreferenceRequestValidator : AbstractValidator<UpdateNotificationPreferenceRequest>
{
    public UpdateNotificationPreferenceRequestValidator()
    {
        RuleFor(x => x.Channels)
            .NotEmpty().WithMessage("Channels cannot be empty.")
            .MaximumLength(50).WithMessage("Channels must not exceed 50 characters.");

        RuleFor(x => x.MaxDailyAlerts)
            .InclusiveBetween(1u, 50u).WithMessage("Max daily alerts must be between 1 and 50.");
    }
}

public class PushSubscriptionRequestValidator : AbstractValidator<PushSubscriptionRequest>
{
    public PushSubscriptionRequestValidator()
    {
        RuleFor(x => x.Endpoint)
            .NotEmpty().WithMessage("Endpoint is required.")
            .MaximumLength(1000).WithMessage("Endpoint must not exceed 1000 characters.");

        RuleFor(x => x.P256dh)
            .NotEmpty().WithMessage("P256dh key is required.")
            .MaximumLength(255).WithMessage("P256dh key must not exceed 255 characters.");

        RuleFor(x => x.Auth)
            .NotEmpty().WithMessage("Auth secret is required.")
            .MaximumLength(255).WithMessage("Auth secret must not exceed 255 characters.");

        RuleFor(x => x.UserAgent)
            .MaximumLength(255).WithMessage("UserAgent must not exceed 255 characters.");
    }
}

