using Application.Common.Abstractions.Envelope;
using Domain.Base;
using Domain.Base.ValueObjects;
using MediatR;

namespace Application.Catalog.Commands.UpdatePrice;

public sealed record UpdatePriceCommand(int Id, decimal Price, string Currency) : IRequest<Envelope>
{
    public CurrencyCode FormatCurrency => new(Currency);
};