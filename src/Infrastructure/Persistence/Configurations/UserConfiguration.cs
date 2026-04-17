using Domain.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
            
        builder.Property(u => u.Email)
            .HasConversion(e => e.Value, v => new EmailAddress(v))
            .HasMaxLength(255)
            .IsRequired();
            
        builder.HasIndex(u => u.Email)
            .IsUnique();
            
        builder.Property(u => u.PasswordHash)
            .HasConversion(h => h.Value, v => new PasswordHash(v))
            .HasMaxLength(500)
            .IsRequired();
            
        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(u => u.SecurityStamp)
            .IsRequired();
            
        builder.Property(u => u.Role)
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);
    }
}