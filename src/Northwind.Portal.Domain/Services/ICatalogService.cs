using Northwind.Portal.Domain.DTOs;

namespace Northwind.Portal.Domain.Services;

public interface ICatalogService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(int page, int pageSize, int? categoryId = null, int? supplierId = null, bool? discontinued = null, bool? inStockOnly = null, string? searchTerm = null);
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    Task<ProductDto?> GetProductByIdAsync(int productId);
    Task<ProductDto> CreateProductAsync(ProductDto product);
    Task<bool> UpdateProductAsync(ProductDto product);
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
    Task<IEnumerable<SupplierDto>> GetSuppliersAsync();
}
