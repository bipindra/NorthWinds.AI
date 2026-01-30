using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northwind.Portal.Data.Services;
using Northwind.Portal.AI.Services;
using Northwind.Portal.Domain.DTOs;
using Northwind.Portal.Domain.Services;
using System.Text;
using System.Linq;

namespace Northwind.Portal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "RequireCatalogAdmin")]
public class ProductsController : Controller
{
    private readonly ProductImportService _importService;
    private readonly IProductEmbeddingService? _embeddingService;
    private readonly ICatalogService _catalogService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        ProductImportService importService,
        IProductEmbeddingService? embeddingService,
        ICatalogService catalogService,
        ILogger<ProductsController> logger)
    {
        _importService = importService;
        _embeddingService = embeddingService;
        _catalogService = catalogService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int page = 1, int? categoryId = null, int? supplierId = null, bool? discontinued = null, bool? inStockOnly = null, string? searchTerm = null)
    {
        var pageSize = 20;
        var products = await _catalogService.GetProductsAsync(page, pageSize, categoryId, supplierId, discontinued, inStockOnly, searchTerm);
        var categories = await _catalogService.GetCategoriesAsync();
        var suppliers = await _catalogService.GetSuppliersAsync();

        ViewBag.Categories = categories;
        ViewBag.Suppliers = suppliers;
        ViewBag.CategoryId = categoryId;
        ViewBag.SupplierId = supplierId;
        ViewBag.Discontinued = discontinued;
        ViewBag.InStockOnly = inStockOnly;
        ViewBag.SearchTerm = searchTerm;

        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var categories = await _catalogService.GetCategoriesAsync();
        var suppliers = await _catalogService.GetSuppliersAsync();
        
        ViewBag.Categories = categories;
        ViewBag.Suppliers = suppliers;
        
        return View(new ProductDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductDto product, bool indexToVectorStore = true)
    {
        if (!ModelState.IsValid)
        {
            var categories = await _catalogService.GetCategoriesAsync();
            var suppliers = await _catalogService.GetSuppliersAsync();
            
            ViewBag.Categories = categories;
            ViewBag.Suppliers = suppliers;
            return View(product);
        }

        try
        {
            var createdProduct = await _catalogService.CreateProductAsync(product);
            
            // Index to vector store if enabled
            if (indexToVectorStore && _embeddingService != null)
            {
                try
                {
                    var productDto = await _catalogService.GetProductByIdAsync(createdProduct.ProductId);
                    if (productDto != null)
                    {
                        await _embeddingService.IndexProductAsync(productDto);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error indexing product to vector store");
                    // Don't fail the create if indexing fails
                }
            }

            TempData["Success"] = $"Product '{product.ProductName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            ModelState.AddModelError("", $"Error creating product: {ex.Message}");
            
            var categories = await _catalogService.GetCategoriesAsync();
            var suppliers = await _catalogService.GetSuppliersAsync();
            
            ViewBag.Categories = categories;
            ViewBag.Suppliers = suppliers;
            return View(product);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _catalogService.GetProductByIdAsync(id);
        if (product == null)
            return NotFound();

        var categories = await _catalogService.GetCategoriesAsync();
        var suppliers = await _catalogService.GetSuppliersAsync();
        
        ViewBag.Categories = categories;
        ViewBag.Suppliers = suppliers;
        
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductDto product, bool indexToVectorStore = true)
    {
        if (id != product.ProductId)
            return NotFound();

        if (!ModelState.IsValid)
        {
            var categories = await _catalogService.GetCategoriesAsync();
            var suppliers = await _catalogService.GetSuppliersAsync();
            
            ViewBag.Categories = categories;
            ViewBag.Suppliers = suppliers;
            return View(product);
        }

        try
        {
            var result = await _catalogService.UpdateProductAsync(product);
            
            if (!result)
            {
                ModelState.AddModelError("", "Product not found or could not be updated.");
                var categories = await _catalogService.GetCategoriesAsync();
                var suppliers = await _catalogService.GetSuppliersAsync();
                
                ViewBag.Categories = categories;
                ViewBag.Suppliers = suppliers;
                return View(product);
            }

            // Re-index to vector store if enabled
            if (indexToVectorStore && _embeddingService != null)
            {
                try
                {
                    var updatedProduct = await _catalogService.GetProductByIdAsync(product.ProductId);
                    if (updatedProduct != null)
                    {
                        await _embeddingService.IndexProductAsync(updatedProduct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error re-indexing product to vector store");
                    // Don't fail the update if indexing fails
                }
            }

            TempData["Success"] = $"Product '{product.ProductName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product");
            ModelState.AddModelError("", $"Error updating product: {ex.Message}");
            
            var categories = await _catalogService.GetCategoriesAsync();
            var suppliers = await _catalogService.GetSuppliersAsync();
            
            ViewBag.Categories = categories;
            ViewBag.Suppliers = suppliers;
            return View(product);
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _catalogService.GetProductByIdAsync(id);
        if (product == null)
            return NotFound();

        return View(product);
    }

    [HttpGet]
    public IActionResult Reindex()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reindex(CancellationToken cancellationToken)
    {
        if (_embeddingService == null)
        {
            return Json(new { success = false, message = "Vector store embedding service is not configured." });
        }

        try
        {
            // Get all products
            var allProducts = await _catalogService.GetAllProductsAsync();
            var productList = allProducts.ToList();

            if (productList.Count == 0)
            {
                return Json(new { success = false, message = "No products found to reindex." });
            }

            // Start reindexing in background
            var progress = new Progress<ReindexProgress>(p =>
            {
                // Progress updates will be handled via SignalR or polling
            });

            // Run reindexing asynchronously
            var result = await _embeddingService.ReindexAllProductsAsync(productList, progress, cancellationToken);

            return Json(new
            {
                success = true,
                message = $"Reindexing completed: {result.SuccessCount} succeeded, {result.ErrorCount} failed out of {result.TotalProducts} products.",
                totalProducts = result.TotalProducts,
                successCount = result.SuccessCount,
                errorCount = result.ErrorCount,
                errors = result.Errors,
                duration = result.Duration.TotalSeconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reindexing");
            return Json(new { success = false, message = $"Error during reindexing: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ReindexStatus()
    {
        // This endpoint can be polled for progress updates
        // For now, return a simple status
        var allProducts = await _catalogService.GetAllProductsAsync();
        return Json(new
        {
            totalProducts = allProducts.Count()
        });
    }

    [HttpGet]
    public IActionResult Import()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile csvFile, bool indexToVectorStore = true)
    {
        // Check if this is an AJAX request
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return await ImportAjaxAsync(csvFile, indexToVectorStore);
        }

        // Regular form post
        if (csvFile == null || csvFile.Length == 0)
        {
            TempData["Error"] = "Please select a CSV file to upload.";
            return View();
        }

        if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Please upload a CSV file.";
            return View();
        }

        try
        {
            using var stream = csvFile.OpenReadStream();
            var result = await _importService.ImportFromCsvAsync(stream);

            if (result.SuccessCount > 0)
            {
                // Index products to vector store for RAG
                if (indexToVectorStore && _embeddingService != null)
                {
                    try
                    {
                        // Get full product details with category and supplier names
                        var productDtos = new List<ProductDto>();
                        foreach (var p in result.Products)
                        {
                            var productDto = await _catalogService.GetProductByIdAsync(p.ProductId);
                            if (productDto != null)
                            {
                                productDtos.Add(productDto);
                            }
                            else
                            {
                                // Fallback: create DTO without category/supplier names
                                productDtos.Add(new ProductDto
                                {
                                    ProductId = p.ProductId,
                                    ProductName = p.ProductName,
                                    Description = p.Description,
                                    CategoryId = p.CategoryId,
                                    SupplierId = p.SupplierId,
                                    QuantityPerUnit = p.QuantityPerUnit,
                                    UnitPrice = p.UnitPrice,
                                    UnitsInStock = p.UnitsInStock,
                                    UnitsOnOrder = p.UnitsOnOrder,
                                    ReorderLevel = p.ReorderLevel,
                                    Discontinued = p.Discontinued
                                });
                            }
                        }

                        await _embeddingService.IndexProductsAsync(productDtos);
                        _logger.LogInformation("Indexed {Count} products to vector store", productDtos.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error indexing products to vector store");
                        // Don't fail the import if indexing fails
                    }
                }

                TempData["Success"] = $"Successfully imported {result.SuccessCount} product(s).";
                if (result.Errors.Any())
                {
                    TempData["Warning"] = $"{result.Errors.Count} error(s) occurred. Check logs for details.";
                }
            }
            else
            {
                TempData["Error"] = "No products were imported. Please check the CSV format.";
                if (result.Errors.Any())
                {
                    TempData["ErrorDetails"] = string.Join("\n", result.Errors.Take(10));
                }
            }

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing products from CSV");
            TempData["Error"] = $"Import failed: {ex.Message}";
            return View();
        }
    }

    private async Task<IActionResult> ImportAjaxAsync(IFormFile csvFile, bool indexToVectorStore)
    {
        if (csvFile == null || csvFile.Length == 0)
        {
            return Json(new { success = false, message = "Please select a CSV file to upload." });
        }

        if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return Json(new { success = false, message = "Please upload a CSV file." });
        }

        try
        {
            using var stream = csvFile.OpenReadStream();
            var result = await _importService.ImportFromCsvAsync(stream);

            if (result.SuccessCount > 0)
            {
                // Index products to vector store for RAG
                if (indexToVectorStore && _embeddingService != null)
                {
                    try
                    {
                        // Get full product details with category and supplier names
                        var productDtos = new List<ProductDto>();
                        foreach (var p in result.Products)
                        {
                            var productDto = await _catalogService.GetProductByIdAsync(p.ProductId);
                            if (productDto != null)
                            {
                                productDtos.Add(productDto);
                            }
                            else
                            {
                                // Fallback: create DTO without category/supplier names
                                productDtos.Add(new ProductDto
                                {
                                    ProductId = p.ProductId,
                                    ProductName = p.ProductName,
                                    Description = p.Description,
                                    CategoryId = p.CategoryId,
                                    SupplierId = p.SupplierId,
                                    QuantityPerUnit = p.QuantityPerUnit,
                                    UnitPrice = p.UnitPrice,
                                    UnitsInStock = p.UnitsInStock,
                                    UnitsOnOrder = p.UnitsOnOrder,
                                    ReorderLevel = p.ReorderLevel,
                                    Discontinued = p.Discontinued
                                });
                            }
                        }

                        await _embeddingService.IndexProductsAsync(productDtos);
                        _logger.LogInformation("Indexed {Count} products to vector store", productDtos.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error indexing products to vector store");
                        // Don't fail the import if indexing fails
                    }
                }

                return Json(new
                {
                    success = true,
                    message = $"Successfully imported {result.SuccessCount} product(s).",
                    successCount = result.SuccessCount,
                    totalProcessed = result.TotalProcessed,
                    errorCount = result.Errors.Count,
                    errors = result.Errors.Take(10).ToList(),
                    warning = result.Errors.Any() ? $"{result.Errors.Count} error(s) occurred." : null
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "No products were imported. Please check the CSV format.",
                    errors = result.Errors.Take(10).ToList()
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing products from CSV");
            return Json(new { success = false, message = $"Import failed: {ex.Message}" });
        }
    }

    [HttpGet]
    public IActionResult DownloadSampleCsv()
    {
        // Serve the static template file from wwwroot
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "product_import_sample.csv");
        
        if (System.IO.File.Exists(filePath))
        {
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "text/csv", "product_import_sample.csv");
        }
        
        // Fallback: Generate CSV if template file doesn't exist
        var csv = new StringBuilder();
        csv.AppendLine("ProductName,Description,CategoryId,SupplierId,QuantityPerUnit,UnitPrice,UnitsInStock,UnitsOnOrder,ReorderLevel,Discontinued");
        csv.AppendLine("Organic Green Tea,\"Premium organic green tea with antioxidant properties\",1,1,\"24 boxes x 20 bags\",15.50,100,0,10,false");
        csv.AppendLine("Premium Coffee Beans,\"High-quality arabica coffee beans, medium roast\",1,1,\"12 bags x 1 lb\",25.99,50,0,5,false");
        csv.AppendLine("Artisan Honey,\"Pure raw honey from local beekeepers\",2,2,\"500g jar\",12.75,75,0,10,false");
        csv.AppendLine("Sea Salt,\"Natural sea salt harvested from pristine waters\",2,2,\"1kg bag\",8.50,200,0,20,false");
        csv.AppendLine("Olive Oil Extra Virgin,\"Cold-pressed extra virgin olive oil, first pressing\",2,3,\"750ml bottle\",18.99,60,0,10,false");

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", "product_import_sample.csv");
    }
}
