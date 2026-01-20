using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.HasKey(p => p.Id);
            
        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);
            
        builder.Property(p => p.Name)
            .HasMaxLength(150)
            .IsRequired();
            
        builder.Property(p => p.Code)
            .HasMaxLength(20)
            .IsRequired();
            
        builder.Property(p => p.Slug)
            .HasMaxLength(155)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(255);
            
        builder.HasOne(c => c.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);
    }
}