using Domain.Base;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public abstract class RepositoryBase<T, TKey>(ApplicationDbContext dbContext)
    where T : class, IEntity<TKey>
{
    protected readonly ApplicationDbContext DbContext = dbContext;
    protected readonly DbSet<T> DbSet = dbContext.Set<T>();

    public void AttachWithRowVersion<TWithToken>(TWithToken entity, byte[] rowVersion)
        where TWithToken : class, T, IHasRowVersion
    {
        var entry = DbContext.Entry(entity);
        if (entry.State == EntityState.Detached) DbSet.Attach(entity);
        
        entry.Property(e => e.RowVersion).OriginalValue = rowVersion;
        entry.Property(e => e.RowVersion).IsModified = false;
    }
}