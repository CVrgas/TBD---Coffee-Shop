using Application.Payments.Commands.ConfirmPayment;
using Application.Payments.Commands.CreatePaymentIntent;
using FluentValidation;

namespace Application.Payments.Validators;

public class CreatePaymentIntentValidator : AbstractValidator<CreatePaymentIntentCommand>
{
    public CreatePaymentIntentValidator()
    {
        RuleFor(p => p.OrderNumber).NotEmpty();
    }   
}

public class ConfirmPaymentValidator : AbstractValidator<ConfirmPaymentCommand>
{
    public ConfirmPaymentValidator()
    {
        RuleFor(p => p.IntentId).NotEmpty();
    }   
}