using System.Security.Claims;
using Application.Common.Interfaces.User;
using Domain.User;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly int? _userId;
    private readonly UserRole _userRole;
    private readonly bool _isAuthenticated;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        _isAuthenticated = user?.Identity?.IsAuthenticated ?? false;

        if (_isAuthenticated && user is not null)
        {
            var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) 
                          ?? user.FindFirstValue("sub");

            if (int.TryParse(idClaim, out var id)) _userId = id;

            var roleClaim = user.FindFirstValue(ClaimTypes.Role)
                            ?? user.FindFirstValue("role");

            if (!string.IsNullOrEmpty(roleClaim) && Enum.TryParse(roleClaim, true, out UserRole role))
                _userRole = role;
            else
                _userRole = UserRole.Anonymous;

        }
        else
        {
            _userRole = UserRole.Anonymous;
        }
    }
    
    public int? UserId => _userId;
    public int RequiredUserId => _userId ?? throw new UnauthorizedAccessException("Operación requiere usuario autenticado.");
    public bool IsAuthenticated => _isAuthenticated;
    public UserRole UserRole => _userRole;
}