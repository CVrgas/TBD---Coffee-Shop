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
            .IsRequired();
    }
}