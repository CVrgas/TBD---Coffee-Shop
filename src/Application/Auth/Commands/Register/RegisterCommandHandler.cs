using Application.Auth.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Security;
using Domain.Users;
using Domain.Users.Entities;
using Domain.Users.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Auth.Commands.Register;

public class RegisterCommandHandler(IPasswordManager hasher, IAppDbContext context) : IRequestHandler<RegisterCommand, Envelope<RegisterResult>>
{
    public async Task<Envelope<RegisterResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = new EmailAddress(request.Email);
        var emailExist = await context.Users.AnyAsync( u => u.Email == email, cancellationToken);
        if (emailExist) return Envelope<RegisterResult>.BadRequest("Email already exist");

        var user = User.CreateCustomer(request.FirstName, request.LastName, request.Email, hasher.HashPassword(request.Password));

        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return Envelope<RegisterResult>.Ok(new RegisterResult(user.Id));
    }
}