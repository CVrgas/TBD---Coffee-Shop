using System.Linq.Expressions;
using Application.Catalog.Dtos;
using Application.Catalog.Mapping;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces;
using Application.Inventory.Dtos;
using Domain.Base;
using Domain.Catalog;
using Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Registry;

namespace Application.Catalog.Services;

public sealed class ProductService(
    IRepository<Product, int> repository, 
    IProductCategoryService categorySer, 
    IRepository<ProductCategory, int> categoryRepository, 
    IUnitOfWork uOw, 
    ResiliencePipelineProvider<string> pipeline) : IProductService
{
    private readonly ResiliencePipeline _pipeline = pipeline.GetPipeline("default-retry-pipeline");
    
    #region Get Products
    public async Task<Envelope<ProductDto>> GetProductByIdAsync(int id, CancellationToken ct = default)
    {
        
        var product = await repository.GetByIdAsync(id, selector: _getProductSelector, ct: ct);
        return product == null ? Envelope<ProductDto>.NotFound("Product not found") : Envelope<ProductDto>.Ok(product);
    }
    
    public async Task<Envelope<List<Product>>> GetProductByIdAsync(List<int> ids, CancellationToken ct = default)
    {
        var products = await repository.ListAsync(p => ids.Contains(p.Id), ct: ct);
        return Envelope<List<Product>>.Ok(products.ToList());
    }
    
    public async Task<Envelope<ProductDto>> GetProductBySkuAsync(string sku, CancellationToken ct = default)
    {
        var product = await repository.GetAsync(p => EF.Functions.Like(sku, p.Sku, "\\"), selector:_getProductSelector, ct: ct);
        return product == null
            ? Envelope<ProductDto>.NotFound("Product not found")
            : Envelope<ProductDto>.Ok(product);
    }
    public async Task<Envelope<IEnumerable<ProductDto>>> GetAllAsync(
        string? query = null,
        SortOption? sort = null,
        int? take = null,
        CancellationToken ct = default)
    {
        query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        var pattern = query is null ? null : $"%{Utilities.EscapeLike(query)}%";
        
        Expression<Func<Product, bool>>? predicate = query is null ? null 
            : p => EF.Functions.Like(p.Name ?? "", pattern, "\\") 
                   || EF.Functions.Like(p.Description ?? "", pattern, "\\");

        var products = await repository.ListAsync(selector: _getProductSelector, predicate, sort, take, ct: ct);
        return Envelope<IEnumerable<ProductDto>>.Ok(products);
    }

    public async Task<Envelope<Paginated<ProductDto>>> GetPaginatedAsync(ProductPaginatedQuery request, CancellationToken ct = default)
    {
        Expression<Func<Product, bool>>? predicate = null;

        if (request.OnlyActive) predicate = p => p.IsActive;

        if (!string.IsNullOrWhiteSpace(request.QueryPattern))
        {
            Expression<Func<Product, bool>> search =
                p => EF.Functions.Like(p.Name ?? "", request.QueryPattern, "\\")
                    || EF.Functions.Like(p.Description ?? "", request.QueryPattern, "\\");
            predicate = predicate is null ? search : predicate.And(search);
        }
        
        var paged  = await repository.GetPaginatedAsync(request.ClampIndex, request.ClampSize, selector: _getProductSelector, predicate, request.SortOption, ct: ct);
        
        var mappedPaginatedProducts = new Paginated<ProductDto>(
            paged .Entities,
            paged .TotalCount,
            paged .PageNumber,
            paged.PageSize);
        
        return Envelope<Paginated<ProductDto>>.Ok(mappedPaginatedProducts);
    }
    public async Task<Envelope<Paginated<ProductDto>>> GetPaginatedByCategoryAsync(string categorySlug, PaginatedRequest request, CancellationToken ct = default)
    {
        
        var categoryResp = await categorySer.GetBySlugAsync(categorySlug, ct);
        if(categoryResp.Data is null) return Envelope<Paginated<ProductDto>>.NotFound(categoryResp.Detail);

        Expression<Func<Product, bool>>? predicate = p => p.CategoryId == categoryResp.Data.Id;
        
        if(request.OnlyActive)
            predicate = predicate.And(p => p.IsActive);

        if (request.QueryPattern is { } pattern)
        {
            Expression<Func<Product, bool>> search = 
                p => EF.Functions.Like(p.Name ?? "", request.QueryPattern, "\\") 
                    ||  EF.Functions.Like(p.Description ?? "", request.QueryPattern, "\\");
            
            predicate = predicate.And(search);
        }
        
        var paginated = await repository.GetPaginatedAsync(request.ClampIndex, request.ClampSize, selector: _getProductSelector, predicate, request.SortOption, ct: ct);
        
        var paginatedDto = new Paginated<ProductDto>(
            paginated.Entities,
            paginated.TotalCount,
            paginated.PageNumber,
            paginated.PageSize);
        
        return Envelope<Paginated<ProductDto>>.Ok(paginatedDto);
    }
    public async Task<Envelope<string>> GetFilters()
    {
        await Task.CompletedTask;
        
        // TODO: validate if needed & Return available filters.
        return Envelope<string>.Ok("");
    }
    #endregion

    #region Add Product
    
    public async Task<Envelope<ProductDto>> AddAsync(ProductCreateDto product, CancellationToken ct = default)
    {
        var exist = await repository.ExistsAsync(p => EF.Functions.Like(p.Name, product.Name), ct: ct);
        if(exist) return Envelope<ProductDto>.BadRequest()
            .WithError(nameof(product.Name), "Product name already exists");
            
        var category = await categorySer.GetByIdAsync(product.CategoryId, ct: ct);
        if(category.Data is null) return Envelope<ProductDto>.BadRequest()
            .WithError(nameof(product.CategoryId), "Category does not exist");
        
        var entity = new Product {
            Name = product.Name!,
            Sku = $"{Utilities.GenerateSku(category.Data.Code)}",
            Price = product.Price,
            Currency = new CurrencyCode(product.Currency!),
            Description = product.Description,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            StockItems = new List<StockItem> { new() { IsActive = true } }
        };
            
        await repository.Create(entity);
        await uOw.SaveChangesAsync(ct);
        return Envelope<ProductDto>.Ok(entity.ToDto() with { CategoryName = category.Data.Name });
    }
    
    #endregion

    #region Update Product

    public async Task<Envelope<ProductDto>> UpdateAsync(ProductUpdateDto dto, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(dto.ProductId, asNoTracking: false, ct: ct);
        if (product is null) return Envelope<ProductDto>.NotFound("Product not found.");
            
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            var exist = await repository.ExistsAsync(p => 
                p.Id != dto.ProductId && dto.Name != null && p.Name == dto.Name, ct: ct);
            if(exist) return Envelope<ProductDto>.BadRequest().WithError(nameof(product.Name),"Name already exists.");
        }
            
        if (dto.CategoryId.HasValue)
        {
            var categoryExist = await categorySer.ExistAsync(c => c.Id == dto.CategoryId, ct: ct);
            if(!categoryExist) return Envelope<ProductDto>.NotFound("Category does not exist.");
        }
            
        if(dto.Name is not null) product.Name = dto.Name;
        if(dto.Description is not null) product.Description = dto.Description;
        if(dto.CategoryId is not null) product.CategoryId = (int)dto.CategoryId;
        if(dto.Name is not null || dto.Description is not null || dto.CategoryId is not null) product.UpdatedAt = DateTimeOffset.UtcNow;

        await uOw.SaveChangesAsync(ct);
        return Envelope<ProductDto>.Ok(product.ToDto());
    }

    public async Task<Envelope> RateProductAsync(int productId, int rate, CancellationToken ct = default)
    {
        return await _pipeline.ExecuteAsync(async _ =>
        {
            uOw.ClearChangeTracker();
            
            var product = await repository.GetByIdAsync(productId, asNoTracking: false, ct: ct);
            if (product is null) return Envelope.NotFound("Product not found.");

            product.RatingSum += rate;
            product.RatingCount++;

            await uOw.SaveChangesAsync(ct);
            return Envelope.Ok();
        }, ct);
    }

    public async Task<Envelope> ActiveProduct(int productId, CancellationToken ct = default) => await ToggleStatus(productId, true, ct: ct);
    public async Task<Envelope> DeactiveProduct(int productId, CancellationToken ct = default) => await ToggleStatus(productId, false, ct: ct);
    public async Task<Envelope> ToggleStatus(int productId, bool? state, CancellationToken ct = default)
    {
        if(productId < 0) return Envelope.BadRequest().WithError("product id", "Invalid product id, need to be positive.");
                
        var product = await repository.GetByIdAsync(productId, asNoTracking: false, ct: ct);
        if(product is null) return Envelope.NotFound("Product not found.");

        product.IsActive = state ?? !product.IsActive;
        await uOw.SaveChangesAsync(ct);
        return Envelope.Ok();
    }

    public async Task<Envelope> UpdatePrice(ProductUpdatePrice updatePrice, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(updatePrice.Id, ct: ct);
        if (product is null) 
            return Envelope.NotFound("Product not found.");
            
        if(updatePrice.Price <= 0)
            return Envelope.BadRequest().WithError(nameof(updatePrice.Price), "Price must be positive.");
            
        if(string.IsNullOrWhiteSpace(updatePrice.Currency) || updatePrice.Currency.Length != 3)
            return Envelope.BadRequest().WithError(nameof(updatePrice.Currency), "Invalid code.");
            
        if (updatePrice.RowVersion is not null) repository.AttachWithRowVersion(product, updatePrice.RowVersion);
        
        product.Price = updatePrice.Price;
        product.Currency = new CurrencyCode(updatePrice.Currency);
        await uOw.SaveChangesAsync(ct);
        return Envelope.Ok();
    }

    public async Task<Envelope> UpdateImageAsync(int id, string imageUrl, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(id, asNoTracking: false, ct: ct);
        if (product is null) return Envelope.NotFound("Product not found.");
            
        if(string.IsNullOrWhiteSpace(imageUrl))
            return Envelope.BadRequest().WithError(nameof(imageUrl), "Invalid image url.");
            
        product.ImageUrl = imageUrl;
        await uOw.SaveChangesAsync(ct);
        return Envelope.Ok();
    }

    public async Task<Envelope> BulkCreateAsync(List<ProductCreateDto> dtos, CancellationToken ct = default)
    {
        var incomingIds = dtos.Select(p => p.CategoryId).ToHashSet();
        var existingCategories = 
            (await categoryRepository.ListAsync(new CategoryListSpec(incomingIds), ct: ct)).ToList();
        
        var existingIds = existingCategories.Select(p => p.Id).ToHashSet();
        var missingIds = incomingIds.Where(id => !existingIds.Contains(id)).ToList();

        if (missingIds.Count != 0) 
            return Envelope.BadRequest()
                .WithError("Categories", $"Missing categories: {string.Join(", ", missingIds)}");
        
        var categoriesMap = existingCategories.ToDictionary(c => c.Id, c => c.Code);
        var products = new List<Product>();

        foreach (var dto in dtos)
        {
            var sku = Utilities.GenerateSku(categoriesMap[dto.CategoryId]);
            products.Add(dto.ToEntity(sku));
        }
        
        await repository.CreateRange(products);
        await uOw.SaveChangesAsync(ct);
        
        return Envelope.Ok();
    }

    #endregion

    #region Helpers

    private Expression<Func<Product, ProductDto>> _getProductSelector = p => new ProductDto
    {
        Id = p.Id,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Sku = p.Sku,
        Name = p.Name,
        Price = p.Price,
        Currency = p.Currency.Code,
        IsOnSale = p.IsOnSale,
        SalePrice = p.SalePrice,
        Description = p.Description ?? "",
        ImageUrl = p.ImageUrl,
        IsActive = p.IsActive,
        RatingCount = p.RatingCount,
        RatingSum = p.RatingSum,
        CategoryId = p.CategoryId,
        CategoryName = p.Category!.Name,
        RowVersion = p.RowVersion
    };

    #endregion
}

public class CategoryListSpec(HashSet<int> ids) : Specification<ProductCategory>(pc => ids.Contains(pc.Id));