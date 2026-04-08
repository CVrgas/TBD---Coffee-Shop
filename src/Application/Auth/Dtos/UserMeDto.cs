using System.ComponentModel.DataAnnotations.Schema;

namespace Application.Auth.Dtos;

public sealed class UserMeDto
{
    [NotMapped]
    public bool IsActive { get; init; }
    public string Email { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string Role { get; init; } = null!;
};