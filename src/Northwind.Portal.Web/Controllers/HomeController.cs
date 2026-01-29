using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Northwind.Portal.Web.Models;

namespace Northwind.Portal.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // If user is authenticated, redirect based on role
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("SuperAdmin") || User.IsInRole("AdminOps") || User.IsInRole("AdminCatalog") || User.IsInRole("AdminFulfillment"))
            {
                return RedirectToAction("Index", "AdminOrders", new { area = "Admin" });
            }
            else if (User.IsInRole("CustomerUser") || User.IsInRole("CustomerApprover"))
            {
                return RedirectToAction("Index", "Catalog", new { area = "Customer" });
            }
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
