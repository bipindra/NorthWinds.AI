using Microsoft.EntityFrameworkCore;
using Northwind.Portal.Domain.DTOs;
using Northwind.Portal.Domain.Entities;
using Northwind.Portal.Domain.Enums;
using Northwind.Portal.Domain.Services;
using Northwind.Portal.Data.Repositories;
using Northwind.Portal.Data.Contexts;

namespace Northwind.Portal.Data.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly NorthwindDbContext _context;

    public OrderService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        IProductRepository productRepository,
        NorthwindDbContext context)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _context = context;
    }

    public async Task<OrderDto> PlaceOrderAsync(CheckoutDto checkout, string userId)
    {
        // Use execution strategy to support retry logic with transactions
        var strategy = _context.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get cart
                var cart = await _cartRepository.GetCartByUserIdAsync(userId);
                if (cart == null || !cart.CartLines.Any())
                    throw new InvalidOperationException("Cart is empty");

                // Get customer ID from tenant context (would be injected, but for now we'll get it from the user map)
                var userMap = await _context.PortalUserCustomerMaps
                    .FirstOrDefaultAsync(m => m.UserId == userId);
                
                if (userMap == null)
                    throw new InvalidOperationException("User is not mapped to a customer");

                // Validate products and get current prices
                var orderDetails = new List<OrderDetail>();
                foreach (var cartLine in cart.CartLines)
                {
                    var product = await _productRepository.GetProductByIdAsync(cartLine.ProductId);
                    if (product == null)
                        throw new InvalidOperationException($"Product {cartLine.ProductId} not found");

                    if (product.Discontinued)
                        throw new InvalidOperationException($"Product {product.ProductName} is discontinued");

                    if (product.UnitPrice == null)
                        throw new InvalidOperationException($"Product {product.ProductName} has no price");

                    orderDetails.Add(new OrderDetail
                    {
                        ProductId = cartLine.ProductId,
                        UnitPrice = product.UnitPrice.Value,
                        Quantity = cartLine.Quantity,
                        Discount = 0
                    });
                }

                // Create order
                var order = new Order
                {
                    CustomerId = userMap.CustomerId,
                    OrderDate = DateTime.UtcNow,
                    RequiredDate = DateTime.UtcNow.AddDays(7),
                    ShipName = checkout.ShipName,
                    ShipAddress = checkout.ShipAddress,
                    ShipCity = checkout.ShipCity,
                    ShipRegion = checkout.ShipRegion,
                    ShipPostalCode = checkout.ShipPostalCode,
                    ShipCountry = checkout.ShipCountry,
                    OrderDetails = orderDetails
                };

                order = await _orderRepository.CreateOrderAsync(order);

                // Create order meta
                if (!string.IsNullOrWhiteSpace(checkout.PoNumber) || !string.IsNullOrWhiteSpace(checkout.Notes))
                {
                    var orderMeta = new OrderMeta
                    {
                        OrderId = order.OrderId,
                        PoNumber = checkout.PoNumber,
                        InternalNotes = checkout.Notes
                    };
                    _context.OrderMetas.Add(orderMeta);
                }

                // Create initial status history
                var statusHistory = new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    Status = OrderPortalStatus.Submitted,
                    ChangedAt = DateTime.UtcNow,
                    ChangedByUserId = userId,
                    Comment = "Order submitted"
                };
                _context.OrderStatusHistories.Add(statusHistory);

                await _context.SaveChangesAsync();

                // Clear cart
                await _cartRepository.DeleteCartAsync(cart.Id);

                await transaction.CommitAsync();

                // Return order DTO
                return await GetOrderByIdAsync(order.OrderId, userMap.CustomerId) ?? 
                    throw new InvalidOperationException("Failed to retrieve created order");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<bool> CancelOrderAsync(int orderId, string userId, string? reason = null)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId);
        if (order == null)
            return false;

        // Check if order can be cancelled (not shipped)
        var currentStatus = order.OrderStatusHistories
            .OrderByDescending(h => h.ChangedAt)
            .FirstOrDefault()?.Status ?? OrderPortalStatus.Submitted;

        if (currentStatus == OrderPortalStatus.Shipped)
            return false; // Cannot cancel shipped orders

        // Add status history
        var statusHistory = new OrderStatusHistory
        {
            OrderId = orderId,
            Status = OrderPortalStatus.Cancelled,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = userId,
            Comment = reason ?? "Order cancelled"
        };
        _context.OrderStatusHistories.Add(statusHistory);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> TransitionStatusAsync(int orderId, OrderPortalStatus newStatus, string userId, string? comment = null)
    {
        var order = await _context.Orders
            .Include(o => o.OrderStatusHistories)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return false;

        var currentStatus = order.OrderStatusHistories
            .OrderByDescending(h => h.ChangedAt)
            .FirstOrDefault()?.Status ?? OrderPortalStatus.Submitted;

        // Validate transition
        if (!IsValidTransition(currentStatus, newStatus))
            return false;

        // Add status history
        var statusHistory = new OrderStatusHistory
        {
            OrderId = orderId,
            Status = newStatus,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = userId,
            Comment = comment
        };
        _context.OrderStatusHistories.Add(statusHistory);

        // Update order if status is Shipped
        if (newStatus == OrderPortalStatus.Shipped && order.ShippedDate == null)
        {
            order.ShippedDate = DateTime.UtcNow;
            _context.Orders.Update(order);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int orderId, string? customerId = null)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, customerId);
        if (order == null)
            return null;

        var currentStatus = order.OrderStatusHistories
            .OrderByDescending(h => h.ChangedAt)
            .FirstOrDefault()?.Status ?? OrderPortalStatus.Submitted;

        return new OrderDto
        {
            OrderId = order.OrderId,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.CompanyName,
            OrderDate = order.OrderDate,
            RequiredDate = order.RequiredDate,
            ShippedDate = order.ShippedDate,
            ShipVia = order.ShipVia,
            ShipperName = order.Shipper?.CompanyName,
            Freight = order.Freight,
            ShipName = order.ShipName,
            ShipAddress = order.ShipAddress,
            ShipCity = order.ShipCity,
            ShipRegion = order.ShipRegion,
            ShipPostalCode = order.ShipPostalCode,
            ShipCountry = order.ShipCountry,
            PoNumber = order.OrderMeta?.PoNumber,
            TrackingNumber = order.OrderMeta?.TrackingNumber,
            CurrentStatus = currentStatus,
            OrderDetails = order.OrderDetails.Select(od => new OrderDetailDto
            {
                ProductId = od.ProductId,
                ProductName = od.Product.ProductName,
                UnitPrice = od.UnitPrice,
                Quantity = od.Quantity,
                Discount = od.Discount
            }).ToList(),
            StatusHistory = order.OrderStatusHistories.Select(h => new OrderStatusHistoryDto
            {
                Status = h.Status,
                ChangedAt = h.ChangedAt,
                ChangedByUserId = h.ChangedByUserId,
                Comment = h.Comment
            }).OrderBy(h => h.ChangedAt).ToList()
        };
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByCustomerIdAsync(string customerId)
    {
        var query = await _orderRepository.GetOrdersQueryableAsync(customerId);
        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return orders.Select(o =>
        {
            var currentStatus = o.OrderStatusHistories
                .OrderByDescending(h => h.ChangedAt)
                .FirstOrDefault()?.Status ?? OrderPortalStatus.Submitted;

            return new OrderDto
            {
                OrderId = o.OrderId,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.CompanyName,
                OrderDate = o.OrderDate,
                RequiredDate = o.RequiredDate,
                ShippedDate = o.ShippedDate,
                ShipVia = o.ShipVia,
                ShipperName = o.Shipper?.CompanyName,
                Freight = o.Freight,
                ShipName = o.ShipName,
                ShipAddress = o.ShipAddress,
                ShipCity = o.ShipCity,
                ShipRegion = o.ShipRegion,
                ShipPostalCode = o.ShipPostalCode,
                ShipCountry = o.ShipCountry,
                PoNumber = o.OrderMeta?.PoNumber,
                TrackingNumber = o.OrderMeta?.TrackingNumber,
                CurrentStatus = currentStatus,
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailDto
                {
                    ProductId = od.ProductId,
                    ProductName = od.Product.ProductName,
                    UnitPrice = od.UnitPrice,
                    Quantity = od.Quantity,
                    Discount = od.Discount
                }).ToList(),
                StatusHistory = o.OrderStatusHistories.Select(h => new OrderStatusHistoryDto
                {
                    Status = h.Status,
                    ChangedAt = h.ChangedAt,
                    ChangedByUserId = h.ChangedByUserId,
                    Comment = h.Comment
                }).OrderBy(h => h.ChangedAt).ToList()
            };
        });
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(OrderPortalStatus? status = null)
    {
        var query = await _orderRepository.GetOrdersQueryableAsync();
        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var orderDtos = orders.Select(o =>
        {
            var currentStatus = o.OrderStatusHistories
                .OrderByDescending(h => h.ChangedAt)
                .FirstOrDefault()?.Status ?? OrderPortalStatus.Submitted;

            return new { Order = o, CurrentStatus = currentStatus };
        });

        if (status.HasValue)
        {
            orderDtos = orderDtos.Where(x => x.CurrentStatus == status.Value);
        }

        return orderDtos.Select(x => new OrderDto
        {
            OrderId = x.Order.OrderId,
            CustomerId = x.Order.CustomerId,
            CustomerName = x.Order.Customer?.CompanyName,
            OrderDate = x.Order.OrderDate,
            RequiredDate = x.Order.RequiredDate,
            ShippedDate = x.Order.ShippedDate,
            ShipVia = x.Order.ShipVia,
            ShipperName = x.Order.Shipper?.CompanyName,
            Freight = x.Order.Freight,
            ShipName = x.Order.ShipName,
            ShipAddress = x.Order.ShipAddress,
            ShipCity = x.Order.ShipCity,
            ShipRegion = x.Order.ShipRegion,
            ShipPostalCode = x.Order.ShipPostalCode,
            ShipCountry = x.Order.ShipCountry,
            PoNumber = x.Order.OrderMeta?.PoNumber,
            TrackingNumber = x.Order.OrderMeta?.TrackingNumber,
            CurrentStatus = x.CurrentStatus,
            OrderDetails = x.Order.OrderDetails.Select(od => new OrderDetailDto
            {
                ProductId = od.ProductId,
                ProductName = od.Product.ProductName,
                UnitPrice = od.UnitPrice,
                Quantity = od.Quantity,
                Discount = od.Discount
            }).ToList(),
            StatusHistory = x.Order.OrderStatusHistories.Select(h => new OrderStatusHistoryDto
            {
                Status = h.Status,
                ChangedAt = h.ChangedAt,
                ChangedByUserId = h.ChangedByUserId,
                Comment = h.Comment
            }).OrderBy(h => h.ChangedAt).ToList()
        });
    }

    private static bool IsValidTransition(OrderPortalStatus currentStatus, OrderPortalStatus newStatus)
    {
        return newStatus switch
        {
            OrderPortalStatus.PendingApproval => currentStatus == OrderPortalStatus.Submitted,
            OrderPortalStatus.Approved => currentStatus == OrderPortalStatus.PendingApproval || currentStatus == OrderPortalStatus.Submitted,
            OrderPortalStatus.Picking => currentStatus == OrderPortalStatus.Approved,
            OrderPortalStatus.Shipped => currentStatus == OrderPortalStatus.Picking,
            OrderPortalStatus.Cancelled => currentStatus != OrderPortalStatus.Shipped && currentStatus != OrderPortalStatus.Cancelled,
            OrderPortalStatus.Rejected => currentStatus == OrderPortalStatus.PendingApproval,
            _ => false
        };
    }
}
