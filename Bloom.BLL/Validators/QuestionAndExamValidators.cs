using Bloom.BLL.DTOs;
using FluentValidation;

namespace Bloom.BLL.Validators;

public class CreateQuestionRequestValidator : AbstractValidator<CreateQuestionRequest>
{
    public CreateQuestionRequestValidator()
    {
        RuleFor(x => x.SubjectId).GreaterThan(0ul).WithMessage("Valid Subject ID is required.");
        RuleFor(x => x.ModuleId).GreaterThan(0ul).WithMessage("Valid Module ID is required.");
        RuleFor(x => x.TopicId).GreaterThan(0ul).WithMessage("Valid Topic ID is required.");
        RuleFor(x => x.QuestionContent).NotEmpty().WithMessage("Question content cannot be empty.");
        RuleFor(x => x.QuestionType)
            .Must(t => t == "MCQ" || t == "TITA")
            .WithMessage("Question type must be either 'MCQ' or 'TITA'.");
        RuleFor(x => x.DifficultyLevel)
            .Must(d => d == "Easy" || d == "Medium" || d == "Hard")
            .WithMessage("Difficulty level must be 'Easy', 'Medium', or 'Hard'.");
        RuleFor(x => x.CorrectAnswer).NotEmpty().WithMessage("Correct answer is required.");

        When(x => x.QuestionType == "MCQ", () =>
        {
            RuleFor(x => x.Options)
                .Must(opts => opts != null && opts.Count >= 2)
                .WithMessage("MCQ questions must provide at least 2 options.");
            RuleFor(x => x.Options)
                .Must(opts => opts != null && opts.Count(o => o.IsCorrect) == 1)
                .WithMessage("MCQ questions must specify exactly one correct option.");
        });
    }
}

public class StartPracticeSessionRequestValidator : AbstractValidator<StartPracticeSessionRequest>
{
    public StartPracticeSessionRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.QuestionCount).InclusiveBetween(1, 100).WithMessage("Question count must be between 1 and 100.");
        RuleFor(x => x.PracticeMode)
            .Must(m => m == "Topic" || m == "Subject" || m == "Difficulty" || m == "Custom" || m == "DILRSet" || m == "VARCPassage")
            .WithMessage("Invalid practice mode.");

        When(x => x.PracticeMode == "Subject", () =>
        {
            RuleFor(x => x.SubjectId).NotNull().GreaterThan(0ul).WithMessage("Subject ID is required for subject practice.");
        });

        When(x => x.PracticeMode == "Topic", () =>
        {
            RuleFor(x => x.TopicId).NotNull().GreaterThan(0ul).WithMessage("Topic ID is required for topic practice.");
        });

        When(x => x.PracticeMode == "DILRSet", () =>
        {
            RuleFor(x => x.QuestionSetId).NotNull().GreaterThan(0ul).WithMessage("Question set ID is required for DILR set practice.");
        });

        When(x => x.PracticeMode == "VARCPassage", () =>
        {
            RuleFor(x => x.PassageId).NotNull().GreaterThan(0ul).WithMessage("Passage ID is required for passage practice.");
        });
    }
}

public class SavePracticeAnswerRequestValidator : AbstractValidator<SavePracticeAnswerRequest>
{
    public SavePracticeAnswerRequestValidator()
    {
        RuleFor(x => x.QuestionId).GreaterThan(0ul).WithMessage("Valid question ID is required.");
        RuleFor(x => x.TimeSpentSeconds).GreaterThanOrEqualTo(0);
    }
}

public class SaveExamAnswerRequestValidator : AbstractValidator<SaveExamAnswerRequest>
{
    public SaveExamAnswerRequestValidator()
    {
        RuleFor(x => x.QuestionId).GreaterThan(0ul).WithMessage("Valid question ID is required.");
        RuleFor(x => x.AnswerStatus)
            .Must(s => s == "NotAnswered" || s == "Answered" || s == "MarkedForReview" || s == "AnsweredAndMarkedForReview")
            .WithMessage("Invalid answer status.");
        RuleFor(x => x.TimeSpentSeconds).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ExpectedVersion)
            .NotNull().WithMessage("ExpectedVersion is required for optimistic concurrency.")
            .GreaterThan(0u).WithMessage("ExpectedVersion must be a positive integer.");
    }
}

public class CreatePassageRequestValidator : AbstractValidator<CreatePassageRequest>
{
    public CreatePassageRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty();
    }
}

public class CreateQuestionSetRequestValidator : AbstractValidator<CreateQuestionSetRequest>
{
    public CreateQuestionSetRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContextScenario).NotEmpty();
    }
}

