using Domain.Base;

namespace Domain.User;

public sealed class User : Entity<int>
{
    public string FirstName { get; private set; } 
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public string SecurityStamp { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    
    private User() { }
    
    public static User CreateCustomer(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email required");
        
        return new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email.ToLowerInvariant(),
            Role = UserRole.Customer,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }
    
    public static User CreateStaff(string firstName, string lastName, string email, UserRole role)
    {
        if (role is UserRole.Customer or UserRole.Anonymous)
            throw new ArgumentException("Use specific method for customers");

        var user = CreateCustomer(firstName, lastName, email);
        user.Role = role;
        return user;
    }


    public void SetPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Invalid hash");
        
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