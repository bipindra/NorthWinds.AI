using Northwind.Portal.Domain.DTOs;

namespace Northwind.Portal.Domain.Services;

public interface IReportsService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<SalesReportDto> GetSalesReportAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<CustomerReportDto> GetCustomerReportAsync();
    Task<ProductReportDto> GetProductReportAsync();
}
