using Application.Common.Abstractions.Envelope;
using Application.Inventory.Dtos;
using MediatR;

namespace Application.Inventory.Commands.GetStockLevel;

public sealed record GetStockLevelCommand(int ProductId) : IRequest<Envelope<StockLevelDto>>;