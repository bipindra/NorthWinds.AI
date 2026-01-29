namespace Northwind.Portal.Domain.DTOs;

public class SupplierDto
{
    public int SupplierId { get; set; }
    public string CompanyName { get; set; } = null!;
    public string? ContactName { get; set; }
}
