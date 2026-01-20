using Domain.Base;

namespace Application.Common.Abstractions.Persistence.Repository;

public interface IRepository<T, TKey> : IReadRepository<T, TKey>  where T : class, IEntity<TKey>
{
    Task<T?> Create(T entity);
    Task<IEnumerable<T>> CreateRange(IEnumerable<T> entities);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    Task Delete(TKey id);
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);
    void AttachWithRowVersion<TWith>(TWith entity, byte[] rowVersion)
        where TWith : class, T, IHasRowVersion;
}