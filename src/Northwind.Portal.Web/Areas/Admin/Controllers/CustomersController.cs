using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northwind.Portal.Domain.Services;
using Northwind.Portal.Domain.DTOs;

namespace Northwind.Portal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int page = 1, string? searchTerm = null, string? country = null, string? city = null)
    {
        var pageSize = 20;
        var customers = await _customerService.GetCustomersAsync(page, pageSize, searchTerm, country, city);
        var countries = await _customerService.GetCountriesAsync();
        var cities = await _customerService.GetCitiesAsync(country);

        ViewBag.Countries = countries;
        ViewBag.Cities = cities;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.Country = country;
        ViewBag.City = city;

        return View(customers);
    }

    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null)
            return NotFound();

        return View(customer);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null)
            return NotFound();

        var countries = await _customerService.GetCountriesAsync();
        var cities = await _customerService.GetCitiesAsync(customer.Country);

        ViewBag.Countries = countries;
        ViewBag.Cities = cities;

        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, CustomerDto customer)
    {
        if (id != customer.CustomerId)
            return NotFound();

        if (!ModelState.IsValid)
        {
            var countries = await _customerService.GetCountriesAsync();
            var cities = await _customerService.GetCitiesAsync(customer.Country);

            ViewBag.Countries = countries;
            ViewBag.Cities = cities;
            return View(customer);
        }

        try
        {
            var result = await _customerService.UpdateCustomerAsync(customer);
            if (!result)
            {
                ModelState.AddModelError("", "Customer not found or could not be updated.");
                var countries = await _customerService.GetCountriesAsync();
                var cities = await _customerService.GetCitiesAsync(customer.Country);

                ViewBag.Countries = countries;
                ViewBag.Cities = cities;
                return View(customer);
            }

            TempData["Success"] = $"Customer '{customer.CompanyName}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = customer.CustomerId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", id);
            ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
            
            var countries = await _customerService.GetCountriesAsync();
            var cities = await _customerService.GetCitiesAsync(customer.Country);

            ViewBag.Countries = countries;
            ViewBag.Cities = cities;
            return View(customer);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCities(string? country)
    {
        var cities = await _customerService.GetCitiesAsync(country);
        return Json(cities);
    }
}
