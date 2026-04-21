using Application.Common.Abstractions.Envelope;
using Application.Inventory.Dtos;
using MediatR;

namespace Application.Inventory.Queries.GetStockLevel;

public sealed record GetStockLevelQuery(int ProductId) : IRequest<Envelope<StockLevelDto>>;