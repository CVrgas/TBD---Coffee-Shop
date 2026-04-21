using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Application.Common.Interfaces.User;
using Domain.Inventory;
using Domain.Orders;
using Domain.Orders.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler(IAppDbContext context, ICurrentUserService userContext) : IRequestHandler<CreateOrderCommand, Envelope<string>>
{
    private const decimal TaxPercentage = 0.18m; // TODO: Get today's tax percentage instead of static.
    private int CurrentUserId => userContext.RequiredUserId;
    
    public async Task<Envelope<string>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var productIds = request.Items.Select(p => p.ProductId).ToHashSet();
        
        return await context.ExecuteInTransactionAsync(async _ =>
        {
            var products = await context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => new
                {
                    p.Id,
                    p.Name,
                    Price = p.IsOnSale ? p.SalePrice ?? p.Price : p.Price,
                    p.Currency
                }, cancellationToken);
            
            var missingProduct = productIds.Except(products.Keys).ToList();
            
            if(missingProduct.Count != 0) 
                return Envelope<string>.NotFound($"Product with ID {missingProduct.First()} not found.");
            
            var newOrder = Order.Create(CurrentUserId, request.Currency, TaxPercentage);
            var stockMovement = new Dictionary<int, int>();
            
            foreach (var item in request.Items)
            {
                var existing = products[item.ProductId];
                
                newOrder.AddItem(existing.Id, existing.Name, existing.Price, item.Quantity);
                
                if (!stockMovement.TryAdd(existing.Id, item.Quantity))
                {
                    stockMovement[existing.Id] += item.Quantity;
                }
            }
            
            var reserveResult = await ReserveStock(stockMovement, CurrentUserId.ToString(), newOrder.OrderNumber, cancellationToken);
            
            if (!reserveResult) 
                return Envelope<string>.BadRequest("Not enough stock.").WithError("Stock", "Not enough stock.");

            await context.Orders.AddAsync(newOrder, cancellationToken);
            return Envelope<string>.Ok(newOrder.OrderNumber);
        }, cancellationToken);
    }
    
    // TODO (CQRS-MIN-001)
    private async Task<bool> ReserveStock(IDictionary<int, int> movements, string userId, string referenceId, CancellationToken ct = default)
    {
        var stockItems = await context.StockItems
            .Where(p => movements.Keys.Contains(p.ProductId))
            .ToListAsync(ct);
        
        if (!RequiredQtyLookup(stockItems, movements, out var adjustmentNeeded)) return false;

        foreach (var (stockId, quantity) in adjustmentNeeded)
        {
            var item = stockItems.First(s => s.Id == stockId);
            item.ReserveStock(quantity, referenceId);
        }

        return true;
    }
    
    private static bool RequiredQtyLookup(
        IList<StockItem> stockItems, 
        IDictionary<int, int> requiredQtyLookup, 
        out Dictionary<int, int> proposedMovements)
    {
        var stockLookup = stockItems.ToLookup(s => s.ProductId);
        var response = new Dictionary<int, int>();
        proposedMovements = new Dictionary<int, int>();

        foreach (var (productId, requiredQty) in requiredQtyLookup)
        {
            var available = stockLookup[productId];
            var current = 0;
            
            foreach (var item in available)
            {
                if (current >= requiredQty) break;
                
                var needed = requiredQty - current;
                var take = Math.Min(needed, item.AvailableQuantity);
                current += take;
                response[item.Id] = take;
            }

            if (current < requiredQty) return false;
        }
        
        proposedMovements = response;
        return true;
    }
}