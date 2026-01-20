namespace Domain.Base;

public abstract class Entity<TKey> : IEntity<TKey> where TKey : notnull
{
    public TKey Id { get; protected set; } = default!;
}

public abstract class EntityWithRowVersion<TKey> : Entity<TKey>, IHasRowVersion where TKey : notnull
{
    public byte[] RowVersion { get; set; } = null!;
}