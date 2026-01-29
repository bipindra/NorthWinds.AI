namespace Northwind.Portal.Domain.Entities;

public class CartLine
{
    public int Id { get; set; }
    public int CartHeaderId { get; set; }
    public int ProductId { get; set; }
    public short Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Navigation properties
    public virtual CartHeader CartHeader { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
