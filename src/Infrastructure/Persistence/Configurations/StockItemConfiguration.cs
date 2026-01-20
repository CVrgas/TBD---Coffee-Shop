using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.HasKey(si => si.Id);
            
        builder.Property(si => si.ProductId)
            .IsRequired(); 
            
        builder.Property(si => si.QuantityOnHand)
            .HasPrecision(18, 2)
            .IsRequired();
            
        builder.Property(si => si.ReorderLevel)
            .HasPrecision(18, 2)
            .IsRequired();
            
        builder.Property(si => si.ReservedQuantity)
            .HasPrecision(18, 2);
            
        builder.Property(si => si.IsActive)
            .HasDefaultValue(true);
            
        builder.Property(si => si.RowVersion)
            .IsRowVersion();
            
        builder.Property(si => si.LastMovementAt)
            .IsRequired();
            
        builder.HasOne(si => si.Product)
            .WithMany(p => p.StockItems)
            .HasForeignKey(si => si.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasIndex(si => new { si.ProductId, si.LocationId})
            .IsUnique();
    }
}