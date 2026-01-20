using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.Property(sm => sm.Reason)
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();
            
        builder.Property(sm => sm.Delta)
            .HasPrecision(18, 2)
            .IsRequired();
            
        builder.HasOne(sm => sm.Product)
            .WithMany(p => p.StockMovements)
            .HasForeignKey(sm => sm.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(sm => sm.StockItem)
            .WithMany(si => si.StockMovements)
            .HasForeignKey(sm => sm.StockItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}