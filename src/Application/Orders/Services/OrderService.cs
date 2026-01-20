using System.Linq.Expressions;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces.User;
using Application.Inventory.Abstractions;
using Application.Orders.Dtos;
using Domain.Base;
using Domain.Catalog;
using Domain.Orders;
using Domain.User;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;

namespace Application.Orders.Services;

public class OrderService(
    ResiliencePipelineProvider<string> pipeline,
    IRepository<Order, int> repository,
    IUnitOfWork uOw,
    IInventoryRepository invRepository,
    IServiceScopeFactory scopeFactory,
    ICurrentUserService userContext)
    : IOrderService
{
    private const decimal TaxPercentage = 0.18m; // TODO: Get today's tax percentage instead of static.
    private readonly ResiliencePipeline _pipeline = pipeline.GetPipeline("default-retry-pipeline");
    private int CurrentUserId => userContext.RequiredUserId;
    private UserRole UserRole => userContext.UserRole;
    
    public async Task<Envelope<string>> AddOrderAsync(OrderCreationDto order, CancellationToken ct = default)
    {
        var productIds = order.Items.Select(p => p.ProductId).ToHashSet();
        
        return await _pipeline.ExecuteAsync(async _ =>
        {
            using var scope = scopeFactory.CreateScope();
            var scopeUoW = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var scopeRepository = scope.ServiceProvider.GetRequiredService<IRepository<Order, int>>();
            var productRepository = scope.ServiceProvider.GetRequiredService<IRepository<Product, int>>();
            var scopeInvRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
            
            var products = await productRepository
                .GetAllAsync(p => productIds.Contains(p.Id), asNoTracking: false, ct: ct);

            var listedResult = products.ToDictionary(p => p.Id);
        
            var missingProduct = productIds.Except(listedResult.Keys).ToList();
            if(missingProduct.Count != 0) 
                return Envelope<string>.NotFound($"Product with ID {missingProduct.First()} not found.");
            
            var newOrder = new Order(CurrentUserId, order.Currency, TaxPercentage);
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
            
            var reserveResult = await scopeInvRepository.ReserveStock(stockMovement, CurrentUserId.ToString(), newOrder.OrderNumber, ct);
            if (!reserveResult) return Envelope<string>.BadRequest("Not enough stock.").WithError("Stock", "Not enough stock.");

            await scopeRepository.Create(newOrder);
            await scopeUoW.SaveChangesAsync(ct);
            return Envelope<string>.Ok(newOrder.OrderNumber);
        }, ct);
    }

    public async Task<Envelope<IEnumerable<OrderDto>>> GetOrdersAsync(int? userId, CancellationToken ct = default)
    {
        Expression<Func<Order, bool>> predicate = order => 
            order.UserId == CurrentUserId 
            && order.Status != OrderStatus.Unspecified; // Return user's orders
        
        if (UserRole is UserRole.Admin or UserRole.Staff && userId.HasValue) predicate = o => o.UserId == userId; // if admin or staff and has userId, returns user order.

        var orders = await repository.ListAsync(selector: _getOrderProjection, predicate: predicate, ct: ct);
        return Envelope<IEnumerable<OrderDto>>.Ok(orders);
    }
    
    public async Task<Envelope> CancelOrderAsync(string orderNumber, CancellationToken ct = default)
    {
        var order = await repository.GetAsync(o => 
            o.OrderNumber == orderNumber
            && o.Status == OrderStatus.Pending,
            asNoTracking: false,
            ct: ct);
        
        if(order is null) return Envelope.NotFound();
        if(order.UserId != CurrentUserId && userContext.UserRole != UserRole.Admin) return Envelope.Forbidden();

        order.UpdateStatus(OrderStatus.Cancelled);
        await invRepository.RestoreStock(order.OrderNumber, CurrentUserId.ToString(), ct);
        await uOw.SaveChangesAsync(ct);
        return Envelope.Ok();
    }

    #region Helpers

    private readonly Expression<Func<Order, OrderDto>> _getOrderProjection = o => new OrderDto(
        o.OrderNumber,
        o.Status.ToString(),
        o.Subtotal,
        o.Tax,
        o.Total,
        o.Currency.Code,
        o.CreatedAt,
        o.UpdatedAt,
        o.OrderItems.Select( oi => new OrderItemDto(oi.ProductId, oi.Qty)).ToList()
        );

    #endregion
}