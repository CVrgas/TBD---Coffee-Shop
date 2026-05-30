using Application.Common.Abstractions.Envelope;
using Application.Orders.Dtos;
using Domain.Base;
using Domain.Base.ValueObjects;
using MediatR;

namespace Application.Orders.Commands.CreateOrder;

public sealed record CreateOrderCommand(string CurrencyCode, IList<OrderItemDto> Items) : IRequest<Envelope<string>>
{
    public CurrencyCode Currency => new (CurrencyCode);
}