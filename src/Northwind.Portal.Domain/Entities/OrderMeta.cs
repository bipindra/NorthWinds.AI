namespace Northwind.Portal.Domain.Entities;

public class OrderMeta
{
    public int OrderId { get; set; }
    public string? PoNumber { get; set; }
    public string? InternalNotes { get; set; }
    public string? TrackingNumber { get; set; }

    // Navigation properties
    public virtual Order Order { get; set; } = null!;
}
