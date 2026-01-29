using Microsoft.AspNetCore.Mvc;

namespace Northwind.Portal.Web.Controllers;

public class ErrorController : Controller
{
    [Route("Error/{statusCode}")]
    public IActionResult HttpStatusCodeHandler(int statusCode)
    {
        ViewBag.StatusCode = statusCode;
        ViewBag.StatusMessage = statusCode switch
        {
            404 => "Page Not Found",
            500 => "Internal Server Error",
            403 => "Access Forbidden",
            _ => "An error occurred"
        };
        return View("Error");
    }

    [Route("Error")]
    public IActionResult Error()
    {
        return View();
    }
}
