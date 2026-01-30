namespace Northwind.Portal.AI.Services;

public class ReindexResult
{
    public int TotalProducts { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
}
