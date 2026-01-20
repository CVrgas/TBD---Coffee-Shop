namespace Domain.Base;

public interface IEntity<TKey>
{
    TKey Id { get; } 
}