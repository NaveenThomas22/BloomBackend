using Bloom.Core.Entities;

namespace Bloom.Core.Interfaces;

public interface ISyllabusProgressRepository
{
    Task<UserTopicProgress?> GetTopicProgressAsync(ulong userId, ulong topicId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<ulong, UserTopicProgress>> GetTopicProgressMapAsync(ulong userId, IEnumerable<ulong> topicIds, CancellationToken ct = default);
    Task<UserTopicProgress> UpsertTopicProgressAsync(UserTopicProgress progress, CancellationToken ct = default);

    Task<UserSubtopicProgress?> GetSubtopicProgressAsync(ulong userId, ulong subtopicId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<ulong, UserSubtopicProgress>> GetSubtopicProgressMapAsync(ulong userId, IEnumerable<ulong> subtopicIds, CancellationToken ct = default);
    Task<UserSubtopicProgress> UpsertSubtopicProgressAsync(UserSubtopicProgress progress, CancellationToken ct = default);

    Task<IReadOnlyList<UserTopicProgress>> GetAllUserProgressAsync(ulong userId, CancellationToken ct = default);
}

