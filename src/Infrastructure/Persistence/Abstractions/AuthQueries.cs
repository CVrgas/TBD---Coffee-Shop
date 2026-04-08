using Application.Auth.Dtos;
using Application.Auth.Interfaces;
using Application.Common.Interfaces.User;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Abstractions;

public class AuthQueries(ApplicationDbContext context, ICurrentUserService userContext) : IAuthQueries
{
    public async Task<UserMeDto?> GetMe()
    {
        var userId = userContext.RequiredUserId;
        
        var user = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserMeDto
            {
                IsActive = u.IsActive,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role.ToString(),
            })
            .FirstOrDefaultAsync();

        return user;
    }
}