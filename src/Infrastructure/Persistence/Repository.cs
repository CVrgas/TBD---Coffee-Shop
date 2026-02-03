using System.Linq.Expressions;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces;
using Domain.Base;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class Repository<T, TKey>(ApplicationDbContext dbContext) : RepositoryBase<T, TKey>(dbContext), IRepository<T, TKey>
    where T : class, IEntity<TKey>
{

    #region Write
    
    public async Task<T?> Create(T entity)
    {
        await DbSet.AddAsync(entity);
        return entity;
    }

    public async Task<IEnumerable<T>> CreateRange(IEnumerable<T> entities)
    {
        var enumerable = entities as T[] ?? entities.ToArray();
        await DbSet.AddRangeAsync(enumerable);
        return enumerable;
    }

    public void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public void UpdateRange(IEnumerable<T> entities)
    {
        DbSet.UpdateRange(entities);
    }

    public async Task Delete(TKey id)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity != null) DbSet.Remove(entity);
    }

    public void Delete(T entity)
    {
        DbSet.Remove(entity);
    }

    public void DeleteRange(IEnumerable<T> entities)
    {
        var enumerable = entities as T[] ?? entities.ToArray();
        DbSet.RemoveRange(enumerable);
    }
    #endregion

    #region Read
    
    // Newest version

    public async Task<T?> GetByIdAsync(TKey id, bool asNoTracking = true, CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking() : DbSet;
        return await query.FirstOrDefaultAsync(e => e.Id!.Equals(id), cancellationToken: ct);
    }
    public async Task<T?> GetAsync(ISpecification<T> spec, bool asNoTracking = true, CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking().AsQueryable() : DbSet.AsQueryable();
        var specificationResult = Specifications.SpecificationEvaluator.GetQuery(query, spec);
        return await specificationResult.FirstOrDefaultAsync(cancellationToken: ct);
    }
    public async Task<IEnumerable<T>> ListAsync(ISpecification<T> spec, bool asNoTracking = true, CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking() : DbSet.AsQueryable();
        var specificationResult = Specifications.SpecificationEvaluator.GetQuery(query, spec);
        return await specificationResult.ToListAsync(cancellationToken: ct);
    }

    public async Task<IEnumerable<TProjection>> ListAsync<TProjection>(ISpecification<T> spec,
        Expression<Func<T, TProjection>> selector, CancellationToken ct = default)
    {
        var query = DbSet.AsNoTracking();
        var specificationResult = Specifications.SpecificationEvaluator.GetQuery(query, spec);
        return await specificationResult.Select(selector).ToListAsync(cancellationToken: ct);
    }
    public async Task<int> CountAsync(ISpecification<T> spec, bool asNoTracking = true, CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking() : DbSet.AsQueryable();
        var specificationResult = Specifications.SpecificationEvaluator.GetQuery(query, spec);
        return await specificationResult.CountAsync(ct);
    }
    public async Task<TProjection?> GetAsync<TProjection>(ISpecification<T> spec,
        Expression<Func<T, TProjection>> selector, bool asNoTracking = true, CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking() : DbSet.AsQueryable();
        var specificationResult = Specifications.SpecificationEvaluator.GetQuery(query, spec);
        return await specificationResult.Select(selector).FirstOrDefaultAsync(cancellationToken: ct);
    }

    public async Task<Paginated<T>> PaginatedAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        var query = DbSet.AsNoTracking();
        var specificationResult = Specifications.SpecificationEvaluator.GetQuery(query, spec);
        var items  = await specificationResult.ToListAsync(ct);
        return new Paginated<T>(items, TotalCount: 0, 0, spec.Take!.Value);
    }
    
    // Old version
    public async Task<T?> GetByIdAsync(TKey id, Func<IQueryable<T>, IQueryable<T>>? includes = null, bool asNoTracking = true, CancellationToken ct = default)
    {
        var baseQuery = asNoTracking ? DbSet.AsNoTracking().AsQueryable() : DbSet.AsQueryable();
        var query = includes?.Invoke(baseQuery) ?? baseQuery;
        return await query.FirstOrDefaultAsync(e => e.Id!.Equals(id), ct);
    }

    public async Task<TProjection?> GetByIdAsync<TProjection>(TKey id, Expression<Func<T, TProjection>> selector, bool asNoTracking = true, CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking().AsQueryable() : DbSet.AsQueryable();
        return await query.Where(e => e.Id!.Equals(id)).Select(selector).FirstOrDefaultAsync(cancellationToken: ct);
    }

    public Task<T?> GetAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? includes = null, bool asNoTracking = true, CancellationToken ct = default)
    {
        var baseQuery = asNoTracking ? DbSet.AsNoTracking().AsQueryable() : DbSet.AsQueryable();
        var query = includes?.Invoke(baseQuery) ?? baseQuery;
        return query.FirstOrDefaultAsync(predicate, ct);
    }
    
    public Task<TProjection?> GetAsync<TProjection>(Expression<Func<T, bool>> predicate, Expression<Func<T, TProjection>> selector, bool asNoTracking = true,
        CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking().AsQueryable() : DbSet.AsQueryable();
        return query.Where(predicate).Select(selector).FirstOrDefaultAsync(cancellationToken: ct);
    }

    public async Task<IEnumerable<T>> ListAsync(
        Expression<Func<T, bool>>? predicate = null, 
        SortOption? sort = null, 
        Func<IQueryable<T>, IQueryable<T>>? includes = null, 
        int? take = null,
        bool asNoTracking = true,
        CancellationToken ct = default)
    {
        var baseQuery = asNoTracking ? DbSet.AsNoTracking().AsQueryable() : DbSet.AsQueryable();
        
        var query = includes?.Invoke(baseQuery) ?? baseQuery;
        
        if (predicate is not null) query = query.Where(predicate);

        if (sort is not null) query = query.ApplySort(sort);

        if (take is not null) query = query.Take(take.Value);
        
        return await query.ToListAsync(ct);
    }

    public async Task<IEnumerable<TProjection>> ListAsync<TProjection>(
        Expression<Func<T, TProjection>> selector, 
        Expression<Func<T, bool>>? predicate = null, 
        SortOption? sort = null, 
        int? take = null,
        bool asNoTracking = true,
        CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking().AsQueryable() : DbSet.AsQueryable();

        if (predicate is not null) query = query.Where(predicate);

        if (sort is not null) query = query.ApplySort(sort);
        
        return await query.Select(selector).ToListAsync(ct);
    }

    public async Task<Paginated<T>> GetPaginatedAsync(
        int pageIndex, 
        int pageSize, 
        Expression<Func<T, bool>>? predicate = null, 
        SortOption? sort = null,
        Func<IQueryable<T>, IQueryable<T>>? includes = null,
        bool asNoTracking = true,
        CancellationToken ct = default)
    {
        var baseQuery = asNoTracking ? DbSet.AsNoTracking().AsQueryable() : DbSet.AsQueryable();
        var query = includes?.Invoke(baseQuery) ?? baseQuery;

        if(predicate is not null)  query = query.Where(predicate);
        
        var totalCount = await query.CountAsync(ct);

        if (sort is not null) query = query.ApplySort(sort);
        
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new Paginated<T>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<Paginated<TProjection>> GetPaginatedAsync<TProjection>(
        int pageIndex, 
        int pageSize, 
        Expression<Func<T, TProjection>> selector, 
        Expression<Func<T, bool>>? predicate = null,
        SortOption? sort = null,
        bool asNoTracking = true,
        CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking().AsQueryable() : DbSet.AsQueryable();
        
        var skip = (pageIndex - 1) * pageSize;
        
        if ( predicate is not null) query = query.Where(predicate);

        if (sort is not null) query = query.ApplySort(sort);
        
        var totalCount = await query.Where(predicate ?? (p => true)).CountAsync(ct);
        
        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync(ct);
        
        return new Paginated<TProjection>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, bool asNoTracking = true, CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking().AsQueryable() : DbSet.AsQueryable();
        return await query.CountAsync(predicate ?? (p => true), ct);
    }

    public Task<bool> ExistsAsync(TKey id, bool asNoTracking = true, CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking().AsQueryable() : DbSet.AsQueryable();
        return query.AnyAsync(e => e.Id!.Equals(id), ct);
    }

    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true, CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking().AsQueryable() : DbSet.AsQueryable();
        return query.AnyAsync(predicate, ct);
    }

    #endregion
}