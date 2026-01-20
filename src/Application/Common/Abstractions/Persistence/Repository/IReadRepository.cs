using System.Linq.Expressions;
using Application.Common.Interfaces;
using Domain.Base;

namespace Application.Common.Abstractions.Persistence.Repository;

public interface IReadRepository<T, TKey> where T : class, IEntity<TKey>
{
    Task<T?> GetByIdAsync(TKey id, bool asNoTracking = true, CancellationToken ct = default); // lastest version.
    Task<T?> GetAsync(ISpecification<T> spec, bool asNoTracking = true, CancellationToken ct = default); // lastest version.
    Task<IEnumerable<T>> ListAsync(ISpecification<T> spec, bool asNoTracking = true, CancellationToken ct = default);
    Task<int> CountAsync(ISpecification<T> spec, bool asNoTracking = true, CancellationToken ct = default);
    Task<TProjection?> GetAsync<TProjection>(ISpecification<T> spec, Expression<Func<T, TProjection>> selector, bool asNoTracking = true, CancellationToken ct = default); // lastest version.
    
    // oldest version
    Task<T?> GetByIdAsync(TKey id, Func<IQueryable<T>, IQueryable<T>>? includes = null, bool asNoTracking = true, CancellationToken ct = default);
    Task<TProjection?> GetByIdAsync<TProjection>(TKey id, Expression<Func<T, TProjection>> selector, bool asNoTracking = true, CancellationToken ct = default );
    //Task<T?> GetAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? includes = null, bool asNoTracking = true, CancellationToken ct = default);
    Task<TProjection?> GetAsync<TProjection>(Expression<Func<T, bool>> predicate, Expression<Func<T, TProjection>> selector, bool asNoTracking = true, CancellationToken ct = default);
    Task<IEnumerable<T>> ListAsync(
        Expression<Func<T, bool>>? predicate = null, 
        SortOption? sort = null, 
        Func<IQueryable<T>, IQueryable<T>>? includes = null, 
        int? take = null, 
        bool asNoTracking = true,
        CancellationToken ct = default);
        
    Task<IEnumerable<TProjection>> ListAsync<TProjection>(
        Expression<Func<T, TProjection>> selector,
        Expression<Func<T, bool>>? predicate = null, 
        SortOption? sort = null,
        int? take = null, 
        bool asNoTracking = true,
        CancellationToken ct = default);
    Task<Paginated<T>> GetPaginatedAsync(
        int pageIndex, 
        int pageSize, 
        Expression<Func<T, bool>>? predicate = null, 
        SortOption? sort = null, 
        Func<IQueryable<T>, IQueryable<T>>? includes = null, 
        bool asNoTracking = true,
        CancellationToken ct = default);
    Task<Paginated<TProjection>> GetPaginatedAsync<TProjection>(
        int pageIndex, 
        int pageSize,
        Expression<Func<T, TProjection>> selector,
        Expression<Func<T, bool>>? predicate = null, 
        SortOption? sort = null, 
        bool asNoTracking = true,
        CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, bool asNoTracking = true, CancellationToken ct = default);
    Task<bool> ExistsAsync(TKey id, bool asNoTracking = true, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true, CancellationToken ct = default);
}