using Application.Auth.Commands.Login;
using Application.Auth.Commands.Register;
using FluentValidation;

namespace Application.Auth.Validators;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
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

public sealed class LoginValidator : AbstractValidator<LoginCommand>
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