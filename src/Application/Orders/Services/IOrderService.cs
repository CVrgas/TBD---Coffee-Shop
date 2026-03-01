using Application.Common.Abstractions.Envelope;
using Application.Orders.Dtos;
using Domain.Orders;

namespace Application.Orders.Services;

public interface IOrderService
{
    Task<Envelope<string>> AddOrderAsync(OrderCreationDto order, CancellationToken ct = default);
    Task<Envelope> CancelOrderAsync(string orderNumber, CancellationToken ct = default);
    
}