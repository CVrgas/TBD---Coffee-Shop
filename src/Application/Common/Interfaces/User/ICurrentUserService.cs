using Domain.Base;
using Domain.User;

namespace Application.Common.Interfaces.User;

public interface ICurrentUserService
{
    public int? UserId { get;}
    public int RequiredUserId { get; }
    public bool IsAuthenticated { get; }
    public UserRole UserRole { get; }
    
}