using Domain.Base.Events;

namespace Domain.Base.Entities;

public abstract class Entity<TKey> : IEntity<TKey> where TKey : notnull
{
    public TKey Id { get; protected set; } = default!;

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj) =>
        obj is Entity<TKey> other && Id.Equals(other.Id);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right) =>
        !(left == right);
}

public abstract class EntityWithRowVersion<TKey> : Entity<TKey>, IHasRowVersion where TKey : notnull
{
    public byte[] RowVersion { get; protected set; } = null!;
}