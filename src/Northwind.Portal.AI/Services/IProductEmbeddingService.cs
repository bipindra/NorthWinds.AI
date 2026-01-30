using Northwind.Portal.Domain.DTOs;

namespace Northwind.Portal.AI.Services;

public interface IProductEmbeddingService
{
    Task IndexProductAsync(ProductDto product);
    Task IndexProductsAsync(IEnumerable<ProductDto> products);
    Task<ReindexResult> ReindexAllProductsAsync(IEnumerable<ProductDto> products, IProgress<ReindexProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<List<ProductDto>> SearchProductsAsync(string query, int topK = 5);
}
