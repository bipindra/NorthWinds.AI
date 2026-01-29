namespace Northwind.Portal.Domain.Services;

public interface ITenantContext
{
    string? GetCurrentCustomerId();
    Task<string?> GetCurrentCustomerIdAsync();
    Task<IList<string>> GetCurrentRolesAsync();
    bool IsAuthorized(string? requiredRole = null, string? customerId = null);
}
