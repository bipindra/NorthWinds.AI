namespace Northwind.Portal.Domain.DTOs;

public class DashboardStatsDto
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ShippedOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public List<OrderStatusCountDto> OrderStatusCounts { get; set; } = new();
    public List<MonthlyRevenueDto> MonthlyRevenues { get; set; } = new();
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
}

public class OrderStatusCountDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MonthlyRevenueDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class TopCustomerDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
    public int OrderCount { get; set; }
}

public class TopProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class SalesReportDto
{
    public decimal TotalSales { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<DailySalesDto> DailySales { get; set; } = new();
}

public class DailySalesDto
{
    public DateTime Date { get; set; }
    public decimal Sales { get; set; }
    public int OrderCount { get; set; }
}

public class CustomerReportDto
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int NewCustomersThisMonth { get; set; }
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
}

public class ProductReportDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int DiscontinuedProducts { get; set; }
    public List<TopProductDto> TopSellingProducts { get; set; } = new();
    public List<TopProductDto> LowStockProductsList { get; set; } = new();
}
