namespace Northwind.Portal.Domain.DTOs;

public class DatabaseOptions
{
    public const string SectionName = "DatabaseOptions";
    public string DbProvider { get; set; } = "SqlServer"; // "SqlServer" or "Sqlite"
}

public class FeatureFlags
{
    public const string SectionName = "FeatureFlags";
    public bool EnableApprovals { get; set; } = true;
}
