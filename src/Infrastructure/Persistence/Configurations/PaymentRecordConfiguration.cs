using Domain.Base;
using Domain.Catalog;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PaymentRecordConfiguration : IEntityTypeConfiguration<PaymentRecord>
{
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        builder.ToTable("PaymentRecords");
        
        builder.HasKey(pr => pr.Id);
        
        builder.Property(pr => pr.IntentId)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(pr => pr.IntentId)
            .IsUnique();
        
        builder.Property(pr => pr.Provider)
            .HasConversion<string>()
            .HasMaxLength(20);
        
        builder.Property(pr => pr.Currency)
            .HasConversion(c => c.Code, val => new CurrencyCode(val))
            .HasMaxLength(3)
            .IsRequired();
        
        builder.Property(pr => pr.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(pr => pr.Amount).HasPrecision(18, 2);
        
        builder.Property(pr => pr.RowVersion).IsRowVersion();
    }
}