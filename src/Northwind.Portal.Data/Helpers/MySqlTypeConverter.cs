using Microsoft.EntityFrameworkCore;

namespace Northwind.Portal.Data.Helpers;

/// <summary>
/// Helper class for converting SQL Server column types to MySQL/MariaDB equivalents.
/// This ensures compatibility when using MySQL/MariaDB provider with EF Core models
/// that were originally designed for SQL Server.
/// </summary>
public static class MySqlTypeConverter
{
    /// <summary>
    /// Applies MySQL type conversions to all entities in the model builder.
    /// Converts SQL Server-specific types (nvarchar, ntext, datetime2, money, bit, image)
    /// to their MySQL equivalents (varchar, longtext, datetime, decimal, tinyint, longblob).
    /// </summary>
    /// <param name="modelBuilder">The model builder to apply conversions to.</param>
    public static void ApplyMySqlTypeConversions(ModelBuilder modelBuilder)
    {
        // Always apply type conversions when SQL Server types are detected
        // This ensures compatibility when using MySQL/MariaDB provider
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var columnType = property.GetColumnType();
                var maxLength = property.GetMaxLength();

                // Convert SQL Server string types to MySQL equivalents
                if (property.ClrType == typeof(string))
                {
                    // Always convert ntext to longtext
                    if (columnType?.Equals("ntext", StringComparison.OrdinalIgnoreCase) == true ||
                        columnType?.StartsWith("nvarchar(max)", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        property.SetColumnType("longtext");
                    }
                    // Convert nvarchar to varchar
                    else if (columnType?.StartsWith("nvarchar", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        property.SetColumnType(maxLength.HasValue && maxLength.Value > 0 
                            ? $"varchar({maxLength.Value})" 
                            : "varchar(255)");
                    }
                    // Convert nchar to char
                    else if (columnType?.StartsWith("nchar", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        property.SetColumnType(maxLength.HasValue && maxLength.Value > 0 
                            ? $"char({maxLength.Value})" 
                            : "char(255)");
                    }
                }
                // Convert SQL Server DateTime types to MySQL equivalents
                else if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    if (columnType?.Equals("datetime2", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        property.SetColumnType("datetime(6)");
                    }
                    else if (columnType?.Equals("datetime", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        // Keep datetime but ensure it's compatible
                        property.SetColumnType("datetime");
                    }
                }
                // Convert SQL Server money type to MySQL decimal
                else if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    if (columnType?.Equals("money", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        property.SetColumnType("decimal(19,4)");
                    }
                }
                // Convert SQL Server bit type to MySQL boolean/tinyint
                else if (property.ClrType == typeof(bool) || property.ClrType == typeof(bool?))
                {
                    if (columnType?.Equals("bit", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        property.SetColumnType("tinyint(1)");
                    }
                }
                // Convert SQL Server image type to MySQL longblob
                else if (property.ClrType == typeof(byte[]))
                {
                    if (columnType?.Equals("image", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        property.SetColumnType("longblob");
                    }
                }
            }
        }
    }
}
