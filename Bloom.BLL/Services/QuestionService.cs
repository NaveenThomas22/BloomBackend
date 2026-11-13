using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Bloom.BLL.Services;

public class QuestionService : IQuestionService, IQuestionPassageService, IQuestionSetService, IBookmarkService
{
    private readonly IQuestionRepository _questionRepo;
    private readonly IQuestionPassageRepository _passageRepo;
    private readonly IQuestionSetRepository _setRepo;
    private readonly IUserQuestionBookmarkRepository _bookmarkRepo;
    private readonly IValidator<CreateQuestionRequest> _createQuestionValidator;
    private readonly IValidator<CreatePassageRequest> _passageValidator;
    private readonly IValidator<CreateQuestionSetRequest> _setValidator;
    private readonly ILogger<QuestionService> _logger;

    public QuestionService(
        IQuestionRepository questionRepo,
        IQuestionPassageRepository passageRepo,
        IQuestionSetRepository setRepo,
        IUserQuestionBookmarkRepository bookmarkRepo,
        IValidator<CreateQuestionRequest> createQuestionValidator,
        IValidator<CreatePassageRequest> passageValidator,
        IValidator<CreateQuestionSetRequest> setValidator,
        ILogger<QuestionService> logger)
    {
        _questionRepo = questionRepo;
        _passageRepo = passageRepo;
        _setRepo = setRepo;
        _bookmarkRepo = bookmarkRepo;
        _createQuestionValidator = createQuestionValidator;
        _passageValidator = passageValidator;
        _setValidator = setValidator;
        _logger = logger;
    }

    #region Questions

    public async Task<QuestionDto?> GetQuestionByIdAsync(ulong id, ulong userId, bool isEditor = false, CancellationToken ct = default)
    {
        var question = await _questionRepo.GetQuestionByIdAsync(id, includeAnswerKey: isEditor, ct);
        if (question == null) return null;

        var isBookmarked = await _bookmarkRepo.IsBookmarkedAsync(userId, id, ct);
        var dto = MapToDto(question);
        dto.IsBookmarked = isBookmarked;
        return dto;
    }

    public async Task<(IReadOnlyList<QuestionDto> Questions, int TotalCount)> GetQuestionsPagedAsync(
        QuestionFilter filter, ulong userId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var (questions, totalCount) = await _questionRepo.GetQuestionsPagedAsync(filter, page, pageSize, ct);
        if (questions.Count == 0) return (Array.Empty<QuestionDto>(), 0);

        var qIds = questions.Select(q => q.Id);
        var bookmarkedIds = await _bookmarkRepo.GetBookmarkedQuestionIdsAsync(userId, qIds, ct);

        var dtos = questions.Select(q =>
        {
            var dto = MapToDto(q);
            dto.IsBookmarked = bookmarkedIds.Contains(q.Id);
            return dto;
        }).ToList();

        return (dtos, totalCount);
    }

    public async Task<QuestionDto> CreateQuestionAsync(CreateQuestionRequest request, CancellationToken ct = default)
    {
        var val = await _createQuestionValidator.ValidateAsync(request, ct);
        if (!val.IsValid)
        {
            var errors = val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Bloom.Core.Exceptions.ValidationException(errors);
        }

        var question = new Question
        {
            SubjectId = request.SubjectId,
            ModuleId = request.ModuleId,
            TopicId = request.TopicId,
            SubtopicId = request.SubtopicId,
            QuestionSetId = request.QuestionSetId,
            PassageId = request.PassageId,
            QuestionType = request.QuestionType,
            DifficultyLevel = request.DifficultyLevel,
            QuestionContent = request.QuestionContent.Trim(),
            QuestionImageUrl = request.QuestionImageUrl,
            CorrectAnswer = request.CorrectAnswer.Trim(),
            TitaNumericRangeMin = request.TitaNumericRangeMin,
            TitaNumericRangeMax = request.TitaNumericRangeMax,
            Explanation = request.Explanation?.Trim(),
            SolutionSteps = request.SolutionSteps?.Trim(),
            EstimatedSeconds = request.EstimatedSeconds > 0 ? request.EstimatedSeconds : 120,
            DefaultMarks = request.DefaultMarks > 0 ? request.DefaultMarks : 3.00m,
            DefaultPenalty = request.QuestionType == "TITA" ? 0m : (request.DefaultPenalty >= 0 ? request.DefaultPenalty : 1.00m),
            SourceAttribution = request.SourceAttribution?.Trim(),
            Tags = request.Tags?.Trim(),
            Status = request.Status
        };

        var options = request.Options.Select(o => new QuestionOption
        {
            OptionKey = o.OptionKey.Trim().ToUpperInvariant(),
            Content = o.Content.Trim(),
            IsCorrect = o.IsCorrect,
            DisplayOrder = o.DisplayOrder
        }).ToList();

        var created = await _questionRepo.CreateQuestionAsync(question, options, ct);
        _logger.LogInformation("Created new question ID {QuestionId} ({Type}, {Difficulty})", created.Id, created.QuestionType, created.DifficultyLevel);
        return MapToDto(created);
    }

    public async Task<QuestionDto> UpdateQuestionAsync(ulong id, UpdateQuestionRequest request, CancellationToken ct = default)
    {
        var existing = await _questionRepo.GetQuestionByIdAsync(id, includeAnswerKey: true, ct);
        if (existing == null) throw new NotFoundException($"Question with ID {id} not found.");

        var val = await _createQuestionValidator.ValidateAsync(request, ct);
        if (!val.IsValid)
        {
            var errors = val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Bloom.Core.Exceptions.ValidationException(errors);
        }

        existing.SubjectId = request.SubjectId;
        existing.ModuleId = request.ModuleId;
        existing.TopicId = request.TopicId;
        existing.SubtopicId = request.SubtopicId;
        existing.QuestionSetId = request.QuestionSetId;
        existing.PassageId = request.PassageId;
        existing.QuestionType = request.QuestionType;
        existing.DifficultyLevel = request.DifficultyLevel;
        existing.QuestionContent = request.QuestionContent.Trim();
        existing.QuestionImageUrl = request.QuestionImageUrl;
        existing.CorrectAnswer = request.CorrectAnswer.Trim();
        existing.TitaNumericRangeMin = request.TitaNumericRangeMin;
        existing.TitaNumericRangeMax = request.TitaNumericRangeMax;
        existing.Explanation = request.Explanation?.Trim();
        existing.SolutionSteps = request.SolutionSteps?.Trim();
        existing.EstimatedSeconds = request.EstimatedSeconds;
        existing.DefaultMarks = request.DefaultMarks;
        existing.DefaultPenalty = request.QuestionType == "TITA" ? 0m : request.DefaultPenalty;
        existing.SourceAttribution = request.SourceAttribution?.Trim();
        existing.Tags = request.Tags?.Trim();
        existing.Status = request.Status;

        var options = request.Options.Select(o => new QuestionOption
        {
            QuestionId = id,
            OptionKey = o.OptionKey.Trim().ToUpperInvariant(),
            Content = o.Content.Trim(),
            IsCorrect = o.IsCorrect,
            DisplayOrder = o.DisplayOrder
        }).ToList();

        await _questionRepo.UpdateQuestionAsync(existing, options, ct);
        existing.Options = options;
        return MapToDto(existing);
    }

    public async Task<bool> PublishQuestionAsync(ulong id, CancellationToken ct = default)
    {
        return await _questionRepo.UpdateQuestionStatusAsync(id, "Published", ct);
    }

    public async Task<bool> ArchiveQuestionAsync(ulong id, CancellationToken ct = default)
    {
        return await _questionRepo.UpdateQuestionStatusAsync(id, "Archived", ct);
    }

    #endregion

    #region Passages & Question Sets

    public async Task<QuestionPassageDto?> GetPassageByIdAsync(ulong id, CancellationToken ct = default)
    {
        var p = await _passageRepo.GetPassageByIdAsync(id, ct);
        return p == null ? null : MapPassageDto(p);
    }

    public async Task<IReadOnlyList<QuestionPassageDto>> GetPassagesAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var list = await _passageRepo.GetPassagesAsync(activeOnly, ct);
        return list.Select(MapPassageDto).ToList();
    }

    public async Task<QuestionPassageDto> CreatePassageAsync(CreatePassageRequest request, CancellationToken ct = default)
    {
        var val = await _passageValidator.ValidateAsync(request, ct);
        if (!val.IsValid)
        {
            var errors = val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Bloom.Core.Exceptions.ValidationException(errors);
        }

        var wordCount = request.Content.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var passage = new QuestionPassage
        {
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            WordCount = wordCount,
            Genre = request.Genre?.Trim(),
            SourceAttribution = request.SourceAttribution?.Trim(),
            IsActive = true
        };

        var created = await _passageRepo.CreatePassageAsync(passage, ct);
        return MapPassageDto(created);
    }

    public async Task<QuestionSetDto?> GetSetByIdAsync(ulong id, CancellationToken ct = default)
    {
        var s = await _setRepo.GetSetByIdAsync(id, ct);
        return s == null ? null : MapSetDto(s);
    }

    public async Task<IReadOnlyList<QuestionSetDto>> GetSetsAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var list = await _setRepo.GetSetsAsync(activeOnly, ct);
        return list.Select(MapSetDto).ToList();
    }

    public async Task<QuestionSetDto> CreateSetAsync(CreateQuestionSetRequest request, CancellationToken ct = default)
    {
        var val = await _setValidator.ValidateAsync(request, ct);
        if (!val.IsValid)
        {
            var errors = val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Bloom.Core.Exceptions.ValidationException(errors);
        }

        var set = new QuestionSet
        {
            Title = request.Title.Trim(),
            ContextScenario = request.ContextScenario.Trim(),
            DataPayloadJson = request.DataPayloadJson,
            SetType = request.SetType,
            SourceAttribution = request.SourceAttribution?.Trim(),
            IsActive = true
        };

        var created = await _setRepo.CreateSetAsync(set, ct);
        return MapSetDto(created);
    }

    #endregion

    #region Bookmarks

    public async Task<(IReadOnlyList<QuestionDto> Questions, int TotalCount)> GetBookmarksAsync(
        ulong userId, ulong? subjectId = null, ulong? topicId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var (questions, total) = await _bookmarkRepo.GetBookmarksAsync(userId, subjectId, topicId, page, pageSize, ct);
        var dtos = questions.Select(q =>
        {
            var dto = MapToDto(q);
            dto.IsBookmarked = true;
            return dto;
        }).ToList();

        return (dtos, total);
    }

    public async Task<bool> AddBookmarkAsync(ulong userId, ulong questionId, AddBookmarkRequest request, CancellationToken ct = default)
    {
        var q = await _questionRepo.GetQuestionByIdAsync(questionId, false, ct);
        if (q == null) throw new NotFoundException($"Question with ID {questionId} not found.");

        return await _bookmarkRepo.AddBookmarkAsync(userId, questionId, request.Note, ct);
    }

    public async Task<bool> RemoveBookmarkAsync(ulong userId, ulong questionId, CancellationToken ct = default)
    {
        return await _bookmarkRepo.RemoveBookmarkAsync(userId, questionId, ct);
    }

    #endregion

    private static QuestionDto MapToDto(Question q)
    {
        return new QuestionDto
        {
            Id = q.Id,
            SubjectId = q.SubjectId,
            SubjectName = q.SubjectName,
            ModuleId = q.ModuleId,
            TopicId = q.TopicId,
            TopicName = q.TopicName,
            SubtopicId = q.SubtopicId,
            QuestionSetId = q.QuestionSetId,
            PassageId = q.PassageId,
            QuestionType = q.QuestionType,
            DifficultyLevel = q.DifficultyLevel,
            QuestionContent = q.QuestionContent,
            QuestionImageUrl = q.QuestionImageUrl,
            EstimatedSeconds = q.EstimatedSeconds,
            DefaultMarks = q.DefaultMarks,
            DefaultPenalty = q.DefaultPenalty,
            SourceAttribution = q.SourceAttribution,
            Tags = q.Tags,
            Status = q.Status,
            CorrectAnswer = q.CorrectAnswer,
            Explanation = q.Explanation,
            SolutionSteps = q.SolutionSteps,
            Options = q.Options.Select(o => new QuestionOptionDto
            {
                Id = o.Id,
                OptionKey = o.OptionKey,
                Content = o.Content,
                DisplayOrder = o.DisplayOrder,
                IsCorrect = string.IsNullOrWhiteSpace(q.CorrectAnswer) ? null : o.IsCorrect
            }).ToList(),
            Passage = q.Passage == null ? null : MapPassageDto(q.Passage),
            QuestionSet = q.QuestionSet == null ? null : MapSetDto(q.QuestionSet)
        };
    }

    private static QuestionPassageDto MapPassageDto(QuestionPassage p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        Content = p.Content,
        WordCount = p.WordCount,
        Genre = p.Genre,
        SourceAttribution = p.SourceAttribution,
        IsActive = p.IsActive
    };

    private static QuestionSetDto MapSetDto(QuestionSet s) => new()
    {
        Id = s.Id,
        Title = s.Title,
        ContextScenario = s.ContextScenario,
        DataPayloadJson = s.DataPayloadJson,
        SetType = s.SetType,
        SourceAttribution = s.SourceAttribution,
        IsActive = s.IsActive
    };
}

