using Application.Common.Abstractions.Envelope;
using MediatR;

namespace Application.Catalog.Commands.ToggleStatus;

public sealed record ToggleStatusCommand(int ProductId, bool? State) : IRequest<Envelope>;