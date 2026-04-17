using Application.Auth.Dtos;
using Application.Auth.Specifications;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces.Security;
using Domain.User;
using MediatR;

namespace Application.Auth.Commands.Register;

public class RegisterCommandHandler(
    IPasswordManager hasher, 
    IRepository<User, int> repository,
    IUnitOfWork uOw) : IRequestHandler<RegisterCommand, Envelope<RegisterResult>>
{
    public async Task<Envelope<RegisterResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var emailExist = await EmailExistAsync(request.Email, cancellationToken);
        if (emailExist) return Envelope<RegisterResult>.BadRequest("Email already exist");

        var user = User.CreateCustomer(request.FirstName, request.LastName, request.Email, hasher.HashPassword(request.Password));

        await repository.Create(user);
        await uOw.SaveChangesAsync(cancellationToken);
        return Envelope<RegisterResult>.Ok(new RegisterResult(user.Id));
    }
    
    private async Task<bool> EmailExistAsync(string email, CancellationToken ct = default) =>
        await repository.ExistsAsync(new ExistEmailSpec(new EmailAddress(email)), ct: ct);
}