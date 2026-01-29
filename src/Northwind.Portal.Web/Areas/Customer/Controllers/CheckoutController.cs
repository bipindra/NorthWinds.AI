using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northwind.Portal.Domain.DTOs;
using Northwind.Portal.Domain.Services;
using Northwind.Portal.Web.ViewModels;
using System.Security.Claims;

namespace Northwind.Portal.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerArea")]
public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(ICartService cartService, IOrderService orderService, ILogger<CheckoutController> logger)
    {
        _cartService = cartService;
        _orderService = orderService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var cart = await _cartService.GetCartAsync(userId);
        if (cart == null || !cart.Lines.Any())
        {
            TempData["Error"] = "Your cart is empty";
            return RedirectToAction("Index", "Cart");
        }

        return View(new CheckoutViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(CheckoutViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Index", model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var checkoutDto = new CheckoutDto
            {
                ShipName = model.ShipName,
                ShipAddress = model.ShipAddress,
                ShipCity = model.ShipCity,
                ShipRegion = model.ShipRegion,
                ShipPostalCode = model.ShipPostalCode,
                ShipCountry = model.ShipCountry,
                PoNumber = model.PoNumber,
                Notes = model.Notes
            };

            var order = await _orderService.PlaceOrderAsync(checkoutDto, userId);
            _logger.LogInformation("Order {OrderId} placed by user {UserId}", order.OrderId, userId);
            TempData["Success"] = $"Order #{order.OrderId} placed successfully!";
            return RedirectToAction("Details", "Orders", new { id = order.OrderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place order for user {UserId}", userId);
            TempData["Error"] = $"Failed to place order: {ex.Message}";
            return View("Index", model);
        }
    }
}
