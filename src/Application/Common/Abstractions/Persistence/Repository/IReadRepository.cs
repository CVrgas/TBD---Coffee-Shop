using System.Linq.Expressions;
using Application.Common.Interfaces;
using Domain.Base;

namespace Application.Common.Abstractions.Persistence.Repository;

public interface IReadRepository<T, TKey> where T : class, IEntity<TKey>
{
    Task<T?> GetByIdAsync(TKey id, bool asNoTracking = true, CancellationToken ct = default);
    Task<T?> GetAsync(ISpecification<T> spec, bool asNoTracking = true, CancellationToken ct = default);
    Task<TProjection?> GetAsync<TProjection>(ISpecification<T> spec, Expression<Func<T, TProjection>> selector, bool asNoTracking = true, CancellationToken ct = default);
    Task<IList<T>> ListAsync(ISpecification<T>? spec = null, bool asNoTracking = true, CancellationToken ct = default);
    Task<int> CountAsync(ISpecification<T>? spec = null, bool asNoTracking = true, CancellationToken ct = default);
    Task<Paginated<T>> PaginatedAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<bool> ExistsAsync(ISpecification<T> specification, bool asNoTracking = true, CancellationToken ct = default);
}