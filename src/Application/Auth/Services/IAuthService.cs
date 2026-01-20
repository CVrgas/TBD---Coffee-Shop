using Application.Auth.Dtos;
using Application.Common.Abstractions.Envelope;

namespace Application.Auth.Services;

public interface IAuthService
{
    Task<Envelope<RegisterResult>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Envelope<AuthResult>> LoginAsync(LoginRequest request,  CancellationToken ct = default);
    Task<Envelope<UserMeDto>> GetMe();
}