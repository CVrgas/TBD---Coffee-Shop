using FluentValidation;

namespace Application.Orders.Commands.CancelOrder;

public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("Order number must not be empty.")
            .MaximumLength(50).WithMessage("Order number must not exceed 50 characters.");
    }
}