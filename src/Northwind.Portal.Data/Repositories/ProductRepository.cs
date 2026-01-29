using Microsoft.EntityFrameworkCore;
using Northwind.Portal.Data.Contexts;
using Northwind.Portal.Domain.Entities;

namespace Northwind.Portal.Data.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly NorthwindDbContext _context;

    public ProductRepository(NorthwindDbContext context)
    {
        _context = context;
    }

    public async Task<IQueryable<Product>> GetProductsQueryableAsync(int? categoryId = null, int? supplierId = null, bool? discontinued = null, bool? inStockOnly = null, string? searchTerm = null)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .AsNoTracking()
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (supplierId.HasValue)
            query = query.Where(p => p.SupplierId == supplierId.Value);

        if (discontinued.HasValue)
            query = query.Where(p => p.Discontinued == discontinued.Value);

        if (inStockOnly == true)
            query = query.Where(p => p.UnitsInStock > 0);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(p => p.ProductName.Contains(searchTerm));

        return await Task.FromResult(query);
    }

    public async Task<Product?> GetProductByIdAsync(int productId)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == productId);
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.CategoryName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Supplier>> GetSuppliersAsync()
    {
        return await _context.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.CompanyName)
            .ToListAsync();
    }
}
