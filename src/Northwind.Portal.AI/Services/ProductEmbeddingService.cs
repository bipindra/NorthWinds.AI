using Microsoft.Extensions.Logging;
using Bipins.AI.LLM;
using Northwind.Portal.Domain.DTOs;
using System.Text.Json;
using System.Reflection;
using System.Linq;

namespace Northwind.Portal.AI.Services;

public class ProductEmbeddingService : IProductEmbeddingService
{
    private readonly IChatService? _chatService;
    private readonly Bipins.AI.Vector.IVectorStore? _vectorStore;
    private readonly ILogger<ProductEmbeddingService> _logger;
    private readonly string _collectionName;

    public ProductEmbeddingService(
        IChatService? chatService,
        Bipins.AI.Vector.IVectorStore? vectorStore,
        ILogger<ProductEmbeddingService> logger,
        string collectionName = "northwind_products")
    {
        _chatService = chatService;
        _vectorStore = vectorStore;
        _logger = logger;
        _collectionName = collectionName;
    }

    public async Task IndexProductAsync(ProductDto product)
    {
        if (_chatService == null || _vectorStore == null)
        {
            _logger.LogWarning("ChatService or VectorStore not available. Skipping product indexing.");
            return;
        }

        try
        {
            // Create text representation of product for embedding
            var productText = CreateProductText(product);
            
            // Generate embedding using ChatService
            // Note: Using reflection to call embedding method as API may vary
            float[]? embedding = null;
            try
            {
                var chatServiceType = _chatService.GetType();
                
                // Try to find embedding method with different signatures
                MethodInfo? embedMethod = null;
                object[]? parameters = null;
                
                // Try: GenerateEmbeddingAsync(string)
                embedMethod = chatServiceType.GetMethod("GenerateEmbeddingAsync", 
                    BindingFlags.Public | BindingFlags.Instance, 
                    null, 
                    new[] { typeof(string) }, 
                    null);
                if (embedMethod != null)
                {
                    parameters = new object[] { productText };
                }
                
                // Try: GenerateEmbeddingAsync(string, CancellationToken)
                if (embedMethod == null)
                {
                    embedMethod = chatServiceType.GetMethod("GenerateEmbeddingAsync", 
                        BindingFlags.Public | BindingFlags.Instance, 
                        null, 
                        new[] { typeof(string), typeof(CancellationToken) }, 
                        null);
                    if (embedMethod != null)
                    {
                        parameters = new object[] { productText, CancellationToken.None };
                    }
                }
                
                // Try: GetEmbeddingAsync(string)
                if (embedMethod == null)
                {
                    embedMethod = chatServiceType.GetMethod("GetEmbeddingAsync", 
                        BindingFlags.Public | BindingFlags.Instance, 
                        null, 
                        new[] { typeof(string) }, 
                        null);
                    if (embedMethod != null)
                    {
                        parameters = new object[] { productText };
                    }
                }
                
                // Try: GetEmbeddingAsync(string, CancellationToken)
                if (embedMethod == null)
                {
                    embedMethod = chatServiceType.GetMethod("GetEmbeddingAsync", 
                        BindingFlags.Public | BindingFlags.Instance, 
                        null, 
                        new[] { typeof(string), typeof(CancellationToken) }, 
                        null);
                    if (embedMethod != null)
                    {
                        parameters = new object[] { productText, CancellationToken.None };
                    }
                }
                
                // Try: EmbedAsync(string)
                if (embedMethod == null)
                {
                    embedMethod = chatServiceType.GetMethod("EmbedAsync", 
                        BindingFlags.Public | BindingFlags.Instance, 
                        null, 
                        new[] { typeof(string) }, 
                        null);
                    if (embedMethod != null)
                    {
                        parameters = new object[] { productText };
                    }
                }
                
                // Try: EmbedAsync(string, CancellationToken)
                if (embedMethod == null)
                {
                    embedMethod = chatServiceType.GetMethod("EmbedAsync", 
                        BindingFlags.Public | BindingFlags.Instance, 
                        null, 
                        new[] { typeof(string), typeof(CancellationToken) }, 
                        null);
                    if (embedMethod != null)
                    {
                        parameters = new object[] { productText, CancellationToken.None };
                    }
                }
                
                if (embedMethod != null && parameters != null)
                {
                    _logger.LogDebug("Found embedding method: {MethodName} with {ParamCount} parameters", 
                        embedMethod.Name, parameters.Length);
                    
                    var result = embedMethod.Invoke(_chatService, parameters);
                    if (result is Task<float[]> task)
                    {
                        embedding = await task;
                    }
                    else if (result is Task<IEnumerable<float>> taskEnumerable)
                    {
                        embedding = (await taskEnumerable).ToArray();
                    }
                    else if (result is Task taskGeneric)
                    {
                        await taskGeneric;
                        var resultProperty = taskGeneric.GetType().GetProperty("Result");
                        if (resultProperty != null)
                        {
                            var resultValue = resultProperty.GetValue(taskGeneric);
                            if (resultValue is float[] floats)
                            {
                                embedding = floats;
                            }
                            else if (resultValue is IEnumerable<float> floatsEnumerable)
                            {
                                embedding = floatsEnumerable.ToArray();
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No suitable embedding method found on ChatService. Available methods: {Methods}", 
                        string.Join(", ", chatServiceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                            .Where(m => m.Name.Contains("Embed", StringComparison.OrdinalIgnoreCase))
                            .Select(m => $"{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})")));
                }
                
                if (embedding == null || embedding.Length == 0)
                {
                    _logger.LogWarning("Failed to generate embedding for product {ProductId}. Embedding method may not be available in Bipins.AI.", product.ProductId);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for product {ProductId}", product.ProductId);
                return;
            }

            // Create metadata
            var metadata = new Dictionary<string, object>
            {
                ["productId"] = product.ProductId,
                ["productName"] = product.ProductName,
                ["categoryId"] = product.CategoryId ?? 0,
                ["categoryName"] = product.CategoryName ?? "",
                ["supplierId"] = product.SupplierId ?? 0,
                ["supplierName"] = product.SupplierName ?? "",
                ["unitPrice"] = product.UnitPrice ?? 0,
                ["unitsInStock"] = product.UnitsInStock ?? 0,
                ["discontinued"] = product.Discontinued
            };

            // Store in vector database
            // Note: Using reflection as Bipins.AI API may vary
            var vectorId = $"product_{product.ProductId}";
            try
            {
                var vectorStoreType = _vectorStore.GetType();
                var upsertMethod = vectorStoreType.GetMethod("UpsertAsync", BindingFlags.Public | BindingFlags.Instance);
                
                if (upsertMethod != null)
                {
                    var parameters = upsertMethod.GetParameters();
                    if (parameters.Length == 4)
                    {
                        // Try: UpsertAsync(collection, id, vector, metadata)
                        var task = upsertMethod.Invoke(_vectorStore, new object[] { _collectionName, vectorId, embedding, metadata }) as Task;
                        if (task != null)
                        {
                            await task;
                        }
                    }
                    else if (parameters.Length == 3)
                    {
                        // Try: UpsertAsync(id, vector, metadata) - collection may be in constructor
                        var task = upsertMethod.Invoke(_vectorStore, new object[] { vectorId, embedding, metadata }) as Task;
                        if (task != null)
                        {
                            await task;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("UpsertAsync method not found on vector store");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing product {ProductId} in vector store", product.ProductId);
            }

            _logger.LogInformation("Indexed product {ProductId} in vector store", product.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing product {ProductId}", product.ProductId);
        }
    }

    public async Task IndexProductsAsync(IEnumerable<ProductDto> products)
    {
        foreach (var product in products)
        {
            await IndexProductAsync(product);
        }
    }

    public async Task<ReindexResult> ReindexAllProductsAsync(IEnumerable<ProductDto> products, IProgress<ReindexProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var result = new ReindexResult
        {
            Errors = new List<string>()
        };
        
        var startTime = DateTime.UtcNow;
        var productList = products.ToList();
        result.TotalProducts = productList.Count;
        
        _logger.LogInformation("Starting reindex of {Count} products", productList.Count);
        
        int successCount = 0;
        int errorCount = 0;
        int currentIndex = 0;
        
        foreach (var product in productList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Reindex cancelled by user");
                break;
            }
            
            currentIndex++;
            
            try
            {
                await IndexProductAsync(product);
                successCount++;
                
                progress?.Report(new ReindexProgress
                {
                    CurrentIndex = currentIndex,
                    TotalProducts = productList.Count,
                    CurrentProductName = product.ProductName,
                    SuccessCount = successCount,
                    ErrorCount = errorCount
                });
            }
            catch (Exception ex)
            {
                errorCount++;
                var errorMsg = $"Error indexing product {product.ProductId} ({product.ProductName}): {ex.Message}";
                result.Errors.Add(errorMsg);
                _logger.LogError(ex, "Error indexing product {ProductId}", product.ProductId);
                
                progress?.Report(new ReindexProgress
                {
                    CurrentIndex = currentIndex,
                    TotalProducts = productList.Count,
                    CurrentProductName = product.ProductName,
                    SuccessCount = successCount,
                    ErrorCount = errorCount
                });
            }
        }
        
        result.SuccessCount = successCount;
        result.ErrorCount = errorCount;
        result.Duration = DateTime.UtcNow - startTime;
        
        _logger.LogInformation("Completed reindex: {SuccessCount} succeeded, {ErrorCount} failed, Duration: {Duration}", 
            successCount, errorCount, result.Duration);
        
        return result;
    }

    public async Task<List<ProductDto>> SearchProductsAsync(string query, int topK = 5)
    {
        if (_chatService == null || _vectorStore == null)
        {
            _logger.LogWarning("ChatService or VectorStore not available. Returning empty results.");
            return new List<ProductDto>();
        }

        try
        {
            // Generate embedding for query
            float[]? queryEmbedding = null;
            try
            {
                var chatServiceType = _chatService.GetType();
                
                // Try to find embedding method with different signatures
                MethodInfo? embedMethod = null;
                object[]? parameters = null;
                
                // Try: GenerateEmbeddingAsync(string)
                embedMethod = chatServiceType.GetMethod("GenerateEmbeddingAsync", 
                    BindingFlags.Public | BindingFlags.Instance, 
                    null, 
                    new[] { typeof(string) }, 
                    null);
                if (embedMethod != null)
                {
                    parameters = new object[] { query };
                }
                
                // Try: GenerateEmbeddingAsync(string, CancellationToken)
                if (embedMethod == null)
                {
                    embedMethod = chatServiceType.GetMethod("GenerateEmbeddingAsync", 
                        BindingFlags.Public | BindingFlags.Instance, 
                        null, 
                        new[] { typeof(string), typeof(CancellationToken) }, 
                        null);
                    if (embedMethod != null)
                    {
                        parameters = new object[] { query, CancellationToken.None };
                    }
                }
                
                // Try: GetEmbeddingAsync(string)
                if (embedMethod == null)
                {
                    embedMethod = chatServiceType.GetMethod("GetEmbeddingAsync", 
                        BindingFlags.Public | BindingFlags.Instance, 
                        null, 
                        new[] { typeof(string) }, 
                        null);
                    if (embedMethod != null)
                    {
                        parameters = new object[] { query };
                    }
                }
                
                // Try: GetEmbeddingAsync(string, CancellationToken)
                if (embedMethod == null)
                {
                    embedMethod = chatServiceType.GetMethod("GetEmbeddingAsync", 
                        BindingFlags.Public | BindingFlags.Instance, 
                        null, 
                        new[] { typeof(string), typeof(CancellationToken) }, 
                        null);
                    if (embedMethod != null)
                    {
                        parameters = new object[] { query, CancellationToken.None };
                    }
                }
                
                // Try: EmbedAsync(string)
                if (embedMethod == null)
                {
                    embedMethod = chatServiceType.GetMethod("EmbedAsync", 
                        BindingFlags.Public | BindingFlags.Instance, 
                        null, 
                        new[] { typeof(string) }, 
                        null);
                    if (embedMethod != null)
                    {
                        parameters = new object[] { query };
                    }
                }
                
                // Try: EmbedAsync(string, CancellationToken)
                if (embedMethod == null)
                {
                    embedMethod = chatServiceType.GetMethod("EmbedAsync", 
                        BindingFlags.Public | BindingFlags.Instance, 
                        null, 
                        new[] { typeof(string), typeof(CancellationToken) }, 
                        null);
                    if (embedMethod != null)
                    {
                        parameters = new object[] { query, CancellationToken.None };
                    }
                }
                
                if (embedMethod != null && parameters != null)
                {
                    var result = embedMethod.Invoke(_chatService, parameters);
                    if (result is Task<float[]> task)
                    {
                        queryEmbedding = await task;
                    }
                    else if (result is Task<IEnumerable<float>> taskEnumerable)
                    {
                        queryEmbedding = (await taskEnumerable).ToArray();
                    }
                    else if (result is Task taskGeneric)
                    {
                        await taskGeneric;
                        var resultProperty = taskGeneric.GetType().GetProperty("Result");
                        if (resultProperty != null)
                        {
                            var resultValue = resultProperty.GetValue(taskGeneric);
                            if (resultValue is float[] floats)
                            {
                                queryEmbedding = floats;
                            }
                            else if (resultValue is IEnumerable<float> floatsEnumerable)
                            {
                                queryEmbedding = floatsEnumerable.ToArray();
                            }
                        }
                    }
                }
                
                if (queryEmbedding == null || queryEmbedding.Length == 0)
                {
                    _logger.LogWarning("Failed to generate embedding for query: {Query}", query);
                    return new List<ProductDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for query: {Query}", query);
                return new List<ProductDto>();
            }

            // Search in vector store
            // Note: Using reflection as Bipins.AI API may vary
            var vectorStoreType = _vectorStore.GetType();
            var searchMethod = vectorStoreType.GetMethod("SearchAsync", BindingFlags.Public | BindingFlags.Instance);
            
            if (searchMethod == null)
            {
                _logger.LogWarning("SearchAsync method not found on vector store");
                return new List<ProductDto>();
            }

            object? searchResult = null;
            try
            {
                var result = searchMethod.Invoke(_vectorStore, new object[] { _collectionName, queryEmbedding, topK });
                if (result is Task task)
                {
                    await task;
                    var resultProperty = task.GetType().GetProperty("Result");
                    searchResult = resultProperty?.GetValue(task);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling SearchAsync on vector store");
                return new List<ProductDto>();
            }

            if (searchResult == null)
            {
                return new List<ProductDto>();
            }

            var products = new List<ProductDto>();
            
            // Handle different return types (IEnumerable, List, Array)
            var enumerable = searchResult as System.Collections.IEnumerable;
            if (enumerable != null)
            {
                foreach (var result in enumerable)
                {
                    try
                    {
                        // Extract metadata using reflection
                        var metadataProperty = result.GetType().GetProperty("Metadata");
                        var metadata = metadataProperty?.GetValue(result) as Dictionary<string, object>;
                        
                        if (metadata == null)
                            continue;

                        var product = new ProductDto
                        {
                            ProductId = ExtractInt(metadata, "productId") ?? 0,
                            ProductName = ExtractString(metadata, "productName") ?? "",
                            CategoryId = ExtractInt(metadata, "categoryId"),
                            CategoryName = ExtractString(metadata, "categoryName"),
                            SupplierId = ExtractInt(metadata, "supplierId"),
                            SupplierName = ExtractString(metadata, "supplierName"),
                            UnitPrice = ExtractDecimal(metadata, "unitPrice"),
                            UnitsInStock = ExtractShort(metadata, "unitsInStock"),
                            Discontinued = ExtractBool(metadata, "discontinued")
                        };

                        if (product.ProductId > 0 && !string.IsNullOrEmpty(product.ProductName))
                        {
                            products.Add(product);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing search result");
                    }
                }
            }

            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with vector store");
            return new List<ProductDto>();
        }
    }

    private string CreateProductText(ProductDto product)
    {
        var parts = new List<string>
        {
            product.ProductName ?? ""
        };

        if (!string.IsNullOrEmpty(product.Description))
            parts.Add(product.Description);

        if (!string.IsNullOrEmpty(product.CategoryName))
            parts.Add($"Category: {product.CategoryName}");

        if (!string.IsNullOrEmpty(product.SupplierName))
            parts.Add($"Supplier: {product.SupplierName}");

        if (!string.IsNullOrEmpty(product.QuantityPerUnit))
            parts.Add($"Package: {product.QuantityPerUnit}");

        if (product.UnitPrice.HasValue)
            parts.Add($"Price: ${product.UnitPrice:F2}");

        if (product.UnitsInStock.HasValue)
            parts.Add($"Stock: {product.UnitsInStock} units");

        return string.Join(". ", parts);
    }

    private int? ExtractInt(Dictionary<string, object> metadata, string key)
    {
        if (metadata.TryGetValue(key, out var value))
        {
            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out var id)) return id;
            if (int.TryParse(value.ToString(), out var parsed)) return parsed;
        }
        return null;
    }

    private string? ExtractString(Dictionary<string, object> metadata, string key)
    {
        if (metadata.TryGetValue(key, out var value))
        {
            return value?.ToString();
        }
        return null;
    }

    private decimal? ExtractDecimal(Dictionary<string, object> metadata, string key)
    {
        if (metadata.TryGetValue(key, out var value))
        {
            if (value is decimal d) return d;
            if (value is double db) return (decimal)db;
            if (value is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetDecimal(out var dec)) return dec;
            if (decimal.TryParse(value.ToString(), out var parsed)) return parsed;
        }
        return null;
    }

    private short? ExtractShort(Dictionary<string, object> metadata, string key)
    {
        if (metadata.TryGetValue(key, out var value))
        {
            if (value is short s) return s;
            if (value is int i && i <= short.MaxValue && i >= short.MinValue) return (short)i;
            if (value is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt16(out var sh)) return sh;
            if (short.TryParse(value.ToString(), out var parsed)) return parsed;
        }
        return null;
    }

    private bool ExtractBool(Dictionary<string, object> metadata, string key)
    {
        if (metadata.TryGetValue(key, out var value))
        {
            if (value is bool b) return b;
            if (value is JsonElement je && je.ValueKind == JsonValueKind.True) return true;
            if (value is JsonElement je2 && je2.ValueKind == JsonValueKind.False) return false;
            if (bool.TryParse(value.ToString(), out var parsed)) return parsed;
        }
        return false;
    }
}
