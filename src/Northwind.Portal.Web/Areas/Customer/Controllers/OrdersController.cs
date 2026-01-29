using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northwind.Portal.Domain.Enums;
using Northwind.Portal.Domain.Services;
using Northwind.Portal.Data.Services;
using System.Security.Claims;

namespace Northwind.Portal.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerArea")]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ITenantContext tenantContext, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var customerId = await _tenantContext.GetCurrentCustomerIdAsync();
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();

        var orders = await _orderService.GetOrdersByCustomerIdAsync(customerId);
        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var customerId = await _tenantContext.GetCurrentCustomerIdAsync();
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();

        var order = await _orderService.GetOrderByIdAsync(id, customerId);
        if (order == null)
            return NotFound();

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _orderService.CancelOrderAsync(id, userId, reason);
        if (result)
        {
            _logger.LogInformation("Order {OrderId} cancelled by user {UserId}", id, userId);
            TempData["Success"] = "Order cancelled";
        }
        else
        {
            _logger.LogWarning("Failed to cancel order {OrderId} by user {UserId}", id, userId);
            TempData["Error"] = "Cannot cancel this order";
        }

        return RedirectToAction("Details", new { id });
    }
}
