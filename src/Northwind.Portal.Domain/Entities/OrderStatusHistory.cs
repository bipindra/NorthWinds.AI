using Northwind.Portal.Domain.Enums;

namespace Northwind.Portal.Domain.Entities;

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public OrderPortalStatus Status { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? ChangedByUserId { get; set; }
    public string? Comment { get; set; }

    // Navigation properties
    public virtual Order Order { get; set; } = null!;
}
