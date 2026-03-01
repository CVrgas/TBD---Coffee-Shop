using System.Linq.Expressions;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces;
using Application.Common.Interfaces.User;
using Application.Inventory.Abstractions;
using Application.Orders.Dtos;
using Domain.Base;
using Domain.Catalog;
using Domain.Orders;
using Domain.User;

namespace Application.Orders.Services;

public class OrderService(
    IRepository<Order, int> repository,
    IRepository<Product, int> productRepository,
    IUnitOfWork uOw,
    IInventoryRepository invRepository,
    ICurrentUserService userContext)
    : IOrderService
{
    private const decimal TaxPercentage = 0.18m; // TODO: Get today's tax percentage instead of static.
    private int CurrentUserId => userContext.RequiredUserId;
    private UserRole UserRole => userContext.UserRole;
    
    public async Task<Envelope<string>> AddOrderAsync(OrderCreationDto order, CancellationToken ct = default)
    {
        var productIds = order.Items.Select(p => p.ProductId).ToHashSet();
        
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var products = await productRepository.ListAsync(new ProductByIds(productIds), asNoTracking: false, ct: ct);

            var listedResult = products.ToDictionary(p => p.Id);
            var missingProduct = productIds.Except(listedResult.Keys).ToList();
            
            if(missingProduct.Count != 0) 
                return Envelope<string>.NotFound($"Product with ID {missingProduct.First()} not found.");
            
            var newOrder = Order.Create(CurrentUserId, order.Currency, TaxPercentage);
            var stockMovement = new Dictionary<int, int>();
            
            foreach (var item in order.Items)
            {
                var existing = listedResult[item.ProductId];
                
                newOrder.AddItem(existing, item.Quantity);
                
                if (!stockMovement.TryAdd(existing.Id, item.Quantity))
                {
                    stockMovement[existing.Id] += item.Quantity;
                }
            }
            
            var reserveResult = await invRepository.ReserveStock(stockMovement, CurrentUserId.ToString(), newOrder.OrderNumber, ct);
            
            if (!reserveResult) 
                return Envelope<string>.BadRequest("Not enough stock.").WithError("Stock", "Not enough stock.");

            await repository.Create(newOrder);
            return Envelope<string>.Ok(newOrder.OrderNumber);
        }, ct);
    }
    
    public async Task<Envelope> CancelOrderAsync(string orderNumber, CancellationToken ct = default)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var order = await repository.GetAsync(new GetOrderCancelSpec(orderNumber), asNoTracking: false, ct: ct);

            if (order is null) return Envelope.NotFound();
            if (order.UserId != CurrentUserId && userContext.UserRole != UserRole.Admin) return Envelope.Forbidden();

            order.UpdateStatus(OrderStatus.Cancelled);
            await invRepository.RestoreStock(order.OrderNumber, CurrentUserId.ToString(), ct);
            return Envelope.Ok();
        }, ct);
    }
}

internal class ProductByIds(IEnumerable<int> productIds) : Specification<Product>(p => productIds.Contains(p.Id));
internal class GetOrderCancelSpec(string orderNumber) : Specification<Order>(order => order.OrderNumber == orderNumber && order.Status == OrderStatus.Pending);