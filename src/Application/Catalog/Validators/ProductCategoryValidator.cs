using Application.Catalog.Dtos;
using FluentValidation;

namespace Application.Catalog.Validators;

public class ProductCategoryValidator : AbstractValidator<ProductCategoryCreateDto>
{
    public ProductCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().MinimumLength(2).MaximumLength(80);
        
        RuleFor(x => x.Code)
            .NotEmpty().MinimumLength(3).MaximumLength(10);
        
        RuleFor(x => x.Description)
            .MaximumLength(255);
        
        RuleFor(x => x.ParentId)
            .GreaterThan(0);
    }
}