using Microsoft.EntityFrameworkCore;
using Northwind.Portal.Domain.DTOs;
using Northwind.Portal.Domain.Services;
using Northwind.Portal.Data.Repositories;

namespace Northwind.Portal.Data.Services;

public class CatalogService : ICatalogService
{
    private readonly IProductRepository _productRepository;

    public CatalogService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(int page, int pageSize, int? categoryId = null, int? supplierId = null, bool? discontinued = null, bool? inStockOnly = null, string? searchTerm = null)
    {
        var query = await _productRepository.GetProductsQueryableAsync(categoryId, supplierId, discontinued, inStockOnly, searchTerm);
        
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.ProductName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier != null ? p.Supplier.CompanyName : null,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.CategoryName : null,
                QuantityPerUnit = p.QuantityPerUnit,
                UnitPrice = p.UnitPrice,
                UnitsInStock = p.UnitsInStock,
                UnitsOnOrder = p.UnitsOnOrder,
                ReorderLevel = p.ReorderLevel,
                Discontinued = p.Discontinued
            })
            .ToListAsync();

        return new PagedResult<ProductDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductDto?> GetProductByIdAsync(int productId)
    {
        var product = await _productRepository.GetProductByIdAsync(productId);
        if (product == null)
            return null;

        return new ProductDto
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            SupplierId = product.SupplierId,
            SupplierName = product.Supplier?.CompanyName,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.CategoryName,
            QuantityPerUnit = product.QuantityPerUnit,
            UnitPrice = product.UnitPrice,
            UnitsInStock = product.UnitsInStock,
            UnitsOnOrder = product.UnitsOnOrder,
            ReorderLevel = product.ReorderLevel,
            Discontinued = product.Discontinued
        };
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
    {
        var categories = await _productRepository.GetCategoriesAsync();
        return categories.Select(c => new CategoryDto
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            Description = c.Description
        });
    }

    public async Task<IEnumerable<SupplierDto>> GetSuppliersAsync()
    {
        var suppliers = await _productRepository.GetSuppliersAsync();
        return suppliers.Select(s => new SupplierDto
        {
            SupplierId = s.SupplierId,
            CompanyName = s.CompanyName,
            ContactName = s.ContactName
        });
    }
}
