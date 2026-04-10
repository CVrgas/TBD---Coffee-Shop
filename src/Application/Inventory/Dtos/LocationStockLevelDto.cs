namespace Application.Inventory.Dtos;

public record LocationStockLevelDto
{
    public int LocationId { get; init; }
    public int AvailableQuantity { get; init; }
}