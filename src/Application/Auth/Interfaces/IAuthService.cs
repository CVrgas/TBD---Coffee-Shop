using Application.Auth.Dtos;
using Application.Auth.Services;
using Application.Common.Abstractions.Envelope;

namespace Application.Auth.Interfaces;

public interface IAuthService
{
    Task<Envelope<RegisterResult>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Envelope<AuthResult>> LoginAsync(LoginRequest request,  CancellationToken ct = default);
    Task<Envelope<UserMeDto>> GetMe();
}