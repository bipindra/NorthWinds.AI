using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northwind.Portal.Domain.Services;

namespace Northwind.Portal.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminArea")]
public class ReportsController : Controller
{
    private readonly IReportsService _reportsService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportsService reportsService, ILogger<ReportsController> logger)
    {
        _reportsService = reportsService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var stats = await _reportsService.GetDashboardStatsAsync();
            return View(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard stats");
            return View(new Domain.DTOs.DashboardStatsDto());
        }
    }

    public async Task<IActionResult> Sales(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var report = await _reportsService.GetSalesReportAsync(startDate, endDate);
            return View(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading sales report");
            return View(new Domain.DTOs.SalesReportDto());
        }
    }

    public async Task<IActionResult> Customers()
    {
        try
        {
            var report = await _reportsService.GetCustomerReportAsync();
            return View(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer report");
            return View(new Domain.DTOs.CustomerReportDto());
        }
    }

    public async Task<IActionResult> Products()
    {
        try
        {
            var report = await _reportsService.GetProductReportAsync();
            return View(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product report");
            return View(new Domain.DTOs.ProductReportDto());
        }
    }
}
