namespace Application.Auth.Dtos;

public sealed record AuthResult(string Token, int ExpiresIn);