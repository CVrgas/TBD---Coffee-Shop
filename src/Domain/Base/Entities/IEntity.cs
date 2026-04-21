namespace Domain.Base.Entities;

public interface IEntity<TKey>
{
    TKey Id { get; } 
}