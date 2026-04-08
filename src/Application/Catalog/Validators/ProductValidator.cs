using Application.Catalog.Commands.Create;
using Application.Catalog.Commands.Update;
using FluentValidation;

namespace Application.Catalog.Validators;

public sealed class ProductValidator  : AbstractValidator<CreateProductCommand>
{
    public ProductValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(2, 80).WithMessage("Name must have between 2 and 80 characters");
        
        RuleFor(p => p.Description)
            .MaximumLength(255).WithMessage("Description must not exceed 255 characters");
        
        //RuleFor(p => p.ImageUrl)
        RuleFor(p => p.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required")
            .GreaterThan(0).WithMessage("CategoryId must be greater than 0");
        
        RuleFor(p => p.Price)
            .NotEmpty().WithMessage("Price is required")
            .GreaterThan(0).WithMessage("Price must be greater than 0");
        
        RuleFor(p => p.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must not exceed 3 characters");
    }
    
}

public class BulkProductValidator : AbstractValidator<List<CreateProductCommand>>
{
    public BulkProductValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("List cannot be empty")
            .Must(x => x.Count <= 100).WithMessage("Max batch size is 100 items."); // PROTECT MEMORY

        RuleForEach(x => x).SetValidator(new ProductValidator()); // VALIDATE ITEMS
    }
}

public sealed class ProductUpdateValidator  : AbstractValidator<UpdateProductCommand>
{
    public ProductUpdateValidator()
    {
        RuleFor(p => p.ProductId)
            .NotEmpty().WithMessage("Product Id is required")
            .GreaterThan(0).WithMessage("Product Id must be greater than 0");
        
        RuleFor(p => p.Name)
            .Length(2, 80).WithMessage("Name must have between 2 and 80 characters");
        
        RuleFor(p => p.Description)
            .MaximumLength(255).WithMessage("Description must not exceed 255 characters");
        
        RuleFor(p => p.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required")
            .GreaterThan(0).WithMessage("CategoryId must be greater than 0");
    }
    
}