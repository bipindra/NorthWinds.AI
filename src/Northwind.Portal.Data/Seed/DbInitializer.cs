using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Northwind.Portal.Data.Contexts;
using Northwind.Portal.Domain.Entities;
using Northwind.Portal.Domain.Enums;

namespace Northwind.Portal.Data.Seed;

public class DbInitializer
{
    private readonly NorthwindDbContext _context;
    private readonly ApplicationDbContext _identityContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DbInitializer(
        NorthwindDbContext context,
        ApplicationDbContext identityContext,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        Microsoft.Extensions.Logging.ILogger<DbInitializer> initializerLogger)
    {
        _context = context;
        _identityContext = identityContext;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitializeAsync()
    {
        // Initialize Identity database
        try
        {
            var identityMigrations = await _identityContext.Database.GetPendingMigrationsAsync();
            if (identityMigrations.Any())
            {
                await _identityContext.Database.MigrateAsync();
            }
            else if (!await _identityContext.Database.CanConnectAsync())
            {
                // If database doesn't exist and no migrations, create it
                await _identityContext.Database.EnsureCreatedAsync();
            }
        }
        catch (InvalidOperationException)
        {
            // If there are pending model changes (no migrations), use EnsureCreated
            if (!await _identityContext.Database.CanConnectAsync())
            {
                await _identityContext.Database.EnsureCreatedAsync();
            }
        }

        // Initialize Northwind database
        try
        {
            var northwindMigrations = await _context.Database.GetPendingMigrationsAsync();
            if (northwindMigrations.Any())
            {
                await _context.Database.MigrateAsync();
            }
            else if (!await _context.Database.CanConnectAsync())
            {
                // If database doesn't exist and no migrations, create it
                await _context.Database.EnsureCreatedAsync();
            }
        }
        catch (InvalidOperationException)
        {
            // If there are pending model changes (no migrations), use EnsureCreated
            if (!await _context.Database.CanConnectAsync())
            {
                await _context.Database.EnsureCreatedAsync();
            }
        }

        // Seed roles
        await SeedRolesAsync();

        // Seed users
        await SeedUsersAsync();

        // Seed Northwind data if empty
        await SeedNorthwindDataAsync();
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            "CustomerUser",
            "CustomerApprover",
            "AdminOps",
            "AdminCatalog",
            "AdminFulfillment",
            "SuperAdmin"
        };

        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private async Task SeedUsersAsync()
    {
        // SuperAdmin
        var adminEmail = "admin@northwind.com";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var result = await _userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "SuperAdmin");
            }
        }

        // AdminOps
        var opsEmail = "ops@northwind.com";
        var opsUser = await _userManager.FindByEmailAsync(opsEmail);
        if (opsUser == null)
        {
            opsUser = new IdentityUser
            {
                UserName = opsEmail,
                Email = opsEmail,
                EmailConfirmed = true
            };
            var result = await _userManager.CreateAsync(opsUser, "Ops123!");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(opsUser, "AdminOps");
                await _userManager.AddToRoleAsync(opsUser, "AdminFulfillment");
            }
        }

        // Customer User 1 (ALFKI)
        var customer1Email = "customer1@example.com";
        var customer1User = await _userManager.FindByEmailAsync(customer1Email);
        if (customer1User == null)
        {
            customer1User = new IdentityUser
            {
                UserName = customer1Email,
                Email = customer1Email,
                EmailConfirmed = true
            };
            var result = await _userManager.CreateAsync(customer1User, "Customer123!");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(customer1User, "CustomerUser");

                // Map to customer ALFKI
                var customer = await _context.Customers.FindAsync("ALFKI");
                if (customer != null)
                {
                    var map = new PortalUserCustomerMap
                    {
                        UserId = customer1User.Id,
                        CustomerId = "ALFKI",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.PortalUserCustomerMaps.Add(map);
                    await _context.SaveChangesAsync();
                }
            }
        }

        // Customer User 2 (ANATR)
        var customer2Email = "customer2@example.com";
        var customer2User = await _userManager.FindByEmailAsync(customer2Email);
        if (customer2User == null)
        {
            customer2User = new IdentityUser
            {
                UserName = customer2Email,
                Email = customer2Email,
                EmailConfirmed = true
            };
            var result = await _userManager.CreateAsync(customer2User, "Customer123!");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(customer2User, "CustomerUser");

                // Map to customer ANATR
                var customer = await _context.Customers.FindAsync("ANATR");
                if (customer != null)
                {
                    var map = new PortalUserCustomerMap
                    {
                        UserId = customer2User.Id,
                        CustomerId = "ANATR",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.PortalUserCustomerMaps.Add(map);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }

    private async Task SeedNorthwindDataAsync()
    {
        // Only seed if database is empty
        if (await _context.Customers.AnyAsync())
            return;

        // Seed minimal Northwind data for demo
        // Note: In production, you would load from the actual Northwind database
        // For now, we'll create a few sample records

        // Create sample customers
        var customers = new[]
        {
            new Customer { CustomerId = "ALFKI", CompanyName = "Alfreds Futterkiste", ContactName = "Maria Anders", City = "Berlin", Country = "Germany" },
            new Customer { CustomerId = "ANATR", CompanyName = "Ana Trujillo Emparedados y helados", ContactName = "Ana Trujillo", City = "México D.F.", Country = "Mexico" },
            new Customer { CustomerId = "ANTON", CompanyName = "Antonio Moreno Taquería", ContactName = "Antonio Moreno", City = "México D.F.", Country = "Mexico" }
        };
        _context.Customers.AddRange(customers);

        // Create sample categories
        var categories = new[]
        {
            new Category { CategoryName = "Beverages", Description = "Soft drinks, coffees, teas, beers, and ales" },
            new Category { CategoryName = "Condiments", Description = "Sweet and savory sauces, relishes, spreads, and seasonings" },
            new Category { CategoryName = "Confections", Description = "Desserts, candies, and sweet breads" },
            new Category { CategoryName = "Dairy Products", Description = "Cheeses" },
            new Category { CategoryName = "Grains/Cereals", Description = "Breads, crackers, pasta, and cereal" }
        };
        _context.Categories.AddRange(categories);

        // Create sample suppliers
        var suppliers = new[]
        {
            new Supplier { CompanyName = "Exotic Liquids", ContactName = "Charlotte Cooper", City = "London", Country = "UK" },
            new Supplier { CompanyName = "New Orleans Cajun Delights", ContactName = "Shelley Burke", City = "New Orleans", Country = "USA" },
            new Supplier { CompanyName = "Grandma Kelly's Homestead", ContactName = "Regina Murphy", City = "Ann Arbor", Country = "USA" }
        };
        _context.Suppliers.AddRange(suppliers);

        // Create sample shippers
        var shippers = new[]
        {
            new Shipper { CompanyName = "Speedy Express", Phone = "(503) 555-9831" },
            new Shipper { CompanyName = "United Package", Phone = "(503) 555-3199" },
            new Shipper { CompanyName = "Federal Shipping", Phone = "(503) 555-9931" }
        };
        _context.Shippers.AddRange(shippers);

        await _context.SaveChangesAsync();

        // Create sample products
        var products = new[]
        {
            new Product { ProductName = "Chai", CategoryId = 1, SupplierId = 1, UnitPrice = 18.00m, UnitsInStock = 39, Discontinued = false },
            new Product { ProductName = "Chang", CategoryId = 1, SupplierId = 1, UnitPrice = 19.00m, UnitsInStock = 17, Discontinued = false },
            new Product { ProductName = "Aniseed Syrup", CategoryId = 2, SupplierId = 1, UnitPrice = 10.00m, UnitsInStock = 13, Discontinued = false },
            new Product { ProductName = "Chef Anton's Cajun Seasoning", CategoryId = 2, SupplierId = 2, UnitPrice = 22.00m, UnitsInStock = 53, Discontinued = false },
            new Product { ProductName = "Grandma's Boysenberry Spread", CategoryId = 2, SupplierId = 3, UnitPrice = 25.00m, UnitsInStock = 120, Discontinued = false }
        };
        _context.Products.AddRange(products);

        await _context.SaveChangesAsync();
    }
}
