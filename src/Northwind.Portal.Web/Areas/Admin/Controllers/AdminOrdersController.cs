using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northwind.Portal.Domain.Enums;
using Northwind.Portal.Domain.Services;
using System.Security.Claims;

namespace Northwind.Portal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class AdminOrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ILogger<AdminOrdersController> _logger;

    public AdminOrdersController(IOrderService orderService, ILogger<AdminOrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? status = null)
    {
        OrderPortalStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderPortalStatus>(status, out var parsedStatus))
        {
            statusEnum = parsedStatus;
        }

        var orders = await _orderService.GetOrdersByStatusAsync(statusEnum);
        ViewBag.Status = status;
        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            return NotFound();

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? comment = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _orderService.TransitionStatusAsync(id, OrderPortalStatus.Approved, userId, comment);
        if (result)
        {
            _logger.LogInformation("Order {OrderId} approved by user {UserId}", id, userId);
            TempData["Success"] = "Order approved";
        }
        else
        {
            _logger.LogWarning("Failed to approve order {OrderId} by user {UserId}", id, userId);
            TempData["Error"] = "Cannot approve this order";
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveToPicking(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _orderService.TransitionStatusAsync(id, OrderPortalStatus.Picking, userId);
        if (result)
        {
            _logger.LogInformation("Order {OrderId} moved to picking by user {UserId}", id, userId);
            TempData["Success"] = "Order moved to picking";
        }
        else
        {
            _logger.LogWarning("Failed to move order {OrderId} to picking by user {UserId}", id, userId);
            TempData["Error"] = "Cannot move order to picking";
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkShipped(int id, decimal? freight, int? shipVia, string? trackingNumber)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _orderService.TransitionStatusAsync(id, OrderPortalStatus.Shipped, userId, trackingNumber);
        if (result)
        {
            _logger.LogInformation("Order {OrderId} marked as shipped by user {UserId}, tracking: {Tracking}", id, userId, trackingNumber);
            TempData["Success"] = "Order marked as shipped";
        }
        else
        {
            _logger.LogWarning("Failed to ship order {OrderId} by user {UserId}", id, userId);
            TempData["Error"] = "Cannot ship this order";
        }

        return RedirectToAction("Details", new { id });
    }
}
