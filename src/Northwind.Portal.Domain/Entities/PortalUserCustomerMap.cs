namespace Northwind.Portal.Domain.Entities;

public class PortalUserCustomerMap
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
}
