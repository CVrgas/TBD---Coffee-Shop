using Application.Auth.Dtos;
using Application.Auth.Services;
using Application.Common.Abstractions.Envelope;

namespace Application.Auth.Interfaces;

public interface IAuthQueryService
{
    Task<Envelope<UserMeDto>> GetMe();
}