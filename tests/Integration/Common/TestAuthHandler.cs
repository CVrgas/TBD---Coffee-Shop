using System.Security.Claims;
using System.Text.Encodings.Web;
using Domain.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integration.Common;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";
    public const string UserIdHeader = "X-Test-UserId";
    public const string UserRoleHeader = "X-Test-Role";
    public const string IdempotencyKey = "X-Idempotency-key";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.Request.Headers.TryGetValue(UserIdHeader, out var userIdValues)) return Task.FromResult(AuthenticateResult.NoResult());

        var userId = userIdValues.FirstOrDefault() ?? "1";
        
        Context.Request.Headers.TryGetValue(UserRoleHeader, out var roleValues);
        var role = roleValues.FirstOrDefault() ?? nameof(UserRole.Customer);
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.NameIdentifier, userId!),
            new(ClaimTypes.Role, role),
            new("role", role)
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}