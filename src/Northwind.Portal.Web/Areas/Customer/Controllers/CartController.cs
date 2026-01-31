using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northwind.Portal.Domain.Services;
using System.Security.Claims;

namespace Northwind.Portal.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerArea")]
public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var cart = await _cartService.GetCartAsync(userId);
        return View(cart);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int productId, short quantity = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _cartService.AddToCartAsync(userId, productId, quantity);
        if (result)
            TempData["Success"] = "Product added to cart";
        else
            TempData["Error"] = "Failed to add product to cart";

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Update(int cartLineId, short quantity)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _cartService.UpdateQuantityAsync(userId, cartLineId, quantity);
        if (result)
            TempData["Success"] = "Cart updated";
        else
            TempData["Error"] = "Failed to update cart";

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> GetCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { count = 0 });
        }

        var cart = await _cartService.GetCartAsync(userId);
        var count = cart?.Lines?.Sum(l => l.Quantity) ?? 0;
        return Json(new { count });
    }

    [HttpGet]
    public async Task<IActionResult> GetDetails()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { lines = new List<object>(), subTotal = 0, totalItems = 0 });
        }

        var cart = await _cartService.GetCartAsync(userId);
        if (cart == null || !cart.Lines.Any())
        {
            return Json(new { lines = new List<object>(), subTotal = 0, totalItems = 0 });
        }

        return Json(new
        {
            lines = cart.Lines.Select(l => new
            {
                id = l.Id,
                productId = l.ProductId,
                productName = l.ProductName,
                description = l.Description,
                quantity = l.Quantity,
                unitPrice = l.UnitPrice,
                lineTotal = l.LineTotal
            }),
            subTotal = cart.SubTotal,
            totalItems = cart.TotalItems
        });
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int cartLineId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _cartService.RemoveFromCartAsync(userId, cartLineId);
        if (result)
            TempData["Success"] = "Item removed from cart";
        else
            TempData["Error"] = "Failed to remove item";

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _cartService.ClearCartAsync(userId);
        TempData["Success"] = "Cart cleared";
        return RedirectToAction("Index");
    }
}
