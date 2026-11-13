using Bloom.BLL.DTOs;
using FluentValidation;

namespace Bloom.BLL.Validators;

public class CreateSubjectValidator : AbstractValidator<CreateSubjectDto>
{
    public CreateSubjectValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Subject code is required.")
            .MaximumLength(20).WithMessage("Subject code must not exceed 20 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Subject name is required.")
            .MaximumLength(150).WithMessage("Subject name must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public class UpdateSubjectValidator : AbstractValidator<UpdateSubjectDto>
{
    public UpdateSubjectValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Subject code is required.")
            .MaximumLength(20).WithMessage("Subject code must not exceed 20 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Subject name is required.")
            .MaximumLength(150).WithMessage("Subject name must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public class CreateModuleValidator : AbstractValidator<CreateModuleDto>
{
    public CreateModuleValidator()
    {
        RuleFor(x => x.SubjectId)
            .GreaterThan(0ul).WithMessage("Valid SubjectId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Module name is required.")
            .MaximumLength(150).WithMessage("Module name must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public class UpdateModuleValidator : AbstractValidator<UpdateModuleDto>
{
    public UpdateModuleValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Module name is required.")
            .MaximumLength(150).WithMessage("Module name must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public class CreateTopicValidator : AbstractValidator<CreateTopicDto>
{
    private static readonly HashSet<string> AllowedDifficulties = new(StringComparer.OrdinalIgnoreCase) { "Easy", "Medium", "Hard" };
    private static readonly HashSet<string> AllowedImportances = new(StringComparer.OrdinalIgnoreCase) { "Low", "Medium", "High" };

    public CreateTopicValidator()
    {
        RuleFor(x => x.ModuleId)
            .GreaterThan(0ul).WithMessage("Valid ModuleId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Topic name is required.")
            .MaximumLength(150).WithMessage("Topic name must not exceed 150 characters.");

        RuleFor(x => x.EstimatedStudyHours)
            .GreaterThan(0m).WithMessage("Estimated study hours must be greater than 0.")
            .LessThanOrEqualTo(100m).WithMessage("Estimated study hours cannot exceed 100.");

        RuleFor(x => x.DifficultyLevel)
            .Must(d => AllowedDifficulties.Contains(d))
            .WithMessage("Difficulty level must be Easy, Medium, or Hard.");

        RuleFor(x => x.ImportanceLevel)
            .Must(i => AllowedImportances.Contains(i))
            .WithMessage("Importance level must be Low, Medium, or High.");
    }
}

public class UpdateTopicValidator : AbstractValidator<UpdateTopicDto>
{
    private static readonly HashSet<string> AllowedDifficulties = new(StringComparer.OrdinalIgnoreCase) { "Easy", "Medium", "Hard" };
    private static readonly HashSet<string> AllowedImportances = new(StringComparer.OrdinalIgnoreCase) { "Low", "Medium", "High" };

    public UpdateTopicValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Topic name is required.")
            .MaximumLength(150).WithMessage("Topic name must not exceed 150 characters.");

        RuleFor(x => x.EstimatedStudyHours)
            .GreaterThan(0m).WithMessage("Estimated study hours must be greater than 0.")
            .LessThanOrEqualTo(100m).WithMessage("Estimated study hours cannot exceed 100.");

        RuleFor(x => x.DifficultyLevel)
            .Must(d => AllowedDifficulties.Contains(d))
            .WithMessage("Difficulty level must be Easy, Medium, or Hard.");

        RuleFor(x => x.ImportanceLevel)
            .Must(i => AllowedImportances.Contains(i))
            .WithMessage("Importance level must be Low, Medium, or High.");
    }
}

public class CreateSubtopicValidator : AbstractValidator<CreateSubtopicDto>
{
    public CreateSubtopicValidator()
    {
        RuleFor(x => x.TopicId)
            .GreaterThan(0ul).WithMessage("Valid TopicId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Subtopic name is required.")
            .MaximumLength(150).WithMessage("Subtopic name must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public class UpdateSubtopicValidator : AbstractValidator<UpdateSubtopicDto>
{
    public UpdateSubtopicValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Subtopic name is required.")
            .MaximumLength(150).WithMessage("Subtopic name must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public class UpdateTopicProgressValidator : AbstractValidator<UpdateTopicProgressDto>
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "NotStarted", "Learning", "Practicing", "Completed", "RevisionDue", "Mastered"
    };

    private static readonly HashSet<string> AllowedPriorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Low", "Medium", "High", "Urgent"
    };

    public UpdateTopicProgressValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage("Invalid status. Allowed values: NotStarted, Learning, Practicing, Completed, RevisionDue, Mastered.");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Priority is required.")
            .Must(p => AllowedPriorities.Contains(p))
            .WithMessage("Invalid priority. Allowed values: Low, Medium, High, Urgent.");

        RuleFor(x => x.ConfidenceRating)
            .InclusiveBetween((byte)0, (byte)5).WithMessage("Confidence rating must be between 0 and 5.");
    }
}

public class CreateResourceValidator : AbstractValidator<CreateResourceDto>
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Article", "Video", "Book", "FormulaSheet", "Notes", "Link"
    };

    public CreateResourceValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Resource title is required.")
            .MaximumLength(200).WithMessage("Resource title must not exceed 200 characters.");

        RuleFor(x => x.ResourceType)
            .Must(t => AllowedTypes.Contains(t))
            .WithMessage("Resource type must be Article, Video, Book, FormulaSheet, Notes, or Link.");

        RuleFor(x => x.Url)
            .MaximumLength(1000).WithMessage("URL must not exceed 1000 characters.");
    }
}

