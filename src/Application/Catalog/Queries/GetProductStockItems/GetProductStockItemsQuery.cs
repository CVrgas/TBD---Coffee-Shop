using Application.Common.Abstractions.Envelope;
using Application.Inventory.Dtos;
using MediatR;

namespace Application.Catalog.Queries.GetProductStockItems;

public sealed record GetProductStockItemsQuery(int ProductId) : IRequest<Envelope<List<StockItemDto>>>;
