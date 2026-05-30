using Application.Auth.Dtos;
using Application.Common.Abstractions.Envelope;
using MediatR;

namespace Application.Auth.Commands.Refresh;

public sealed record RefreshCommand(string RefreshToken) : IRequest<Envelope<AuthResult>>;