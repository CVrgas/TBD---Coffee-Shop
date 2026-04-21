using Domain.Users.Enum;

namespace Infrastructure.Integration;

public class AuthorizationPolicy(string name, List<UserRole> roles)
{
    public static readonly AuthorizationPolicy ElevatedRights = new("ElevatedRights", [UserRole.Admin, UserRole.Staff]);
    public static readonly AuthorizationPolicy RegisteredUser = new("RegisterUser", [UserRole.Customer, UserRole.Staff, UserRole.Admin]);
    public static readonly AuthorizationPolicy Customer = new(nameof(UserRole.Customer), [UserRole.Customer]);
    public static readonly AuthorizationPolicy Staff = new(nameof(UserRole.Staff), [UserRole.Staff]);
    public static readonly AuthorizationPolicy Admin = new(nameof(UserRole.Admin), [UserRole.Admin]);

    public string Name { get; } = name;
    private List<UserRole> Roles { get; } = roles;
    public IReadOnlyList<string> RoleNames => Roles.Select(r => r.ToString()).ToList();
    public static List<AuthorizationPolicy> ListOfPolicies() => [ ElevatedRights, RegisteredUser, Customer, Staff, Admin ];
}