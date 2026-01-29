using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Northwind.Portal.Data.Contexts;
using Northwind.Portal.Domain.Services;

namespace Northwind.Portal.Data.Services;

public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NorthwindDbContext _context;
    private string? _cachedCustomerId;

    public TenantContext(IHttpContextAccessor httpContextAccessor, NorthwindDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    public string? GetCurrentCustomerId()
    {
        if (_cachedCustomerId != null)
            return _cachedCustomerId;

        var userId = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        if (string.IsNullOrEmpty(userId))
            return null;

        var map = _context.PortalUserCustomerMaps
            .AsNoTracking()
            .FirstOrDefault(m => m.UserId == userId);

        _cachedCustomerId = map?.CustomerId;
        return _cachedCustomerId;
    }

    public async Task<string?> GetCurrentCustomerIdAsync()
    {
        if (_cachedCustomerId != null)
            return _cachedCustomerId;

        var userId = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        if (string.IsNullOrEmpty(userId))
            return null;

        var map = await _context.PortalUserCustomerMaps
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId);

        _cachedCustomerId = map?.CustomerId;
        return _cachedCustomerId;
    }

    public async Task<IList<string>> GetCurrentRolesAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return new List<string>();

        var roles = user.FindAll(System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return await Task.FromResult(roles);
    }

    public bool IsAuthorized(string? requiredRole = null, string? customerId = null)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return false;

        if (!string.IsNullOrEmpty(requiredRole))
        {
            if (!user.IsInRole(requiredRole))
                return false;
        }

        if (!string.IsNullOrEmpty(customerId))
        {
            var currentCustomerId = GetCurrentCustomerId();
            if (currentCustomerId != customerId)
                return false;
        }

        return true;
    }
}
