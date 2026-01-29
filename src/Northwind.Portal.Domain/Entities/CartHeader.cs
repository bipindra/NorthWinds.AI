namespace Northwind.Portal.Domain.Entities;

public class CartHeader
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<CartLine> CartLines { get; set; } = new List<CartLine>();
}
