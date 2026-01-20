using Domain.Base;
using Domain.Orders;

namespace Domain.User;

public class User : Entity<int>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public string SecurityStamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IEnumerable<Order> Orders { get; set; } = new List<Order>();
}

public enum UserRole { Anonymous, Customer, Staff, Admin}