using Application.Orders.Dtos;
using FluentValidation;

namespace Application.Orders.Validators;

public class OrderCreationValidator :  AbstractValidator<OrderCreationDto>
{
    public OrderCreationValidator()
    {
        RuleFor(o => o.Currency)
            .NotEmpty().WithMessage("Currency can not be empty");

        RuleFor(o => o.Items)
            .NotEmpty().WithMessage("Items can not be empty");
        
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
            .GreaterThan(0).WithMessage("Quantity should be greater than 0");
    }
}