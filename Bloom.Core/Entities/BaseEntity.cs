using Bloom.Core.Interfaces;

namespace Bloom.Core.Entities;

/// <summary>
/// Abstract base entity for all persistent domain entities in Bloom.
/// </summary>
public abstract class BaseEntity : IEntity<ulong>, IAuditableEntity, ISoftDeletable
{
    public ulong Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}

