using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql;
using Northwind.Portal.Data.Contexts;

namespace Northwind.Portal.Data.DesignTime;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use MySQL provider for design-time to generate MySQL-compatible migrations
        var connectionString = "Server=localhost;Database=NorthwindsDb;User=root;Password=temp;";
        var serverVersion = new MariaDbServerVersion(new Version(10, 6, 22));
        
        optionsBuilder.UseMySql(connectionString, serverVersion, options =>
        {
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
