using Bloom.BLL.DTOs;
using FluentValidation;

namespace Bloom.BLL.Validators;

public class SaveMorningReflectionRequestValidator : AbstractValidator<SaveMorningReflectionRequest>
{
    public SaveMorningReflectionRequestValidator()
    {
        RuleFor(x => x.ReflectionDate)
            .NotEmpty().WithMessage("Reflection date is required.");

        RuleFor(x => x.MoodIntensity)
            .InclusiveBetween((byte)1, (byte)5).When(x => x.MoodIntensity.HasValue)
            .WithMessage("Mood intensity must be between 1 and 5.");

        RuleFor(x => x.EnergyLevel)
            .InclusiveBetween((byte)1, (byte)5).When(x => x.EnergyLevel.HasValue)
            .WithMessage("Energy level must be between 1 and 5.");

        RuleFor(x => x.StudyTargetMinutes)
            .LessThanOrEqualTo(1440u).When(x => x.StudyTargetMinutes.HasValue)
            .WithMessage("Study target cannot exceed 1,440 minutes (24 hours).");

        RuleFor(x => x.TodayIntention)
            .MaximumLength(500).WithMessage("Today's intention cannot exceed 500 characters.");

        RuleFor(x => x.MainPriority)
            .MaximumLength(500).WithMessage("Main priority cannot exceed 500 characters.");

        RuleFor(x => x.TaskOne)
            .MaximumLength(255).WithMessage("Task 1 cannot exceed 255 characters.");

        RuleFor(x => x.TaskTwo)
            .MaximumLength(255).WithMessage("Task 2 cannot exceed 255 characters.");

        RuleFor(x => x.TaskThree)
            .MaximumLength(255).WithMessage("Task 3 cannot exceed 255 characters.");

        RuleFor(x => x.PersonalCommitment)
            .MaximumLength(500).WithMessage("Personal commitment cannot exceed 500 characters.");

        RuleFor(x => x.MorningNotes)
            .MaximumLength(5000).WithMessage("Morning notes cannot exceed 5,000 characters.");
    }
}

public class SaveEveningReflectionRequestValidator : AbstractValidator<SaveEveningReflectionRequest>
{
    public SaveEveningReflectionRequestValidator()
    {
        RuleFor(x => x.ReflectionDate)
            .NotEmpty().WithMessage("Reflection date is required.");

        RuleFor(x => x.MoodIntensity)
            .InclusiveBetween((byte)1, (byte)5).When(x => x.MoodIntensity.HasValue)
            .WithMessage("Mood intensity must be between 1 and 5.");

        RuleFor(x => x.SatisfactionRating)
            .InclusiveBetween((byte)1, (byte)5).When(x => x.SatisfactionRating.HasValue)
            .WithMessage("Satisfaction rating must be between 1 and 5.");

        RuleFor(x => x.HowWasToday)
            .MaximumLength(5000).WithMessage("Day summary cannot exceed 5,000 characters.");

        RuleFor(x => x.WhatAccomplished)
            .MaximumLength(5000).WithMessage("Accomplishments cannot exceed 5,000 characters.");

        RuleFor(x => x.WhatWentWell)
            .MaximumLength(5000).WithMessage("What went well cannot exceed 5,000 characters.");

        RuleFor(x => x.CouldHaveBeenBetter)
            .MaximumLength(5000).WithMessage("Could have been better cannot exceed 5,000 characters.");

        RuleFor(x => x.WhatLearnedToday)
            .MaximumLength(5000).WithMessage("Learnings cannot exceed 5,000 characters.");

        RuleFor(x => x.WhatChallengedMe)
            .MaximumLength(5000).WithMessage("Challenges cannot exceed 5,000 characters.");

        RuleFor(x => x.ImproveTomorrow)
            .MaximumLength(5000).WithMessage("Improvement for tomorrow cannot exceed 5,000 characters.");

        RuleFor(x => x.GratitudeEntry)
            .MaximumLength(5000).WithMessage("Gratitude entry cannot exceed 5,000 characters.");
    }
}
