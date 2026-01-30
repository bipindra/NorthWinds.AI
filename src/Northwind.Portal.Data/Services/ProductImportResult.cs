using Northwind.Portal.Domain.Entities;

namespace Northwind.Portal.Data.Services;

public class ProductImportResult
{
    public int SuccessCount { get; set; }
    public int TotalProcessed { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<Product> Products { get; set; } = new();
}
