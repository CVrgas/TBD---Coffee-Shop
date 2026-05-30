using Application.Auth.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Application.Common.Interfaces.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Auth.Queries.GetMe;

public class GetMeQueryHandler(IAppDbContext context, ICurrentUserService userService) : IRequestHandler<GetMeQuery, Envelope<UserMeDto>>
{
    public async Task<Envelope<UserMeDto>> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.RequiredUserId;
        
        var user = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserMeDto
            {
                IsActive = u.IsActive,
                Email = u.Email.Value,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role.ToString(),
            })
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        return user is not null ? Envelope<UserMeDto>.Ok(user) : Envelope<UserMeDto>.Unauthorized();
    }
}