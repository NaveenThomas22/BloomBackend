using Bloom.BLL.DTOs;
using FluentValidation;

namespace Bloom.BLL.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(150).WithMessage("Full name cannot exceed 150 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one numerical digit.")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");

        When(x => x.CatTargetYear.HasValue, () =>
        {
            RuleFor(x => x.CatTargetYear!.Value)
                .InclusiveBetween((ushort)2024, (ushort)2035)
                .WithMessage("CAT target year must be between 2024 and 2035.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.PreparationLevel), () =>
        {
            RuleFor(x => x.PreparationLevel!)
                .MaximumLength(50).WithMessage("Preparation level cannot exceed 50 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.PreparationMode), () =>
        {
            RuleFor(x => x.PreparationMode!)
                .MaximumLength(50).WithMessage("Preparation mode cannot exceed 50 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Timezone), () =>
        {
            RuleFor(x => x.Timezone!)
                .MaximumLength(50).WithMessage("Timezone cannot exceed 50 characters.");
        });
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequestDto>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(150).WithMessage("Full name cannot exceed 150 characters.");

        RuleFor(x => x.CatTargetYear)
            .InclusiveBetween((ushort)2024, (ushort)2035)
            .WithMessage("CAT target year must be between 2024 and 2035.");

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Timezone is required.")
            .MaximumLength(50).WithMessage("Timezone cannot exceed 50 characters.");

        RuleFor(x => x.ProfilePictureUrl)
            .Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Profile picture URL must be a valid absolute URL when provided.")
            .MaximumLength(1000).WithMessage("Profile picture URL cannot exceed 1000 characters.");
    }
}

public class UpdatePreferencesRequestValidator : AbstractValidator<UpdatePreferencesRequestDto>
{
    public UpdatePreferencesRequestValidator()
    {
        RuleFor(x => x.Theme)
            .NotEmpty().WithMessage("Theme is required.")
            .MaximumLength(50).WithMessage("Theme name cannot exceed 50 characters.");

        RuleFor(x => x.DailyStudyTargetMinutes)
            .InclusiveBetween((uint)15, (uint)1440)
            .WithMessage("Daily study target must be between 15 minutes and 24 hours (1440 minutes).");

        RuleFor(x => x.PreferredWakeTime)
            .Must(t => TimeSpan.TryParse(t, out _))
            .WithMessage("Preferred wake time must be a valid time in HH:mm:ss format.");

        RuleFor(x => x.PreferredSleepTime)
            .Must(t => TimeSpan.TryParse(t, out _))
            .WithMessage("Preferred sleep time must be a valid time in HH:mm:ss format.");
    }
}

