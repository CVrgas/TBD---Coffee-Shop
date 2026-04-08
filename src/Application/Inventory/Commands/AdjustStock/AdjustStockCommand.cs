using Application.Common.Abstractions.Envelope;
using Domain.Base;
using MediatR;

namespace Application.Inventory.Commands.AdjustStock;

public sealed record AdjustStockCommand(int ProductId, int Delta, StockMovementReason Reason, byte[] RowVersion, string? ReferenceId) : IRequest<Envelope>;