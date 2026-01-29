namespace Northwind.Portal.Domain.Entities;

public class OrderFulfillment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public short PickedQty { get; set; }
    public DateTime? PickedAt { get; set; }
    public string? PickedByUserId { get; set; }

    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
