using Application.Auth.Dtos;
using Application.Common.Abstractions.Envelope;
using MediatR;

namespace Application.Auth.Queries.GetMe;

public record GetMeQuery : IRequest<Envelope<UserMeDto>>;