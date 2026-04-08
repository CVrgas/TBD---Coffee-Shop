using Domain.Base;

namespace Domain.User;

public sealed class User : Entity<int>
{
    public string FirstName { get; private set; } 
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public PasswordHash PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public string SecurityStamp { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    
    private User()
    {
        FirstName = null!;
        LastName = null!;
        Email = null!;
        SecurityStamp = null!;
        PasswordHash = null!;
    }
    
    public static User CreateCustomer(string firstName, string lastName, string email, PasswordHash passwordHash)
    {
        if(string.IsNullOrWhiteSpace(firstName)) throw new ArgumentNullException(nameof(firstName), "First name is required.");
        if(string.IsNullOrWhiteSpace(lastName)) throw new ArgumentNullException(nameof(lastName), "Last name is required.");
        
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email.ToLowerInvariant(),
            Role = UserRole.Customer,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString(),
            PasswordHash = passwordHash,
        };
        return user;
    }
    
    public static User CreateStaff(string firstName, string lastName, string email, UserRole role, PasswordHash passwordHash)
    {
        if (role is UserRole.Customer or UserRole.Anonymous)
            throw new ArgumentException("Use specific method for customers");

        var user = CreateCustomer(firstName, lastName, email, passwordHash);
        user.Role = role;
        return user;
    }

    public void UpdatePassword(PasswordHash passwordHash)
    {
        if (PasswordHash.Value == passwordHash.Value) throw new ArgumentException("New password cannot be the same as the old one");
        
        PasswordHash = passwordHash;
        RegenerateSecurityStamp();
        MarkAsUpdated();
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        RegenerateSecurityStamp();
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }
    
    private void RegenerateSecurityStamp()
    {
        SecurityStamp = Guid.NewGuid().ToString();
    }

    private void MarkAsUpdated()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum UserRole { Anonymous, Customer, Staff, Admin }