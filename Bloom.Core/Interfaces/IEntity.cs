namespace Bloom.Core.Interfaces;

/// <summary>
/// Base contract for entities with strongly-typed identifiers.
/// </summary>
/// <typeparam name="TId">The type of the primary key.</typeparam>
public interface IEntity<TId>
{
    TId Id { get; set; }
}

/// <summary>
/// Contract for entities requiring auditing timestamps.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    DateTime UpdatedAtUtc { get; set; }
}

/// <summary>
/// Contract for entities supporting soft deletion.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}

