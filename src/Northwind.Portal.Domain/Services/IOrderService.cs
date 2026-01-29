using Northwind.Portal.Domain.DTOs;
using Northwind.Portal.Domain.Enums;

namespace Northwind.Portal.Domain.Services;

public interface IOrderService
{
    Task<OrderDto> PlaceOrderAsync(CheckoutDto checkout, string userId);
    Task<bool> CancelOrderAsync(int orderId, string userId, string? reason = null);
    Task<bool> TransitionStatusAsync(int orderId, OrderPortalStatus newStatus, string userId, string? comment = null);
    Task<OrderDto?> GetOrderByIdAsync(int orderId, string? customerId = null);
    Task<IEnumerable<OrderDto>> GetOrdersByCustomerIdAsync(string customerId);
    Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(OrderPortalStatus? status = null);
}
