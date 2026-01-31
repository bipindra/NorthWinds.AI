using Northwind.Portal.Domain.DTOs;

namespace Northwind.Portal.Domain.Services;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(int page, int pageSize, string? searchTerm = null, string? country = null, string? city = null);
    Task<CustomerDto?> GetCustomerByIdAsync(string customerId);
    Task<bool> UpdateCustomerAsync(CustomerDto customer);
    Task<IEnumerable<string>> GetCountriesAsync();
    Task<IEnumerable<string>> GetCitiesAsync(string? country = null);
}
