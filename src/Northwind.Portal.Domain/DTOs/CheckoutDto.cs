namespace Northwind.Portal.Domain.DTOs;

public class CheckoutDto
{
    public string ShipName { get; set; } = null!;
    public string ShipAddress { get; set; } = null!;
    public string ShipCity { get; set; } = null!;
    public string? ShipRegion { get; set; }
    public string? ShipPostalCode { get; set; }
    public string ShipCountry { get; set; } = null!;
    public string? PoNumber { get; set; }
    public string? Notes { get; set; }
}
