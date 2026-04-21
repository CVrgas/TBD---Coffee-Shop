using Domain.Base.ValueObjects;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
            
        builder.Property(p => p.RowVersion)
            .IsRowVersion();
            
        builder.Property(p => p.Currency)
            .HasConversion(c => c.Code, c => new CurrencyCode(c))
            .HasDefaultValue(new CurrencyCode("USD"));

        builder.Property(p => p.Sku)
            .HasMaxLength(25)
            .IsRequired();
            
        builder.Property(p => p.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(p => p.Name)
            .IsUnique();

        builder.Property(p => p.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.IsOnSale)
            .HasDefaultValue(false);
            
        builder.Property(p => p.SalePrice)
            .HasPrecision(18, 2);

        builder.Property(p => p.Description)
            .HasMaxLength(255);
            
        builder.Property(p => p.ImageUrl)
            .HasMaxLength(500);
            
        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);
            
        builder.Property(p => p.RatingSum)
            .HasPrecision(18, 2);

        builder.Ignore(p => p.AverageRating);
            
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}