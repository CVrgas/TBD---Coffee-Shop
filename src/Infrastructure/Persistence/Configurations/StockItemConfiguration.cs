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
        
        builder.Property(si => si.LocationId)
            .IsRequired();
            
        builder.Property(si => si.QuantityOnHand)
            .IsRequired();

        builder.HasMany(si => si.Movements)
            .WithOne(m => m.StockItem)
            .HasForeignKey(sm => sm.StockItemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Metadata.FindNavigation(nameof(StockItem.Movements))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        
        builder.Property(si => si.IsActive)
            .HasDefaultValue(true);
            
        builder.Property(si => si.RowVersion)
            .IsRowVersion();
            
        builder.HasIndex(si => new { si.ProductId, si.LocationId})
            .IsUnique();
    }
}