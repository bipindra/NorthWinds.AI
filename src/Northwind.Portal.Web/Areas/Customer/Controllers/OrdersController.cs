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

    public async Task<IActionResult> Index(string? status = null, string? searchTerm = null, int page = 1)
    {
        var customerId = await _tenantContext.GetCurrentCustomerIdAsync();
        if (string.IsNullOrEmpty(customerId))
        {
            _logger.LogWarning("User {UserId} attempted to access orders but has no customer mapping", User.Identity?.Name);
            TempData["Error"] = "Your account is not linked to a customer. Please contact support.";
            return RedirectToAction("Index", "Catalog", new { area = "Customer" });
        }

        var allOrders = await _orderService.GetOrdersByCustomerIdAsync(customerId);
        
        // Filter by status
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderPortalStatus>(status, out var statusEnum))
        {
            allOrders = allOrders.Where(o => o.CurrentStatus == statusEnum);
        }

        // Search by order ID or product name
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchLower = searchTerm.ToLowerInvariant();
            allOrders = allOrders.Where(o => 
                o.OrderId.ToString().Contains(searchLower) ||
                o.OrderDetails.Any(od => od.ProductName.ToLowerInvariant().Contains(searchLower)));
        }

        // Pagination
        var pageSize = 10;
        var totalOrders = allOrders.Count();
        var orders = allOrders
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.Status = status;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);
        ViewBag.TotalOrders = totalOrders;

        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var customerId = await _tenantContext.GetCurrentCustomerIdAsync();
        if (string.IsNullOrEmpty(customerId))
        {
            _logger.LogWarning("User {UserId} attempted to access order {OrderId} but has no customer mapping", User.Identity?.Name, id);
            TempData["Error"] = "Your account is not linked to a customer. Please contact support.";
            return RedirectToAction("Index", "Catalog", new { area = "Customer" });
        }

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
