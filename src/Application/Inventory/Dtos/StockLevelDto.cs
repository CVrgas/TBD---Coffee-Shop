namespace Application.Inventory.Dtos;

public record StockLevelDto
{
    public int ProductId { get; init; }
    public int QuantityInStock { get; init; }
    public int ReservedQuantity { get; init; }
    public int AvailableQuantity => QuantityInStock - ReservedQuantity;
    public List<LocationStockLevelDto> LocationStockLevels { get; init; } = [];
}