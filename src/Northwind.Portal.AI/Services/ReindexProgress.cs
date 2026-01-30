namespace Northwind.Portal.AI.Services;

public class ReindexProgress
{
    public int CurrentIndex { get; set; }
    public int TotalProducts { get; set; }
    public string? CurrentProductName { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public double Percentage => TotalProducts > 0 ? (double)CurrentIndex / TotalProducts * 100 : 0;
}
