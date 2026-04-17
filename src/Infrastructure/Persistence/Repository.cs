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
    public async Task<IList<T>> ListAsync(ISpecification<T>? spec =  null, bool asNoTracking = true, CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking() : DbSet.AsQueryable();
        if (spec == null) return await query.ToListAsync(cancellationToken: ct);
        var specificationResult = Specifications.SpecificationEvaluator.GetQuery(query, spec);
        return await specificationResult.ToListAsync(cancellationToken: ct);
    }
    public async Task<int> CountAsync(ISpecification<T>? spec = null, bool asNoTracking = true, CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking() : DbSet.AsQueryable();
        if (spec == null) return await query.CountAsync(ct);
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
    public Task<bool> ExistsAsync(TKey id, bool asNoTracking = true, CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking().AsQueryable() : DbSet.AsQueryable();
        return query.AnyAsync(e => e.Id!.Equals(id), ct);
    }
    public async Task<bool> ExistsAsync(ISpecification<T> specification, bool asNoTracking = true, CancellationToken ct = default)
    {
        var query = asNoTracking ? DbSet.AsNoTracking().AsQueryable() : DbSet.AsQueryable();
        var specificationResult = Specifications.SpecificationEvaluator.GetQuery(query, specification);
        return await specificationResult.AnyAsync(cancellationToken: ct);
    }

    #endregion
}