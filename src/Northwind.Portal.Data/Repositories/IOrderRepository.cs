using Northwind.Portal.Domain.Entities;

namespace Northwind.Portal.Data.Repositories;

public interface IOrderRepository
{
    Task<IQueryable<Order>> GetOrdersQueryableAsync(string? customerId = null);
    Task<Order?> GetOrderByIdAsync(int orderId, string? customerId = null);
    Task<Order> CreateOrderAsync(Order order);
    Task<bool> UpdateOrderAsync(Order order);
    Task<IEnumerable<Shipper>> GetShippersAsync();
}
