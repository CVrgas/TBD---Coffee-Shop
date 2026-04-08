using Application.Common.Abstractions.Envelope;
using MediatR;

namespace Application.Orders.Commands.CancelOrder;

public sealed record CancelOrderCommand(string OrderNumber) : IRequest<Envelope>;