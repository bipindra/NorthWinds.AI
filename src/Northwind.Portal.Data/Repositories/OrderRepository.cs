using Microsoft.EntityFrameworkCore;
using Northwind.Portal.Data.Contexts;
using Northwind.Portal.Domain.Entities;

namespace Northwind.Portal.Data.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly NorthwindDbContext _context;

    public OrderRepository(NorthwindDbContext context)
    {
        _context = context;
    }

    public async Task<IQueryable<Order>> GetOrdersQueryableAsync(string? customerId = null)
    {
        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Shipper)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.OrderStatusHistories)
            .Include(o => o.OrderMeta)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(customerId))
            query = query.Where(o => o.CustomerId == customerId);

        return await Task.FromResult(query);
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId, string? customerId = null)
    {
        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Shipper)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.OrderStatusHistories)
            .Include(o => o.OrderMeta)
            .AsNoTracking()
            .Where(o => o.OrderId == orderId);

        if (!string.IsNullOrEmpty(customerId))
            query = query.Where(o => o.CustomerId == customerId);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<bool> UpdateOrderAsync(Order order)
    {
        _context.Orders.Update(order);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<IEnumerable<Shipper>> GetShippersAsync()
    {
        return await _context.Shippers
            .AsNoTracking()
            .OrderBy(s => s.CompanyName)
            .ToListAsync();
    }
}
