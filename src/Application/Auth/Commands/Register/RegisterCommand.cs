using Application.Auth.Dtos;
using Application.Common.Abstractions.Envelope;
using MediatR;

namespace Application.Auth.Commands.Register;

public sealed record RegisterCommand(string Email, string FirstName, string LastName, string Password) : IRequest<Envelope<RegisterResult>>;