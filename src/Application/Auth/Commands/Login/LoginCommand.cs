using Application.Auth.Dtos;
using Application.Common.Abstractions.Envelope;
using MediatR;

namespace Application.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Envelope<AuthResult>>;