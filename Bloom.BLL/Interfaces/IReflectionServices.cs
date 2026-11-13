using Bloom.BLL.DTOs;

namespace Bloom.BLL.Interfaces;

public interface IReflectionService
{
    Task<IReadOnlyList<ReflectionMoodDto>> GetMoodsAsync(CancellationToken ct = default);
    Task<DailyReflectionDto> GetReflectionByDateAsync(ulong userId, DateOnly? reflectionDate, CancellationToken ct = default);
    Task<DailyReflectionDto> SaveMorningReflectionAsync(ulong userId, SaveMorningReflectionRequest request, CancellationToken ct = default);
    Task<DailyReflectionDto> SaveEveningReflectionAsync(ulong userId, SaveEveningReflectionRequest request, CancellationToken ct = default);
    Task<WeeklyReflectionSummaryDto> GetWeeklySummaryAsync(ulong userId, DateOnly? startOfWeek, DateOnly? endOfWeek, CancellationToken ct = default);
}

