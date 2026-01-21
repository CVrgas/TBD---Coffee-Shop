using Domain.Base;

namespace Application.Catalog.Dtos;

public record ProductUpdatePrice(int Id, decimal Price, string Currency, byte[] RowVersion)
{
    public CurrencyCode FormatCurrency { get; } = new CurrencyCode(Currency);
};