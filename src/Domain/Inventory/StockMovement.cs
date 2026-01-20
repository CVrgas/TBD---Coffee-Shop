using Domain.Base;
using Domain.Catalog;

namespace Domain.Inventory;

public class StockMovement : Entity<int>
{
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? ReferenceId { get; set; }
    public StockMovementReason Reason { get; set; }
    public int Delta { get; set; }
    
    public int? LocationId { get; set; }
    
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;

}