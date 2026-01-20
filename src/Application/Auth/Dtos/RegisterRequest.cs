namespace Application.Auth.Dtos;

public sealed record RegisterRequest(string Email, string FirstName, string LastName, string Password);