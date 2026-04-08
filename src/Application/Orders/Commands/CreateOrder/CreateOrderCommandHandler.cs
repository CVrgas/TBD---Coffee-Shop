using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces.User;
using Application.Inventory.Abstractions;
using Application.Orders.Specifications;
using Domain.Catalog;
using Domain.Orders;
using MediatR;

namespace Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler(
    IRepository<Order, int> repository,
    IRepository<Product, int> productRepository,
    IInventoryRepository invRepository,
    IUnitOfWork uOw,
    ICurrentUserService userContext) : IRequestHandler<CreateOrderCommand, Envelope<string>>
{
    private const decimal TaxPercentage = 0.18m; // TODO: Get today's tax percentage instead of static.
    private int CurrentUserId => userContext.RequiredUserId;
    
    public async Task<Envelope<string>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var productIds = request.Items.Select(p => p.ProductId).ToHashSet();
        
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var products = await productRepository.ListAsync(new ProductByIds(productIds), asNoTracking: false, ct: cancellationToken);

            var listedResult = products.ToDictionary(p => p.Id);
            var missingProduct = productIds.Except(listedResult.Keys).ToList();
            
            if(missingProduct.Count != 0) 
                return Envelope<string>.NotFound($"Product with ID {missingProduct.First()} not found.");
            
            var newOrder = Order.Create(CurrentUserId, request.Currency, TaxPercentage);
            var stockMovement = new Dictionary<int, int>();
            
            foreach (var item in request.Items)
            {
                var existing = listedResult[item.ProductId];
                
                newOrder.AddItem(existing, item.Quantity);
                
                if (!stockMovement.TryAdd(existing.Id, item.Quantity))
                {
                    stockMovement[existing.Id] += item.Quantity;
                }
            }
            
            var reserveResult = await invRepository.ReserveStock(stockMovement, CurrentUserId.ToString(), newOrder.OrderNumber, cancellationToken);
            
            if (!reserveResult) 
                return Envelope<string>.BadRequest("Not enough stock.").WithError("Stock", "Not enough stock.");

            await repository.Create(newOrder);
            return Envelope<string>.Ok(newOrder.OrderNumber);
        }, cancellationToken);
    }
}