using Bloom.Core.Entities;

namespace Bloom.Core.Interfaces;

public interface ISyllabusRepository
{
    // Subjects
    Task<IReadOnlyList<SyllabusSubject>> GetSubjectsAsync(ulong? userId, bool includeInactive = false, CancellationToken ct = default);
    Task<SyllabusSubject?> GetSubjectByIdAsync(ulong id, ulong? userId, CancellationToken ct = default);
    Task<SyllabusSubject> CreateSubjectAsync(SyllabusSubject subject, CancellationToken ct = default);
    Task<bool> UpdateSubjectAsync(SyllabusSubject subject, CancellationToken ct = default);
    Task<bool> DeleteSubjectAsync(ulong id, ulong userId, CancellationToken ct = default);

    // Modules
    Task<IReadOnlyList<SyllabusModule>> GetModulesBySubjectIdAsync(ulong subjectId, ulong? userId, bool includeInactive = false, CancellationToken ct = default);
    Task<SyllabusModule?> GetModuleByIdAsync(ulong id, ulong? userId, CancellationToken ct = default);
    Task<SyllabusModule> CreateModuleAsync(SyllabusModule module, CancellationToken ct = default);
    Task<bool> UpdateModuleAsync(SyllabusModule module, CancellationToken ct = default);
    Task<bool> DeleteModuleAsync(ulong id, ulong userId, CancellationToken ct = default);

    // Topics
    Task<IReadOnlyList<SyllabusTopic>> GetTopicsByModuleIdAsync(ulong moduleId, ulong? userId, bool includeInactive = false, CancellationToken ct = default);
    Task<SyllabusTopic?> GetTopicByIdAsync(ulong id, ulong? userId, CancellationToken ct = default);
    Task<SyllabusTopic> CreateTopicAsync(SyllabusTopic topic, CancellationToken ct = default);
    Task<bool> UpdateTopicAsync(SyllabusTopic topic, CancellationToken ct = default);
    Task<bool> DeleteTopicAsync(ulong id, ulong userId, CancellationToken ct = default);

    // Subtopics
    Task<IReadOnlyList<SyllabusSubtopic>> GetSubtopicsByTopicIdAsync(ulong topicId, ulong? userId, bool includeInactive = false, CancellationToken ct = default);
    Task<SyllabusSubtopic?> GetSubtopicByIdAsync(ulong id, ulong? userId, CancellationToken ct = default);
    Task<SyllabusSubtopic> CreateSubtopicAsync(SyllabusSubtopic subtopic, CancellationToken ct = default);
    Task<bool> UpdateSubtopicAsync(SyllabusSubtopic subtopic, CancellationToken ct = default);
    Task<bool> DeleteSubtopicAsync(ulong id, ulong userId, CancellationToken ct = default);

    // Resources
    Task<IReadOnlyList<SyllabusResource>> GetResourcesByTopicIdAsync(ulong topicId, ulong? userId, CancellationToken ct = default);
    Task<SyllabusResource?> GetResourceByIdAsync(ulong id, ulong? userId, CancellationToken ct = default);
    Task<SyllabusResource> CreateResourceAsync(SyllabusResource resource, CancellationToken ct = default);
    Task<bool> DeleteResourceAsync(ulong id, ulong userId, CancellationToken ct = default);
}

