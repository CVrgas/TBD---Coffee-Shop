using Domain.Base;
using Domain.Catalog;

namespace Application.Orders.Dtos;



public sealed record OrderCreationDto(string CurrencyCode, IList<OrderItemDto> Items)
{
    public CurrencyCode Currency => new (CurrencyCode);
};