using Bloom.BLL.DTOs;

namespace Bloom.BLL.Interfaces;

public interface ISyllabusService
{
    // Overview & Tree
    Task<SyllabusOverviewDto> GetOverviewAsync(ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<SubjectSummaryDto>> GetSubjectsAsync(ulong userId, CancellationToken ct = default);
    Task<SubjectDetailDto> GetSubjectDetailAsync(ulong userId, ulong subjectId, CancellationToken ct = default);
    Task<ModuleDetailDto> GetModuleDetailAsync(ulong userId, ulong moduleId, CancellationToken ct = default);
    Task<TopicDetailDto> GetTopicDetailAsync(ulong userId, ulong topicId, CancellationToken ct = default);

    // Customization - Subjects
    Task<SubjectSummaryDto> CreateCustomSubjectAsync(ulong userId, CreateSubjectDto dto, CancellationToken ct = default);
    Task<SubjectSummaryDto> UpdateSubjectAsync(ulong userId, ulong subjectId, UpdateSubjectDto dto, CancellationToken ct = default);
    Task<bool> DeleteSubjectAsync(ulong userId, ulong subjectId, CancellationToken ct = default);

    // Customization - Modules
    Task<ModuleDetailDto> CreateCustomModuleAsync(ulong userId, CreateModuleDto dto, CancellationToken ct = default);
    Task<ModuleDetailDto> UpdateModuleAsync(ulong userId, ulong moduleId, UpdateModuleDto dto, CancellationToken ct = default);
    Task<bool> DeleteModuleAsync(ulong userId, ulong moduleId, CancellationToken ct = default);

    // Customization - Topics
    Task<TopicDetailDto> CreateCustomTopicAsync(ulong userId, CreateTopicDto dto, CancellationToken ct = default);
    Task<TopicDetailDto> UpdateTopicAsync(ulong userId, ulong topicId, UpdateTopicDto dto, CancellationToken ct = default);
    Task<bool> DeleteTopicAsync(ulong userId, ulong topicId, CancellationToken ct = default);

    // Customization - Subtopics
    Task<SubtopicDetailDto> CreateCustomSubtopicAsync(ulong userId, CreateSubtopicDto dto, CancellationToken ct = default);
    Task<SubtopicDetailDto> UpdateSubtopicAsync(ulong userId, ulong subtopicId, UpdateSubtopicDto dto, CancellationToken ct = default);
    Task<bool> DeleteSubtopicAsync(ulong userId, ulong subtopicId, CancellationToken ct = default);

    // Progress updates
    Task<TopicSummaryDto> UpdateTopicProgressAsync(ulong userId, ulong topicId, UpdateTopicProgressDto dto, CancellationToken ct = default);
    Task<SubtopicDetailDto> UpdateSubtopicProgressAsync(ulong userId, ulong subtopicId, UpdateSubtopicProgressDto dto, CancellationToken ct = default);

    // Resources
    Task<SyllabusResourceDto> AddTopicResourceAsync(ulong userId, ulong topicId, CreateResourceDto dto, CancellationToken ct = default);
    Task<bool> DeleteTopicResourceAsync(ulong userId, ulong resourceId, CancellationToken ct = default);
}

