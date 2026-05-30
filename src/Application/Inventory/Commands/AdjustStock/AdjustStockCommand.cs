using Application.Common.Abstractions.Envelope;
using Domain.Base.Enum;
using MediatR;

namespace Application.Inventory.Commands.AdjustStock;

public sealed record AdjustStockCommand(int ProductId, int Delta, StockMovementReason Reason, string? ReferenceId) : IRequest<Envelope>;