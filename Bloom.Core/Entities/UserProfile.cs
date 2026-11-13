namespace Bloom.Core.Entities;

/// <summary>
/// Domain model for an aspirant's preparation profile.
/// </summary>
public class UserProfile : BaseEntity
{
    public ulong UserId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public ushort CatTargetYear { get; set; } = 2026;
    public DateTime? CatExamDate { get; set; }
    public string PreparationLevel { get; set; } = "Beginner";
    public string PreparationMode { get; set; } = "SelfStudy";
    public string Timezone { get; set; } = "Asia/Kolkata";
}

