using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Northwind.Portal.Domain.DTOs;

namespace Northwind.Portal.Data.Helpers;

/// <summary>
/// Helper class for database configuration and connection string parsing.
/// Handles auto-detection of database provider and normalization of connection strings.
/// </summary>
public static class DatabaseConfigurationHelper
{
    /// <summary>
    /// Configures database options by auto-detecting the provider from connection string if needed,
    /// and normalizes the connection string for the detected provider.
    /// </summary>
    /// <param name="configuration">The configuration to read database options from.</param>
    /// <param name="connectionStringName">The name of the connection string in configuration.</param>
    /// <returns>A tuple containing the database options and normalized connection string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when connection string is missing or invalid.</exception>
    public static (DatabaseOptions Options, string ConnectionString) ConfigureDatabase(
        IConfiguration configuration,
        string connectionStringName = "NorthwindsDb")
    {
        // Get database options from configuration
        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() 
            ?? new DatabaseOptions { DbProvider = "SqlServer" };

        // Get connection string
        var connectionString = configuration.GetConnectionString(connectionStringName) 
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' not found. Please set it in User Secrets (Development) or appsettings.json (Production).");

        // Auto-detect database provider from connection string if not explicitly set or if it's SqlServer but connection string looks like MariaDB
        if (databaseOptions.DbProvider == "SqlServer" || string.IsNullOrEmpty(databaseOptions.DbProvider))
        {
            var connStringLower = connectionString.ToLowerInvariant();
            // Check for MariaDB/MySQL connection string indicators
            if (connStringLower.Contains("host=", StringComparison.OrdinalIgnoreCase) || 
                (connStringLower.Contains("server=", StringComparison.OrdinalIgnoreCase) && (connStringLower.Contains("port=", StringComparison.OrdinalIgnoreCase) || connStringLower.Contains(":3306", StringComparison.OrdinalIgnoreCase))) ||
                connStringLower.Contains("user=", StringComparison.OrdinalIgnoreCase) || 
                connStringLower.Contains("uid=", StringComparison.OrdinalIgnoreCase) ||
                (connStringLower.Contains("database=", StringComparison.OrdinalIgnoreCase) && !connStringLower.Contains("trusted_connection", StringComparison.OrdinalIgnoreCase)))
            {
                databaseOptions.DbProvider = "MariaDB";
                Console.WriteLine($"[Auto-Detection] Detected MariaDB connection string format. Setting DbProvider to MariaDB.");
            }
        }

        // Log the DbProvider being used
        Console.WriteLine($"[Configuration] DbProvider: {databaseOptions.DbProvider}");

        // Parse and normalize connection string for MariaDB
        if (databaseOptions.DbProvider == "MariaDB")
        {
            connectionString = NormalizeMariaDbConnectionString(connectionString);
        }

        return (databaseOptions, connectionString);
    }

    /// <summary>
    /// Normalizes a MariaDB connection string by handling various parameter formats and validating required parameters.
    /// </summary>
    /// <param name="connectionString">The connection string to normalize.</param>
    /// <returns>The normalized connection string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when connection string is invalid or missing required parameters.</exception>
    private static string NormalizeMariaDbConnectionString(string connectionString)
    {
        try
        {
            // Handle "Host" parameter by converting it to "Server" before parsing
            var normalizedConnString = connectionString;
            if (normalizedConnString.Contains("Host=", StringComparison.OrdinalIgnoreCase) && 
                !normalizedConnString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
            {
                normalizedConnString = System.Text.RegularExpressions.Regex.Replace(
                    normalizedConnString, 
                    @"Host\s*=", 
                    "Server=", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            
            var connBuilder = new MySqlConnectionStringBuilder(normalizedConnString);
            
            // Normalize common parameter names
            // Handle both 'User' and 'Uid' parameters
            if (string.IsNullOrEmpty(connBuilder.UserID) && !string.IsNullOrEmpty(normalizedConnString))
            {
                // Try to extract from connection string manually if MySqlConnectionStringBuilder didn't parse it
                var userMatch = System.Text.RegularExpressions.Regex.Match(normalizedConnString, @"(?:User|Uid|UserID|UserId)\s*=\s*([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (userMatch.Success)
                {
                    connBuilder.UserID = userMatch.Groups[1].Value.Trim();
                }
            }
            
            // Handle both 'Password' and 'Pwd' parameters
            if (string.IsNullOrEmpty(connBuilder.Password) && !string.IsNullOrEmpty(normalizedConnString))
            {
                var pwdMatch = System.Text.RegularExpressions.Regex.Match(normalizedConnString, @"(?:Password|Pwd)\s*=\s*([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (pwdMatch.Success)
                {
                    connBuilder.Password = pwdMatch.Groups[1].Value.Trim();
                }
            }
            
            // Handle Server with port (e.g., "Server=host:port" or "Server=host;Port=3306")
            if (!string.IsNullOrEmpty(connBuilder.Server) && connBuilder.Server.Contains(':'))
            {
                var parts = connBuilder.Server.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out var port))
                {
                    connBuilder.Server = parts[0];
                    connBuilder.Port = (uint)port;
                }
            }
            
            // Validate required parameters
            if (string.IsNullOrEmpty(connBuilder.Database))
            {
                throw new InvalidOperationException(
                    $"Database name is missing from connection string. " +
                    $"Parsed: Server={connBuilder.Server}, Port={connBuilder.Port}, User={connBuilder.UserID}");
            }
            
            if (string.IsNullOrEmpty(connBuilder.Server))
            {
                throw new InvalidOperationException(
                    $"Server is missing from connection string. " +
                    $"Parsed: Database={connBuilder.Database}, Port={connBuilder.Port}, User={connBuilder.UserID}");
            }
            
            if (string.IsNullOrEmpty(connBuilder.UserID))
            {
                throw new InvalidOperationException(
                    $"User is missing from connection string. " +
                    $"Parsed: Server={connBuilder.Server}, Port={connBuilder.Port}, Database={connBuilder.Database}");
            }
            
            // Rebuild connection string with normalized format
            var normalized = connBuilder.ConnectionString;
            Console.WriteLine($"[Connection String] Normalized MariaDB connection string: Server={connBuilder.Server}, Port={connBuilder.Port}, Database={connBuilder.Database}, User={connBuilder.UserID}");
            return normalized;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse MariaDB connection string: {ex.Message}. " +
                $"Original connection string format may be invalid. " +
                $"Expected format: Server=host:port;Database=dbname;User=username;Password=password", ex);
        }
    }
}
