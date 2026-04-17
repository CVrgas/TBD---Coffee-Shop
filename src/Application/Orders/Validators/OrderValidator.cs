using Application.Orders.Commands.CreateOrder;
using Application.Orders.Dtos;
using FluentValidation;

namespace Application.Orders.Validators;

public class OrderCreationValidator :  AbstractValidator<CreateOrderCommand>
{
    public OrderCreationValidator()
    {
        RuleFor(o => o.Currency)
            .NotEmpty().WithMessage("Currency code must be exactly 3 characters (e.g., USD).");

        RuleFor(o => o.Items)
            .NotEmpty().WithMessage("Order must contain at least one item.");
        
        RuleForEach(o => o.Items).SetValidator(new OrderItemValidator());
    }
}

public class OrderItemValidator : AbstractValidator<OrderItemDto>
{
    public OrderItemValidator()
    {
        RuleFor(i => i.ProductId)
            .NotEmpty().WithMessage("ProductId should not be empty")
            .GreaterThan(0).WithMessage("ProductId should be greater than 0");
        
        RuleFor(i => i.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1.");
    }
}