namespace Northwind.Portal.Domain.Entities;

public class Shipper
{
    public int ShipperId { get; set; }
    public string CompanyName { get; set; } = null!;
    public string? Phone { get; set; }

    // Navigation properties
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
