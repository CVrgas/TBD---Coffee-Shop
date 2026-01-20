namespace Application.Common.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
    //Task BeginTransactionAsync();
    //Task CommitAsync();
    //Task RollbackAsync();
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);

    void ClearChangeTracker();
}
