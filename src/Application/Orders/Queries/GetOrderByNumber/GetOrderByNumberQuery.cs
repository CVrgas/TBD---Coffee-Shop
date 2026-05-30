using Application.Common.Abstractions.Envelope;
using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Queries.GetOrderByNumber;

public sealed record GetOrderByNumberQuery(string OrderNumber) : IRequest<Envelope<OrderDto>>;