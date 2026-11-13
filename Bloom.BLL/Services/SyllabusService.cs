using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bloom.BLL.Services;

public class SyllabusService : ISyllabusService
{
    private readonly ISyllabusRepository _syllabusRepo;
    private readonly ISyllabusProgressRepository _progressRepo;
    private readonly IRevisionScheduleService? _revisionScheduleService;
    private readonly ILogger<SyllabusService> _logger;

    public SyllabusService(
        ISyllabusRepository syllabusRepo,
        ISyllabusProgressRepository progressRepo,
        ILogger<SyllabusService> logger,
        IRevisionScheduleService? revisionScheduleService = null)
    {
        _syllabusRepo = syllabusRepo ?? throw new ArgumentNullException(nameof(syllabusRepo));
        _progressRepo = progressRepo ?? throw new ArgumentNullException(nameof(progressRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _revisionScheduleService = revisionScheduleService;
    }

    #region Overview & Summaries

    public async Task<SyllabusOverviewDto> GetOverviewAsync(ulong userId, CancellationToken ct = default)
    {
        var subjects = await _syllabusRepo.GetSubjectsAsync(userId, false, ct);
        var allUserProgress = (await _progressRepo.GetAllUserProgressAsync(userId, ct))
            .ToDictionary(p => p.TopicId, p => p);

        var subjectSummaries = new List<SubjectSummaryDto>();

        var totalTopicsAll = 0;
        var completedTopicsAll = 0;
        var masteredTopicsAll = 0;
        var inProgressTopicsAll = 0;
        var revisionDueTopicsAll = 0;
        var notStartedTopicsAll = 0;
        var estimatedTotalHoursAll = 0m;
        var estimatedRemainingHoursAll = 0m;

        foreach (var subject in subjects)
        {
            var modules = await _syllabusRepo.GetModulesBySubjectIdAsync(subject.Id, userId, false, ct);
            var subjectTotalTopics = 0;
            var subjectCompletedTopics = 0;

            foreach (var module in modules)
            {
                var topics = await _syllabusRepo.GetTopicsByModuleIdAsync(module.Id, userId, false, ct);
                foreach (var topic in topics)
                {
                    allUserProgress.TryGetValue(topic.Id, out var prog);

                    if (prog?.IsCustomExcluded == true)
                    {
                        continue;
                    }

                    subjectTotalTopics++;
                    totalTopicsAll++;
                    estimatedTotalHoursAll += topic.EstimatedStudyHours;

                    var status = prog?.Status ?? "NotStarted";

                    switch (status)
                    {
                        case "Mastered":
                            masteredTopicsAll++;
                            completedTopicsAll++;
                            subjectCompletedTopics++;
                            break;
                        case "Completed":
                            completedTopicsAll++;
                            subjectCompletedTopics++;
                            break;
                        case "RevisionDue":
                            revisionDueTopicsAll++;
                            break;
                        case "Learning":
                        case "Practicing":
                            inProgressTopicsAll++;
                            estimatedRemainingHoursAll += (topic.EstimatedStudyHours * 0.5m);
                            break;
                        default:
                            notStartedTopicsAll++;
                            estimatedRemainingHoursAll += topic.EstimatedStudyHours;
                            break;
                    }
                }
            }

            var subjectProgressPct = subjectTotalTopics > 0
                ? Math.Round(((double)subjectCompletedTopics / subjectTotalTopics) * 100.0, 1)
                : 0.0;

            subjectSummaries.Add(new SubjectSummaryDto
            {
                Id = subject.Id,
                Code = subject.Code,
                Name = subject.Name,
                Description = subject.Description,
                IsCustom = subject.UserId.HasValue,
                ModuleCount = modules.Count,
                TopicCount = subjectTotalTopics,
                CompletedTopicCount = subjectCompletedTopics,
                ProgressPercentage = subjectProgressPct,
                DisplayOrder = subject.DisplayOrder
            });
        }

        var overallProgressPct = totalTopicsAll > 0
            ? Math.Round(((double)completedTopicsAll / totalTopicsAll) * 100.0, 1)
            : 0.0;

        return new SyllabusOverviewDto
        {
            TotalTopics = totalTopicsAll,
            CompletedTopics = completedTopicsAll,
            MasteredTopics = masteredTopicsAll,
            InProgressTopics = inProgressTopicsAll,
            RevisionDueTopics = revisionDueTopicsAll,
            NotStartedTopics = notStartedTopicsAll,
            OverallProgressPercentage = overallProgressPct,
            EstimatedTotalHours = estimatedTotalHoursAll,
            EstimatedRemainingHours = estimatedRemainingHoursAll,
            Subjects = subjectSummaries
        };
    }

    public async Task<IReadOnlyList<SubjectSummaryDto>> GetSubjectsAsync(ulong userId, CancellationToken ct = default)
    {
        var overview = await GetOverviewAsync(userId, ct);
        return overview.Subjects;
    }

    public async Task<SubjectDetailDto> GetSubjectDetailAsync(ulong userId, ulong subjectId, CancellationToken ct = default)
    {
        var subject = await _syllabusRepo.GetSubjectByIdAsync(subjectId, userId, ct);
        if (subject == null)
        {
            throw new NotFoundException($"Subject with ID {subjectId} not found.");
        }

        var modules = await _syllabusRepo.GetModulesBySubjectIdAsync(subjectId, userId, false, ct);
        var moduleDetails = new List<ModuleDetailDto>();

        var subjectTotalTopics = 0;
        var subjectCompletedTopics = 0;

        foreach (var mod in modules)
        {
            var topics = await _syllabusRepo.GetTopicsByModuleIdAsync(mod.Id, userId, false, ct);
            var topicIds = topics.Select(t => t.Id).ToList();
            var progressMap = await _progressRepo.GetTopicProgressMapAsync(userId, topicIds, ct);

            var topicSummaries = new List<TopicSummaryDto>();
            var modCompletedTopics = 0;

            foreach (var topic in topics)
            {
                progressMap.TryGetValue(topic.Id, out var prog);

                var isCompleted = prog?.Status == "Completed" || prog?.Status == "Mastered";
                if (isCompleted)
                {
                    modCompletedTopics++;
                    subjectCompletedTopics++;
                }

                subjectTotalTopics++;

                // Subtopics count
                var subtopics = await _syllabusRepo.GetSubtopicsByTopicIdAsync(topic.Id, userId, false, ct);
                var subtopicIds = subtopics.Select(s => s.Id).ToList();
                var subProgressMap = await _progressRepo.GetSubtopicProgressMapAsync(userId, subtopicIds, ct);
                var completedSubCount = subProgressMap.Values.Count(s => s.IsCompleted);

                topicSummaries.Add(new TopicSummaryDto
                {
                    Id = topic.Id,
                    ModuleId = mod.Id,
                    Name = topic.Name,
                    Description = topic.Description,
                    EstimatedStudyHours = topic.EstimatedStudyHours,
                    DifficultyLevel = topic.DifficultyLevel,
                    ImportanceLevel = topic.ImportanceLevel,
                    DisplayOrder = topic.DisplayOrder,
                    IsCustom = topic.UserId.HasValue,
                    Status = prog?.Status ?? "NotStarted",
                    Priority = prog?.Priority ?? "Medium",
                    TargetDate = prog?.TargetDate,
                    CompletionDate = prog?.CompletionDate,
                    ConfidenceRating = prog?.ConfidenceRating ?? 0,
                    PersonalNotes = prog?.PersonalNotes,
                    IsCustomExcluded = prog?.IsCustomExcluded ?? false,
                    SubtopicCount = subtopics.Count,
                    CompletedSubtopicCount = completedSubCount
                });
            }

            var modProgressPct = topics.Count > 0
                ? Math.Round(((double)modCompletedTopics / topics.Count) * 100.0, 1)
                : 0.0;

            moduleDetails.Add(new ModuleDetailDto
            {
                Id = mod.Id,
                SubjectId = mod.SubjectId,
                Name = mod.Name,
                Description = mod.Description,
                IsCustom = mod.UserId.HasValue,
                TopicCount = topics.Count,
                CompletedTopicCount = modCompletedTopics,
                ProgressPercentage = modProgressPct,
                DisplayOrder = mod.DisplayOrder,
                Topics = topicSummaries
            });
        }

        var subjectProgressPct = subjectTotalTopics > 0
            ? Math.Round(((double)subjectCompletedTopics / subjectTotalTopics) * 100.0, 1)
            : 0.0;

        return new SubjectDetailDto
        {
            Subject = new SubjectSummaryDto
            {
                Id = subject.Id,
                Code = subject.Code,
                Name = subject.Name,
                Description = subject.Description,
                IsCustom = subject.UserId.HasValue,
                ModuleCount = modules.Count,
                TopicCount = subjectTotalTopics,
                CompletedTopicCount = subjectCompletedTopics,
                ProgressPercentage = subjectProgressPct,
                DisplayOrder = subject.DisplayOrder
            },
            Modules = moduleDetails
        };
    }

    public async Task<ModuleDetailDto> GetModuleDetailAsync(ulong userId, ulong moduleId, CancellationToken ct = default)
    {
        var module = await _syllabusRepo.GetModuleByIdAsync(moduleId, userId, ct);
        if (module == null)
        {
            throw new NotFoundException($"Module with ID {moduleId} not found.");
        }

        var topics = await _syllabusRepo.GetTopicsByModuleIdAsync(moduleId, userId, false, ct);
        var topicIds = topics.Select(t => t.Id).ToList();
        var progressMap = await _progressRepo.GetTopicProgressMapAsync(userId, topicIds, ct);

        var topicSummaries = new List<TopicSummaryDto>();
        var modCompleted = 0;

        foreach (var topic in topics)
        {
            progressMap.TryGetValue(topic.Id, out var prog);

            var isCompleted = prog?.Status == "Completed" || prog?.Status == "Mastered";
            if (isCompleted) modCompleted++;

            var subtopics = await _syllabusRepo.GetSubtopicsByTopicIdAsync(topic.Id, userId, false, ct);
            var subIds = subtopics.Select(s => s.Id).ToList();
            var subProgMap = await _progressRepo.GetSubtopicProgressMapAsync(userId, subIds, ct);
            var completedSubCount = subProgMap.Values.Count(s => s.IsCompleted);

            topicSummaries.Add(new TopicSummaryDto
            {
                Id = topic.Id,
                ModuleId = module.Id,
                Name = topic.Name,
                Description = topic.Description,
                EstimatedStudyHours = topic.EstimatedStudyHours,
                DifficultyLevel = topic.DifficultyLevel,
                ImportanceLevel = topic.ImportanceLevel,
                DisplayOrder = topic.DisplayOrder,
                IsCustom = topic.UserId.HasValue,
                Status = prog?.Status ?? "NotStarted",
                Priority = prog?.Priority ?? "Medium",
                TargetDate = prog?.TargetDate,
                CompletionDate = prog?.CompletionDate,
                ConfidenceRating = prog?.ConfidenceRating ?? 0,
                PersonalNotes = prog?.PersonalNotes,
                IsCustomExcluded = prog?.IsCustomExcluded ?? false,
                SubtopicCount = subtopics.Count,
                CompletedSubtopicCount = completedSubCount
            });
        }

        return new ModuleDetailDto
        {
            Id = module.Id,
            SubjectId = module.SubjectId,
            Name = module.Name,
            Description = module.Description,
            IsCustom = module.UserId.HasValue,
            TopicCount = topics.Count,
            CompletedTopicCount = modCompleted,
            ProgressPercentage = topics.Count > 0 ? Math.Round(((double)modCompleted / topics.Count) * 100.0, 1) : 0.0,
            DisplayOrder = module.DisplayOrder,
            Topics = topicSummaries
        };
    }

    public async Task<TopicDetailDto> GetTopicDetailAsync(ulong userId, ulong topicId, CancellationToken ct = default)
    {
        var topic = await _syllabusRepo.GetTopicByIdAsync(topicId, userId, ct);
        if (topic == null)
        {
            throw new NotFoundException($"Topic with ID {topicId} not found.");
        }

        var module = await _syllabusRepo.GetModuleByIdAsync(topic.ModuleId, userId, ct);
        var subject = module != null ? await _syllabusRepo.GetSubjectByIdAsync(module.SubjectId, userId, ct) : null;

        var prog = await _progressRepo.GetTopicProgressAsync(userId, topicId, ct);

        var subtopics = await _syllabusRepo.GetSubtopicsByTopicIdAsync(topicId, userId, false, ct);
        var subIds = subtopics.Select(s => s.Id).ToList();
        var subProgMap = await _progressRepo.GetSubtopicProgressMapAsync(userId, subIds, ct);

        var subtopicDetails = subtopics.Select(s =>
        {
            subProgMap.TryGetValue(s.Id, out var sp);
            return new SubtopicDetailDto
            {
                Id = s.Id,
                TopicId = s.TopicId,
                Name = s.Name,
                Description = s.Description,
                DisplayOrder = s.DisplayOrder,
                IsCustom = s.UserId.HasValue,
                IsCompleted = sp?.IsCompleted ?? false,
                CompletedAtUtc = sp?.CompletedAtUtc,
                Notes = sp?.Notes
            };
        }).ToList();

        var resources = await _syllabusRepo.GetResourcesByTopicIdAsync(topicId, userId, ct);
        var resourceDtos = resources.Select(r => new SyllabusResourceDto
        {
            Id = r.Id,
            TopicId = r.TopicId,
            Title = r.Title,
            ResourceType = r.ResourceType,
            Url = r.Url,
            Notes = r.Notes,
            IsCustom = r.UserId.HasValue,
            DisplayOrder = r.DisplayOrder
        }).ToList();

        return new TopicDetailDto
        {
            Topic = new TopicSummaryDto
            {
                Id = topic.Id,
                ModuleId = topic.ModuleId,
                Name = topic.Name,
                Description = topic.Description,
                EstimatedStudyHours = topic.EstimatedStudyHours,
                DifficultyLevel = topic.DifficultyLevel,
                ImportanceLevel = topic.ImportanceLevel,
                DisplayOrder = topic.DisplayOrder,
                IsCustom = topic.UserId.HasValue,
                Status = prog?.Status ?? "NotStarted",
                Priority = prog?.Priority ?? "Medium",
                TargetDate = prog?.TargetDate,
                CompletionDate = prog?.CompletionDate,
                ConfidenceRating = prog?.ConfidenceRating ?? 0,
                PersonalNotes = prog?.PersonalNotes,
                IsCustomExcluded = prog?.IsCustomExcluded ?? false,
                SubtopicCount = subtopics.Count,
                CompletedSubtopicCount = subtopicDetails.Count(s => s.IsCompleted)
            },
            ModuleName = module?.Name ?? string.Empty,
            SubjectName = subject?.Name ?? string.Empty,
            SubjectCode = subject?.Code ?? string.Empty,
            Subtopics = subtopicDetails,
            Resources = resourceDtos
        };
    }

    #endregion

    #region Customization - Subjects

    public async Task<SubjectSummaryDto> CreateCustomSubjectAsync(ulong userId, CreateSubjectDto dto, CancellationToken ct = default)
    {
        var subject = new SyllabusSubject
        {
            UserId = userId,
            Code = dto.Code.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            DisplayOrder = dto.DisplayOrder,
            IsActive = true
        };

        var created = await _syllabusRepo.CreateSubjectAsync(subject, ct);
        _logger.LogInformation("User {UserId} created custom subject {SubjectId} ({Code}).", userId, created.Id, created.Code);

        return new SubjectSummaryDto
        {
            Id = created.Id,
            Code = created.Code,
            Name = created.Name,
            Description = created.Description,
            IsCustom = true,
            ModuleCount = 0,
            TopicCount = 0,
            CompletedTopicCount = 0,
            ProgressPercentage = 0.0,
            DisplayOrder = created.DisplayOrder
        };
    }

    public async Task<SubjectSummaryDto> UpdateSubjectAsync(ulong userId, ulong subjectId, UpdateSubjectDto dto, CancellationToken ct = default)
    {
        var existing = await _syllabusRepo.GetSubjectByIdAsync(subjectId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Subject with ID {subjectId} not found.");
        }

        if (!existing.UserId.HasValue || existing.UserId.Value != userId)
        {
            throw new ForbiddenException("Cannot modify system default syllabus subjects.");
        }

        existing.Code = dto.Code.Trim().ToUpperInvariant();
        existing.Name = dto.Name.Trim();
        existing.Description = dto.Description?.Trim();
        existing.DisplayOrder = dto.DisplayOrder;
        existing.IsActive = dto.IsActive;

        await _syllabusRepo.UpdateSubjectAsync(existing, ct);

        return new SubjectSummaryDto
        {
            Id = existing.Id,
            Code = existing.Code,
            Name = existing.Name,
            Description = existing.Description,
            IsCustom = true,
            DisplayOrder = existing.DisplayOrder
        };
    }

    public async Task<bool> DeleteSubjectAsync(ulong userId, ulong subjectId, CancellationToken ct = default)
    {
        var existing = await _syllabusRepo.GetSubjectByIdAsync(subjectId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Subject with ID {subjectId} not found.");
        }

        if (!existing.UserId.HasValue || existing.UserId.Value != userId)
        {
            throw new ForbiddenException("Cannot delete system default syllabus subjects.");
        }

        return await _syllabusRepo.DeleteSubjectAsync(subjectId, userId, ct);
    }

    #endregion

    #region Customization - Modules

    public async Task<ModuleDetailDto> CreateCustomModuleAsync(ulong userId, CreateModuleDto dto, CancellationToken ct = default)
    {
        var subject = await _syllabusRepo.GetSubjectByIdAsync(dto.SubjectId, userId, ct);
        if (subject == null)
        {
            throw new NotFoundException($"Parent subject with ID {dto.SubjectId} not found.");
        }

        var module = new SyllabusModule
        {
            SubjectId = dto.SubjectId,
            UserId = userId,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            DisplayOrder = dto.DisplayOrder,
            IsActive = true
        };

        var created = await _syllabusRepo.CreateModuleAsync(module, ct);
        _logger.LogInformation("User {UserId} created custom module {ModuleId} under subject {SubjectId}.", userId, created.Id, dto.SubjectId);

        return new ModuleDetailDto
        {
            Id = created.Id,
            SubjectId = created.SubjectId,
            Name = created.Name,
            Description = created.Description,
            IsCustom = true,
            TopicCount = 0,
            CompletedTopicCount = 0,
            ProgressPercentage = 0.0,
            DisplayOrder = created.DisplayOrder,
            Topics = Array.Empty<TopicSummaryDto>()
        };
    }

    public async Task<ModuleDetailDto> UpdateModuleAsync(ulong userId, ulong moduleId, UpdateModuleDto dto, CancellationToken ct = default)
    {
        var existing = await _syllabusRepo.GetModuleByIdAsync(moduleId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Module with ID {moduleId} not found.");
        }

        if (!existing.UserId.HasValue || existing.UserId.Value != userId)
        {
            throw new ForbiddenException("Cannot modify system default syllabus modules.");
        }

        existing.Name = dto.Name.Trim();
        existing.Description = dto.Description?.Trim();
        existing.DisplayOrder = dto.DisplayOrder;
        existing.IsActive = dto.IsActive;

        await _syllabusRepo.UpdateModuleAsync(existing, ct);

        return new ModuleDetailDto
        {
            Id = existing.Id,
            SubjectId = existing.SubjectId,
            Name = existing.Name,
            Description = existing.Description,
            IsCustom = true,
            DisplayOrder = existing.DisplayOrder
        };
    }

    public async Task<bool> DeleteModuleAsync(ulong userId, ulong moduleId, CancellationToken ct = default)
    {
        var existing = await _syllabusRepo.GetModuleByIdAsync(moduleId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Module with ID {moduleId} not found.");
        }

        if (!existing.UserId.HasValue || existing.UserId.Value != userId)
        {
            throw new ForbiddenException("Cannot delete system default syllabus modules.");
        }

        return await _syllabusRepo.DeleteModuleAsync(moduleId, userId, ct);
    }

    #endregion

    #region Customization - Topics

    public async Task<TopicDetailDto> CreateCustomTopicAsync(ulong userId, CreateTopicDto dto, CancellationToken ct = default)
    {
        var module = await _syllabusRepo.GetModuleByIdAsync(dto.ModuleId, userId, ct);
        if (module == null)
        {
            throw new NotFoundException($"Parent module with ID {dto.ModuleId} not found.");
        }

        var topic = new SyllabusTopic
        {
            ModuleId = dto.ModuleId,
            UserId = userId,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            EstimatedStudyHours = dto.EstimatedStudyHours,
            DifficultyLevel = dto.DifficultyLevel,
            ImportanceLevel = dto.ImportanceLevel,
            DisplayOrder = dto.DisplayOrder,
            IsActive = true
        };

        var created = await _syllabusRepo.CreateTopicAsync(topic, ct);
        _logger.LogInformation("User {UserId} created custom topic {TopicId} under module {ModuleId}.", userId, created.Id, dto.ModuleId);

        return await GetTopicDetailAsync(userId, created.Id, ct);
    }

    public async Task<TopicDetailDto> UpdateTopicAsync(ulong userId, ulong topicId, UpdateTopicDto dto, CancellationToken ct = default)
    {
        var existing = await _syllabusRepo.GetTopicByIdAsync(topicId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Topic with ID {topicId} not found.");
        }

        if (!existing.UserId.HasValue || existing.UserId.Value != userId)
        {
            throw new ForbiddenException("Cannot modify system default syllabus topics.");
        }

        existing.Name = dto.Name.Trim();
        existing.Description = dto.Description?.Trim();
        existing.EstimatedStudyHours = dto.EstimatedStudyHours;
        existing.DifficultyLevel = dto.DifficultyLevel;
        existing.ImportanceLevel = dto.ImportanceLevel;
        existing.DisplayOrder = dto.DisplayOrder;
        existing.IsActive = dto.IsActive;

        await _syllabusRepo.UpdateTopicAsync(existing, ct);

        return await GetTopicDetailAsync(userId, topicId, ct);
    }

    public async Task<bool> DeleteTopicAsync(ulong userId, ulong topicId, CancellationToken ct = default)
    {
        var existing = await _syllabusRepo.GetTopicByIdAsync(topicId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Topic with ID {topicId} not found.");
        }

        if (!existing.UserId.HasValue || existing.UserId.Value != userId)
        {
            throw new ForbiddenException("Cannot delete system default syllabus topics.");
        }

        return await _syllabusRepo.DeleteTopicAsync(topicId, userId, ct);
    }

    #endregion

    #region Customization - Subtopics

    public async Task<SubtopicDetailDto> CreateCustomSubtopicAsync(ulong userId, CreateSubtopicDto dto, CancellationToken ct = default)
    {
        var topic = await _syllabusRepo.GetTopicByIdAsync(dto.TopicId, userId, ct);
        if (topic == null)
        {
            throw new NotFoundException($"Parent topic with ID {dto.TopicId} not found.");
        }

        var subtopic = new SyllabusSubtopic
        {
            TopicId = dto.TopicId,
            UserId = userId,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            DisplayOrder = dto.DisplayOrder,
            IsActive = true
        };

        var created = await _syllabusRepo.CreateSubtopicAsync(subtopic, ct);
        return new SubtopicDetailDto
        {
            Id = created.Id,
            TopicId = created.TopicId,
            Name = created.Name,
            Description = created.Description,
            DisplayOrder = created.DisplayOrder,
            IsCustom = true,
            IsCompleted = false
        };
    }

    public async Task<SubtopicDetailDto> UpdateSubtopicAsync(ulong userId, ulong subtopicId, UpdateSubtopicDto dto, CancellationToken ct = default)
    {
        var existing = await _syllabusRepo.GetSubtopicByIdAsync(subtopicId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Subtopic with ID {subtopicId} not found.");
        }

        if (!existing.UserId.HasValue || existing.UserId.Value != userId)
        {
            throw new ForbiddenException("Cannot modify system default subtopics.");
        }

        existing.Name = dto.Name.Trim();
        existing.Description = dto.Description?.Trim();
        existing.DisplayOrder = dto.DisplayOrder;
        existing.IsActive = dto.IsActive;

        await _syllabusRepo.UpdateSubtopicAsync(existing, ct);

        return new SubtopicDetailDto
        {
            Id = existing.Id,
            TopicId = existing.TopicId,
            Name = existing.Name,
            Description = existing.Description,
            DisplayOrder = existing.DisplayOrder,
            IsCustom = true
        };
    }

    public async Task<bool> DeleteSubtopicAsync(ulong userId, ulong subtopicId, CancellationToken ct = default)
    {
        var existing = await _syllabusRepo.GetSubtopicByIdAsync(subtopicId, userId, ct);
        if (existing == null)
        {
            throw new NotFoundException($"Subtopic with ID {subtopicId} not found.");
        }

        if (!existing.UserId.HasValue || existing.UserId.Value != userId)
        {
            throw new ForbiddenException("Cannot delete system default subtopics.");
        }

        return await _syllabusRepo.DeleteSubtopicAsync(subtopicId, userId, ct);
    }

    #endregion

    #region Progress Updates

    public async Task<TopicSummaryDto> UpdateTopicProgressAsync(ulong userId, ulong topicId, UpdateTopicProgressDto dto, CancellationToken ct = default)
    {
        var topic = await _syllabusRepo.GetTopicByIdAsync(topicId, userId, ct);
        if (topic == null)
        {
            throw new NotFoundException($"Topic with ID {topicId} not found.");
        }

        var existing = await _progressRepo.GetTopicProgressAsync(userId, topicId, ct);

        DateTime? completionDate = existing?.CompletionDate;
        if ((dto.Status == "Completed" || dto.Status == "Mastered") && completionDate == null)
        {
            completionDate = DateTime.UtcNow.Date;
        }

        var progress = new UserTopicProgress
        {
            UserId = userId,
            TopicId = topicId,
            Status = dto.Status,
            Priority = dto.Priority,
            TargetDate = dto.TargetDate?.Date,
            CompletionDate = completionDate,
            ConfidenceRating = dto.ConfidenceRating,
            PersonalNotes = dto.PersonalNotes?.Trim(),
            IsCustomExcluded = dto.IsCustomExcluded
        };

        var saved = await _progressRepo.UpsertTopicProgressAsync(progress, ct);

        // Auto-schedule initial revision step if topic transitioned to Completed or Mastered
        if ((saved.Status == "Completed" || saved.Status == "Mastered") &&
            (existing == null || (existing.Status != "Completed" && existing.Status != "Mastered")))
        {
            if (_revisionScheduleService != null)
            {
                try
                {
                    await _revisionScheduleService.ScheduleInitialRevisionForTopicAsync(userId, topicId, null, null, ct);
                    _logger.LogInformation("Auto-scheduled initial revision for topic {TopicId} upon completion by user {UserId}", topicId, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not auto-schedule initial revision for topic {TopicId}", topicId);
                }
            }
        }

        var subtopics = await _syllabusRepo.GetSubtopicsByTopicIdAsync(topicId, userId, false, ct);
        var subIds = subtopics.Select(s => s.Id).ToList();
        var subProgMap = await _progressRepo.GetSubtopicProgressMapAsync(userId, subIds, ct);

        return new TopicSummaryDto
        {
            Id = topic.Id,
            ModuleId = topic.ModuleId,
            Name = topic.Name,
            Description = topic.Description,
            EstimatedStudyHours = topic.EstimatedStudyHours,
            DifficultyLevel = topic.DifficultyLevel,
            ImportanceLevel = topic.ImportanceLevel,
            DisplayOrder = topic.DisplayOrder,
            IsCustom = topic.UserId.HasValue,
            Status = saved.Status,
            Priority = saved.Priority,
            TargetDate = saved.TargetDate,
            CompletionDate = saved.CompletionDate,
            ConfidenceRating = saved.ConfidenceRating,
            PersonalNotes = saved.PersonalNotes,
            IsCustomExcluded = saved.IsCustomExcluded,
            SubtopicCount = subtopics.Count,
            CompletedSubtopicCount = subProgMap.Values.Count(s => s.IsCompleted)
        };
    }

    public async Task<SubtopicDetailDto> UpdateSubtopicProgressAsync(ulong userId, ulong subtopicId, UpdateSubtopicProgressDto dto, CancellationToken ct = default)
    {
        var subtopic = await _syllabusRepo.GetSubtopicByIdAsync(subtopicId, userId, ct);
        if (subtopic == null)
        {
            throw new NotFoundException($"Subtopic with ID {subtopicId} not found.");
        }

        var progress = new UserSubtopicProgress
        {
            UserId = userId,
            SubtopicId = subtopicId,
            IsCompleted = dto.IsCompleted,
            CompletedAtUtc = dto.IsCompleted ? DateTime.UtcNow : null,
            Notes = dto.Notes?.Trim()
        };

        var saved = await _progressRepo.UpsertSubtopicProgressAsync(progress, ct);

        return new SubtopicDetailDto
        {
            Id = subtopic.Id,
            TopicId = subtopic.TopicId,
            Name = subtopic.Name,
            Description = subtopic.Description,
            DisplayOrder = subtopic.DisplayOrder,
            IsCustom = subtopic.UserId.HasValue,
            IsCompleted = saved.IsCompleted,
            CompletedAtUtc = saved.CompletedAtUtc,
            Notes = saved.Notes
        };
    }

    #endregion

    #region Resources

    public async Task<SyllabusResourceDto> AddTopicResourceAsync(ulong userId, ulong topicId, CreateResourceDto dto, CancellationToken ct = default)
    {
        var topic = await _syllabusRepo.GetTopicByIdAsync(topicId, userId, ct);
        if (topic == null)
        {
            throw new NotFoundException($"Topic with ID {topicId} not found.");
        }

        var resource = new SyllabusResource
        {
            TopicId = topicId,
            UserId = userId,
            Title = dto.Title.Trim(),
            ResourceType = dto.ResourceType,
            Url = dto.Url?.Trim(),
            Notes = dto.Notes?.Trim(),
            DisplayOrder = dto.DisplayOrder
        };

        var created = await _syllabusRepo.CreateResourceAsync(resource, ct);

        return new SyllabusResourceDto
        {
            Id = created.Id,
            TopicId = created.TopicId,
            Title = created.Title,
            ResourceType = created.ResourceType,
            Url = created.Url,
            Notes = created.Notes,
            IsCustom = true,
            DisplayOrder = created.DisplayOrder
        };
    }

    public async Task<bool> DeleteTopicResourceAsync(ulong userId, ulong resourceId, CancellationToken ct = default)
    {
        var resource = await _syllabusRepo.GetResourceByIdAsync(resourceId, userId, ct);
        if (resource == null)
        {
            throw new NotFoundException($"Resource with ID {resourceId} not found.");
        }

        if (!resource.UserId.HasValue || resource.UserId.Value != userId)
        {
            throw new ForbiddenException("Cannot delete default syllabus resources.");
        }

        return await _syllabusRepo.DeleteResourceAsync(resourceId, userId, ct);
    }

    #endregion
}

