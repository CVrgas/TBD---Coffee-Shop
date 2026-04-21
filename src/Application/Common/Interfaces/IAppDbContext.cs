using Domain.Catalog;
using Domain.Inventory;
using Domain.Orders.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

public interface IAppDbContext
{
    #region Sets

    DbSet<Product> Products {get; set;}
    DbSet<ProductCategory> ProductCategories {get; set;}
    DbSet<StockItem> StockItems {get; set;}
    DbSet<StockMovement> StockMovements {get; set;}
    DbSet<Domain.Users.Entities.User> Users {get; set;}
    DbSet<Order> Orders {get; set;}
    DbSet<OrderItem> OrderItems {get; set;}
    DbSet<PaymentRecord> PaymentRecords {get; set;}

    #endregion

    #region methods

    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);

    #endregion
}