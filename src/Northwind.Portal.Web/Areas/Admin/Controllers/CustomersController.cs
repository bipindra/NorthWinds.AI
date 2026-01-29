using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Northwind.Portal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class CustomersController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(string id)
    {
        return View();
    }
}
