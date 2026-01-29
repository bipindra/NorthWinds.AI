using Northwind.Portal.Domain.Enums;

namespace Northwind.Portal.Domain.DTOs;

public class OrderDto
{
    public int OrderId { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? RequiredDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public int? ShipVia { get; set; }
    public string? ShipperName { get; set; }
    public decimal? Freight { get; set; }
    public string? ShipName { get; set; }
    public string? ShipAddress { get; set; }
    public string? ShipCity { get; set; }
    public string? ShipRegion { get; set; }
    public string? ShipPostalCode { get; set; }
    public string? ShipCountry { get; set; }
    public string? PoNumber { get; set; }
    public string? TrackingNumber { get; set; }
    public OrderPortalStatus CurrentStatus { get; set; }
    public List<OrderDetailDto> OrderDetails { get; set; } = new();
    public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
    public decimal SubTotal => OrderDetails.Sum(od => od.LineTotal);
    public decimal Total => SubTotal + (Freight ?? 0);
}

public class OrderDetailDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public short Quantity { get; set; }
    public float Discount { get; set; }
    public decimal LineTotal => UnitPrice * Quantity * (decimal)(1 - Discount);
}

public class OrderStatusHistoryDto
{
    public OrderPortalStatus Status { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedByUserId { get; set; }
    public string? Comment { get; set; }
}
