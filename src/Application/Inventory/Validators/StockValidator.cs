using Application.Inventory.Dtos;
using FluentValidation;

namespace Application.Inventory.Validators;

public class StockValidator : AbstractValidator<AdjustStockDto>
{
    public StockValidator()
    {
        RuleFor(s => s.ProductId)
            .NotEmpty().GreaterThan(0);
        RuleFor(s => s.Delta)
            .NotEmpty();
        RuleFor(s => s.Reason)
            .IsInEnum()
            .NotEmpty();
        RuleFor(s => s.RowVersion)
            .NotEmpty();
    }
}