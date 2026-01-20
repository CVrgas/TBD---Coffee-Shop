using Application.Auth.Dtos;
using FluentValidation;

namespace Application.Auth.Validators;

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty()
            .EmailAddress();
        
        RuleFor(r => r.Password)
            .MinimumLength(6)
            .NotEmpty();

        RuleFor(r => r.FirstName)
            .MaximumLength(150)
            .NotEmpty();
        
        RuleFor(r => r.LastName)
            .MaximumLength(150)
            .NotEmpty();
    }
}

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty()
            .NotNull()
            .EmailAddress();

        RuleFor(r => r.Password)
            .NotEmpty()
            .NotNull();
    }
}