using Application.Common.Abstractions.Envelope;
using MediatR;

namespace Application.Catalog.Commands.Rate;

public sealed record RateProductCommand(int ProductId, int Rate) : IRequest<Envelope>;