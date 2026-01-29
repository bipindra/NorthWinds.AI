using Northwind.Portal.Domain.Entities;

namespace Northwind.Portal.Data.Repositories;

public interface IProductRepository
{
    Task<IQueryable<Product>> GetProductsQueryableAsync(int? categoryId = null, int? supplierId = null, bool? discontinued = null, bool? inStockOnly = null, string? searchTerm = null);
    Task<Product?> GetProductByIdAsync(int productId);
    Task<IEnumerable<Category>> GetCategoriesAsync();
    Task<IEnumerable<Supplier>> GetSuppliersAsync();
}
