using Microsoft.EntityFrameworkCore;
using Northwind.Portal.Domain.DTOs;
using Northwind.Portal.Domain.Enums;
using Northwind.Portal.Domain.Services;
using Northwind.Portal.Data.Contexts;

namespace Northwind.Portal.Data.Services;

public class ReportsService : IReportsService
{
    private readonly NorthwindDbContext _context;

    public ReportsService(NorthwindDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfYear = new DateTime(now.Year, 1, 1);

        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .Include(o => o.OrderStatusHistories)
            .AsNoTracking()
            .ToListAsync();

        var orderStatusCounts = orders
            .GroupBy(o => o.OrderStatusHistories.OrderByDescending(h => h.ChangedAt).FirstOrDefault()?.Status ?? OrderPortalStatus.Submitted)
            .Select(g => new OrderStatusCountDto
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToList();

        var totalRevenue = orders
            .SelectMany(o => o.OrderDetails)
            .Sum(od => od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount)) + 
            orders.Sum(o => o.Freight ?? 0);

        var monthlyRevenue = orders
            .Where(o => o.OrderDate >= startOfMonth)
            .SelectMany(o => o.OrderDetails)
            .Sum(od => od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount)) +
            orders.Where(o => o.OrderDate >= startOfMonth).Sum(o => o.Freight ?? 0);

        var monthlyRevenues = orders
            .Where(o => o.OrderDate >= startOfYear)
            .GroupBy(o => new { o.OrderDate.Value.Year, o.OrderDate.Value.Month })
            .Select(g => new MonthlyRevenueDto
            {
                Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                Revenue = g.SelectMany(o => o.OrderDetails).Sum(od => od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount)) +
                         g.Sum(o => o.Freight ?? 0),
                OrderCount = g.Count()
            })
            .OrderBy(m => m.Month)
            .ToList();

        var topCustomers = orders
            .GroupBy(o => new { o.CustomerId, CustomerName = o.Customer != null ? o.Customer.CompanyName : "Unknown" })
            .Select(g => new TopCustomerDto
            {
                CustomerId = g.Key.CustomerId ?? "",
                CustomerName = g.Key.CustomerName,
                TotalSpent = g.SelectMany(o => o.OrderDetails).Sum(od => od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount)) +
                            g.Sum(o => o.Freight ?? 0),
                OrderCount = g.Count()
            })
            .OrderByDescending(c => c.TotalSpent)
            .Take(5)
            .ToList();

        var topProducts = orders
            .SelectMany(o => o.OrderDetails)
            .GroupBy(od => new { od.ProductId, ProductName = od.Product != null ? od.Product.ProductName : "Unknown" })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(od => od.Quantity),
                Revenue = g.Sum(od => od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount))
            })
            .OrderByDescending(p => p.Revenue)
            .Take(5)
            .ToList();

        var customers = await _context.Customers.AsNoTracking().ToListAsync();
        var activeCustomers = customers
            .Where(c => orders.Any(o => o.CustomerId == c.CustomerId && o.OrderDate >= startOfMonth))
            .Count();

        var products = await _context.Products.AsNoTracking().ToListAsync();
        var lowStockProducts = products.Count(p => p.UnitsInStock.HasValue && p.UnitsInStock.Value <= (p.ReorderLevel ?? 0));

        return new DashboardStatsDto
        {
            TotalOrders = orders.Count,
            PendingOrders = orderStatusCounts.Where(s => s.Status == OrderPortalStatus.PendingApproval.ToString() || s.Status == OrderPortalStatus.Approved.ToString()).Sum(s => s.Count),
            ShippedOrders = orderStatusCounts.Where(s => s.Status == OrderPortalStatus.Shipped.ToString()).Sum(s => s.Count),
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue,
            TotalCustomers = customers.Count,
            ActiveCustomers = activeCustomers,
            TotalProducts = products.Count,
            LowStockProducts = lowStockProducts,
            OrderStatusCounts = orderStatusCounts,
            MonthlyRevenues = monthlyRevenues,
            TopCustomers = topCustomers,
            TopProducts = topProducts
        };
    }

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Orders
            .Include(o => o.OrderDetails)
            .AsNoTracking()
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(o => o.OrderDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(o => o.OrderDate <= endDate.Value);

        var orders = await query.ToListAsync();

        var totalSales = orders
            .SelectMany(o => o.OrderDetails)
            .Sum(od => od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount)) +
            orders.Sum(o => o.Freight ?? 0);

        var totalOrders = orders.Count;
        var averageOrderValue = totalOrders > 0 ? totalSales / totalOrders : 0;

        var dailySales = orders
            .Where(o => o.OrderDate.HasValue)
            .GroupBy(o => o.OrderDate.Value.Date)
            .Select(g => new DailySalesDto
            {
                Date = g.Key,
                Sales = g.SelectMany(o => o.OrderDetails).Sum(od => od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount)) +
                       g.Sum(o => o.Freight ?? 0),
                OrderCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new SalesReportDto
        {
            TotalSales = totalSales,
            TotalOrders = totalOrders,
            AverageOrderValue = averageOrderValue,
            DailySales = dailySales
        };
    }

    public async Task<CustomerReportDto> GetCustomerReportAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var customers = await _context.Customers.AsNoTracking().ToListAsync();
        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .AsNoTracking()
            .ToListAsync();

        var activeCustomers = customers
            .Where(c => orders.Any(o => o.CustomerId == c.CustomerId && o.OrderDate >= startOfMonth))
            .Count();

        var newCustomersThisMonth = customers
            .Count(c => orders.Any(o => o.CustomerId == c.CustomerId && 
                                       o.OrderDate >= startOfMonth && 
                                       !orders.Any(o2 => o2.CustomerId == c.CustomerId && o2.OrderDate < startOfMonth)));

        var topCustomers = orders
            .GroupBy(o => new { o.CustomerId, CustomerName = o.Customer != null ? o.Customer.CompanyName : "Unknown" })
            .Select(g => new TopCustomerDto
            {
                CustomerId = g.Key.CustomerId ?? "",
                CustomerName = g.Key.CustomerName,
                TotalSpent = g.SelectMany(o => o.OrderDetails).Sum(od => od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount)) +
                            g.Sum(o => o.Freight ?? 0),
                OrderCount = g.Count()
            })
            .OrderByDescending(c => c.TotalSpent)
            .Take(10)
            .ToList();

        return new CustomerReportDto
        {
            TotalCustomers = customers.Count,
            ActiveCustomers = activeCustomers,
            NewCustomersThisMonth = newCustomersThisMonth,
            TopCustomers = topCustomers
        };
    }

    public async Task<ProductReportDto> GetProductReportAsync()
    {
        var products = await _context.Products.AsNoTracking().ToListAsync();
        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .AsNoTracking()
            .ToListAsync();

        var activeProducts = products.Count(p => !p.Discontinued);
        var lowStockProducts = products.Count(p => p.UnitsInStock.HasValue && p.UnitsInStock.Value <= (p.ReorderLevel ?? 0));
        var discontinuedProducts = products.Count(p => p.Discontinued);

        var topSellingProducts = orders
            .SelectMany(o => o.OrderDetails)
            .GroupBy(od => new { od.ProductId, ProductName = od.Product != null ? od.Product.ProductName : "Unknown" })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(od => od.Quantity),
                Revenue = g.Sum(od => od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount))
            })
            .OrderByDescending(p => p.Revenue)
            .Take(10)
            .ToList();

        var lowStockProductsList = products
            .Where(p => p.UnitsInStock.HasValue && p.UnitsInStock.Value <= (p.ReorderLevel ?? 0))
            .Select(p => new TopProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                QuantitySold = p.UnitsInStock ?? 0,
                Revenue = 0
            })
            .OrderBy(p => p.QuantitySold)
            .Take(10)
            .ToList();

        return new ProductReportDto
        {
            TotalProducts = products.Count,
            ActiveProducts = activeProducts,
            LowStockProducts = lowStockProducts,
            DiscontinuedProducts = discontinuedProducts,
            TopSellingProducts = topSellingProducts,
            LowStockProductsList = lowStockProductsList
        };
    }
}
