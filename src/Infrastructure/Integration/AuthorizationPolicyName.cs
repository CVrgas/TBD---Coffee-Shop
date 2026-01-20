using Domain.User;

namespace Infrastructure.Integration;

public static class AuthPolicyName
{
    public static string ElevatedRights => "ElevatedRights";
    public static string RegisteredUser => "RegisterUser";
    public static string Customer => nameof(UserRole.Customer);
    public static string Staff => nameof(UserRole.Staff);
    public static string Admin => nameof(UserRole.Admin);
}
public enum AuthorizationPolicyName
{
    ElevatedRights,
    RegisterUser
}