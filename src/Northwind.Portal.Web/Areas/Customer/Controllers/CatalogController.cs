using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northwind.Portal.Domain.Services;

namespace Northwind.Portal.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerArea")]
public class CatalogController : Controller
{
    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
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

    public async Task<IActionResult> Details(int id)
    {
        var product = await _catalogService.GetProductByIdAsync(id);
        if (product == null)
            return NotFound();

        return View(product);
    }
}
