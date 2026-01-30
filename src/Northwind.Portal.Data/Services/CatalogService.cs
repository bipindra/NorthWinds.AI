using Microsoft.EntityFrameworkCore;
using Northwind.Portal.Domain.DTOs;
using Northwind.Portal.Domain.Services;
using Northwind.Portal.Domain.Entities;
using Northwind.Portal.Data.Repositories;
using Northwind.Portal.Data.Contexts;

namespace Northwind.Portal.Data.Services;

public class CatalogService : ICatalogService
{
    private readonly IProductRepository _productRepository;
    private readonly NorthwindDbContext _context;

    public CatalogService(IProductRepository productRepository, NorthwindDbContext context)
    {
        _productRepository = productRepository;
        _context = context;
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
                Description = p.Description,
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

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        var query = await _productRepository.GetProductsQueryableAsync();
        var products = await query
            .OrderBy(p => p.ProductName)
            .Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Description = p.Description,
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

        return products;
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
            Description = product.Description,
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

    public async Task<ProductDto> CreateProductAsync(ProductDto productDto)
    {
        var product = new Product
        {
            ProductName = productDto.ProductName,
            Description = productDto.Description,
            CategoryId = productDto.CategoryId,
            SupplierId = productDto.SupplierId,
            QuantityPerUnit = productDto.QuantityPerUnit,
            UnitPrice = productDto.UnitPrice,
            UnitsInStock = productDto.UnitsInStock,
            UnitsOnOrder = productDto.UnitsOnOrder,
            ReorderLevel = productDto.ReorderLevel,
            Discontinued = productDto.Discontinued
        };

        var created = await _productRepository.CreateProductAsync(product);
        
        // Return the created product as DTO
        return new ProductDto
        {
            ProductId = created.ProductId,
            ProductName = created.ProductName,
            Description = created.Description,
            CategoryId = created.CategoryId,
            SupplierId = created.SupplierId,
            QuantityPerUnit = created.QuantityPerUnit,
            UnitPrice = created.UnitPrice,
            UnitsInStock = created.UnitsInStock,
            UnitsOnOrder = created.UnitsOnOrder,
            ReorderLevel = created.ReorderLevel,
            Discontinued = created.Discontinued
        };
    }

    public async Task<bool> UpdateProductAsync(ProductDto productDto)
    {
        var product = await _context.Products.FindAsync(productDto.ProductId);
        if (product == null)
            return false;

        product.ProductName = productDto.ProductName;
        product.Description = productDto.Description;
        product.CategoryId = productDto.CategoryId;
        product.SupplierId = productDto.SupplierId;
        product.QuantityPerUnit = productDto.QuantityPerUnit;
        product.UnitPrice = productDto.UnitPrice;
        product.UnitsInStock = productDto.UnitsInStock;
        product.UnitsOnOrder = productDto.UnitsOnOrder;
        product.ReorderLevel = productDto.ReorderLevel;
        product.Discontinued = productDto.Discontinued;

        return await _productRepository.UpdateProductAsync(product);
    }
}
