using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Northwind.Portal.Data.Contexts;
using Northwind.Portal.Domain.Entities;
using System.Globalization;
using System.Text;

namespace Northwind.Portal.Data.Services;

public class ProductImportService
{
    private readonly NorthwindDbContext _context;
    private readonly ILogger<ProductImportService> _logger;

    public ProductImportService(NorthwindDbContext context, ILogger<ProductImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductImportResult> ImportFromCsvAsync(Stream csvStream)
    {
        var result = new ProductImportResult();
        var products = new List<Product>();
        var errors = new List<string>();

        try
        {
            using var reader = new StreamReader(csvStream, Encoding.UTF8);
            
            // Read header line
            var headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                result.Errors.Add("CSV file is empty or invalid");
                return result;
            }

            var headers = ParseCsvLine(headerLine);
            var expectedHeaders = new[] { "ProductName", "Description", "CategoryId", "SupplierId", "QuantityPerUnit", "UnitPrice", "UnitsInStock", "UnitsOnOrder", "ReorderLevel", "Discontinued" };
            
            // Validate headers (flexible - allow different order)
            var headerMap = new Dictionary<string, int>();
            for (int i = 0; i < headers.Length; i++)
            {
                var header = headers[i].Trim();
                if (expectedHeaders.Contains(header, StringComparer.OrdinalIgnoreCase))
                {
                    headerMap[header] = i;
                }
            }

            if (!headerMap.ContainsKey("ProductName"))
            {
                result.Errors.Add("CSV must contain 'ProductName' column");
                return result;
            }

            // Read data lines
            int lineNumber = 1;
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var values = ParseCsvLine(line);
                    if (values.Length < headerMap.Count)
                    {
                        errors.Add($"Line {lineNumber}: Not enough columns");
                        continue;
                    }

                    var product = new Product
                    {
                        ProductName = GetValue(values, headerMap, "ProductName") ?? string.Empty,
                        Description = GetValue(values, headerMap, "Description"),
                        CategoryId = ParseInt(GetValue(values, headerMap, "CategoryId")),
                        SupplierId = ParseInt(GetValue(values, headerMap, "SupplierId")),
                        QuantityPerUnit = GetValue(values, headerMap, "QuantityPerUnit"),
                        UnitPrice = ParseDecimal(GetValue(values, headerMap, "UnitPrice")),
                        UnitsInStock = ParseShort(GetValue(values, headerMap, "UnitsInStock")),
                        UnitsOnOrder = ParseShort(GetValue(values, headerMap, "UnitsOnOrder")),
                        ReorderLevel = ParseShort(GetValue(values, headerMap, "ReorderLevel")),
                        Discontinued = ParseBool(GetValue(values, headerMap, "Discontinued"))
                    };

                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(product.ProductName))
                    {
                        errors.Add($"Line {lineNumber}: ProductName is required");
                        continue;
                    }

                    // Validate CategoryId and SupplierId exist
                    if (product.CategoryId.HasValue)
                    {
                        var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == product.CategoryId.Value);
                        if (!categoryExists)
                        {
                            errors.Add($"Line {lineNumber}: CategoryId {product.CategoryId} does not exist");
                            continue;
                        }
                    }

                    if (product.SupplierId.HasValue)
                    {
                        var supplierExists = await _context.Suppliers.AnyAsync(s => s.SupplierId == product.SupplierId.Value);
                        if (!supplierExists)
                        {
                            errors.Add($"Line {lineNumber}: SupplierId {product.SupplierId} does not exist");
                            continue;
                        }
                    }

                    products.Add(product);
                }
                catch (Exception ex)
                {
                    errors.Add($"Line {lineNumber}: {ex.Message}");
                }
            }

            // Save products to database
            if (products.Any())
            {
                _context.Products.AddRange(products);
                await _context.SaveChangesAsync();
                result.SuccessCount = products.Count;
            }

            result.Errors = errors;
            result.TotalProcessed = lineNumber - 1;
            result.Products = products;

            _logger.LogInformation("Imported {Count} products from CSV. Errors: {ErrorCount}", result.SuccessCount, errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing products from CSV");
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    private string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var currentValue = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    // Toggle quote state
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue.ToString().Trim());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        values.Add(currentValue.ToString().Trim());
        return values.ToArray();
    }

    private string? GetValue(string[] values, Dictionary<string, int> headerMap, string headerName)
    {
        if (headerMap.TryGetValue(headerName, out var index) && index < values.Length)
        {
            var value = values[index].Trim();
            // Remove surrounding quotes if present
            if (value.StartsWith('"') && value.EndsWith('"'))
            {
                value = value.Substring(1, value.Length - 2);
            }
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        return null;
    }

    private int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return int.TryParse(value, out var result) ? result : null;
    }

    private short? ParseShort(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return short.TryParse(value, out var result) ? result : null;
    }

    private decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return decimal.TryParse(value, NumberStyles.Currency | NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private bool ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return bool.TryParse(value, out var result) && result || 
               value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}
