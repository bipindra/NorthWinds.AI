using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Northwind.Portal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "RequireCatalogAdmin")]
public class ProductsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Create()
    {
        return View();
    }

    public IActionResult Edit(int id)
    {
        return View();
    }

    public IActionResult Details(int id)
    {
        return View();
    }
}
