using Microsoft.EntityFrameworkCore.Storage;
using Application.Common.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Persistence;

public class UnitOfWork(MyDbContext dbContext) : IUnitOfWork
{
    
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await dbContext.SaveChangesAsync(ct);
    }
    
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await action(ct);

                await dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    public void ClearChangeTracker()
    {
        dbContext.ChangeTracker.Clear();
    }
}