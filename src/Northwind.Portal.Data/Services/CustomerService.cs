using Microsoft.EntityFrameworkCore;
using Northwind.Portal.Domain.DTOs;
using Northwind.Portal.Domain.Services;
using Northwind.Portal.Data.Contexts;

namespace Northwind.Portal.Data.Services;

public class CustomerService : ICustomerService
{
    private readonly NorthwindDbContext _context;

    public CustomerService(NorthwindDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(int page, int pageSize, string? searchTerm = null, string? country = null, string? city = null)
    {
        var query = _context.Customers
            .Include(c => c.Orders)
            .AsNoTracking()
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(c =>
                c.CompanyName.Contains(searchTerm) ||
                c.ContactName != null && c.ContactName.Contains(searchTerm) ||
                c.CustomerId.Contains(searchTerm));
        }

        // Country filter
        if (!string.IsNullOrWhiteSpace(country))
        {
            query = query.Where(c => c.Country == country);
        }

        // City filter
        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(c => c.City == city);
        }

        var totalCount = await query.CountAsync();
        
        var customers = await query
            .OrderBy(c => c.CompanyName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CompanyName = c.CompanyName,
                ContactName = c.ContactName,
                ContactTitle = c.ContactTitle,
                Address = c.Address,
                City = c.City,
                Region = c.Region,
                PostalCode = c.PostalCode,
                Country = c.Country,
                Phone = c.Phone,
                Fax = c.Fax,
                OrderCount = c.Orders.Count,
                TotalSpent = c.Orders
                    .SelectMany(o => o.OrderDetails)
                    .Sum(od => od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount)) +
                    c.Orders.Sum(o => o.Freight ?? 0),
                LastOrderDate = c.Orders.OrderByDescending(o => o.OrderDate).FirstOrDefault() != null
                    ? c.Orders.OrderByDescending(o => o.OrderDate).FirstOrDefault()!.OrderDate
                    : null
            })
            .ToListAsync();

        return new PagedResult<CustomerDto>
        {
            Items = customers,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(string customerId)
    {
        var customer = await _context.Customers
            .Include(c => c.Orders)
                .ThenInclude(o => o.OrderDetails)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (customer == null)
            return null;

        return new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CompanyName = customer.CompanyName,
            ContactName = customer.ContactName,
            ContactTitle = customer.ContactTitle,
            Address = customer.Address,
            City = customer.City,
            Region = customer.Region,
            PostalCode = customer.PostalCode,
            Country = customer.Country,
            Phone = customer.Phone,
            Fax = customer.Fax,
            OrderCount = customer.Orders.Count,
            TotalSpent = customer.Orders
                .SelectMany(o => o.OrderDetails)
                .Sum(od => od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount)) +
                customer.Orders.Sum(o => o.Freight ?? 0),
            LastOrderDate = customer.Orders.OrderByDescending(o => o.OrderDate).FirstOrDefault() != null
                ? customer.Orders.OrderByDescending(o => o.OrderDate).FirstOrDefault()!.OrderDate
                : null
        };
    }

    public async Task<bool> UpdateCustomerAsync(CustomerDto customerDto)
    {
        var customer = await _context.Customers.FindAsync(customerDto.CustomerId);
        if (customer == null)
            return false;

        customer.CompanyName = customerDto.CompanyName;
        customer.ContactName = customerDto.ContactName;
        customer.ContactTitle = customerDto.ContactTitle;
        customer.Address = customerDto.Address;
        customer.City = customerDto.City;
        customer.Region = customerDto.Region;
        customer.PostalCode = customerDto.PostalCode;
        customer.Country = customerDto.Country;
        customer.Phone = customerDto.Phone;
        customer.Fax = customerDto.Fax;

        _context.Customers.Update(customer);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<IEnumerable<string>> GetCountriesAsync()
    {
        return await _context.Customers
            .Where(c => !string.IsNullOrEmpty(c.Country))
            .Select(c => c.Country!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetCitiesAsync(string? country = null)
    {
        var query = _context.Customers
            .Where(c => !string.IsNullOrEmpty(c.City))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(country))
        {
            query = query.Where(c => c.Country == country);
        }

        return await query
            .Select(c => c.City!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }
}
