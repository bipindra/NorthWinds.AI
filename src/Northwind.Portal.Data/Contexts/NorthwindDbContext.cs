using Microsoft.EntityFrameworkCore;
using Northwind.Portal.Data.Helpers;
using Northwind.Portal.Domain.Entities;

namespace Northwind.Portal.Data.Contexts;

public class NorthwindDbContext : DbContext
{
    public NorthwindDbContext(DbContextOptions<NorthwindDbContext> options) : base(options)
    {
    }

    // Northwind core entities
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Shipper> Shippers { get; set; }
    public DbSet<Employee> Employees { get; set; }

    // Portal extension entities
    public DbSet<PortalUserCustomerMap> PortalUserCustomerMaps { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
    public DbSet<OrderFulfillment> OrderFulfillments { get; set; }
    public DbSet<OrderMeta> OrderMetas { get; set; }
    public DbSet<CartHeader> CartHeaders { get; set; }
    public DbSet<CartLine> CartLines { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // This is only used for design-time operations
            return;
        }
        
        // Suppress pending model changes warning during development
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer - matches standard Northwind schema
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(e => e.CustomerId);
            entity.Property(e => e.CustomerId)
                .IsRequired()
                .HasMaxLength(5)
                .IsFixedLength()
                .HasColumnType("nchar(5)");
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(40);
            entity.Property(e => e.ContactName).HasMaxLength(30);
            entity.Property(e => e.ContactTitle).HasMaxLength(30);
            entity.Property(e => e.Address).HasMaxLength(60);
            entity.Property(e => e.City).HasMaxLength(15);
            entity.Property(e => e.Region).HasMaxLength(15);
            entity.Property(e => e.PostalCode).HasMaxLength(10);
            entity.Property(e => e.Country).HasMaxLength(15);
            entity.Property(e => e.Phone).HasMaxLength(24);
            entity.Property(e => e.Fax).HasMaxLength(24);
        });

        // Category - matches standard Northwind schema
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(15);
            entity.Property(e => e.Description).HasColumnType("ntext");
            entity.Property(e => e.Picture).HasColumnType("image");
        });

        // Supplier - matches standard Northwind schema
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Suppliers");
            entity.HasKey(e => e.SupplierId);
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(40);
            entity.Property(e => e.ContactName).HasMaxLength(30);
            entity.Property(e => e.ContactTitle).HasMaxLength(30);
            entity.Property(e => e.Address).HasMaxLength(60);
            entity.Property(e => e.City).HasMaxLength(15);
            entity.Property(e => e.Region).HasMaxLength(15);
            entity.Property(e => e.PostalCode).HasMaxLength(10);
            entity.Property(e => e.Country).HasMaxLength(15);
            entity.Property(e => e.Phone).HasMaxLength(24);
            entity.Property(e => e.Fax).HasMaxLength(24);
            entity.Property(e => e.HomePage).HasColumnType("ntext");
        });

        // Shipper - matches standard Northwind schema
        modelBuilder.Entity<Shipper>(entity =>
        {
            entity.ToTable("Shippers");
            entity.HasKey(e => e.ShipperId);
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Phone).HasMaxLength(24);
        });

        // Employee - matches standard Northwind schema
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasKey(e => e.EmployeeId);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(20);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Title).HasMaxLength(30);
            entity.Property(e => e.TitleOfCourtesy).HasMaxLength(25);
            entity.Property(e => e.BirthDate).HasColumnType("datetime");
            entity.Property(e => e.HireDate).HasColumnType("datetime");
            entity.Property(e => e.Address).HasMaxLength(60);
            entity.Property(e => e.City).HasMaxLength(15);
            entity.Property(e => e.Region).HasMaxLength(15);
            entity.Property(e => e.PostalCode).HasMaxLength(10);
            entity.Property(e => e.Country).HasMaxLength(15);
            entity.Property(e => e.HomePhone).HasMaxLength(24);
            entity.Property(e => e.Extension).HasMaxLength(4);
            entity.Property(e => e.Photo).HasColumnType("image");
            entity.Property(e => e.Notes).HasColumnType("ntext");
            entity.Property(e => e.PhotoPath).HasMaxLength(255);

            entity.HasOne(e => e.ReportsToEmployee)
                .WithMany(e => e.DirectReports)
                .HasForeignKey(e => e.ReportsTo)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Product - matches standard Northwind schema
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(e => e.ProductId);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.QuantityPerUnit).HasMaxLength(20);
            entity.Property(e => e.UnitPrice).HasColumnType("money");
            entity.Property(e => e.UnitsInStock).HasColumnType("smallint");
            entity.Property(e => e.UnitsOnOrder).HasColumnType("smallint");
            entity.Property(e => e.ReorderLevel).HasColumnType("smallint");
            entity.Property(e => e.Discontinued).HasDefaultValue(false).HasColumnType("bit");

            entity.HasOne(e => e.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Order - matches standard Northwind schema
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.CustomerId)
                .HasMaxLength(5)
                .IsFixedLength()
                .HasColumnType("nchar(5)");
            entity.Property(e => e.EmployeeId).HasColumnType("int");
            entity.Property(e => e.OrderDate).HasColumnType("datetime");
            entity.Property(e => e.RequiredDate).HasColumnType("datetime");
            entity.Property(e => e.ShippedDate).HasColumnType("datetime");
            entity.Property(e => e.ShipVia).HasColumnType("int");
            entity.Property(e => e.Freight).HasColumnType("money");
            entity.Property(e => e.ShipName).HasMaxLength(40);
            entity.Property(e => e.ShipAddress).HasMaxLength(60);
            entity.Property(e => e.ShipCity).HasMaxLength(15);
            entity.Property(e => e.ShipRegion).HasMaxLength(15);
            entity.Property(e => e.ShipPostalCode).HasMaxLength(10);
            entity.Property(e => e.ShipCountry).HasMaxLength(15);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Employee)
                .WithMany(e => e.Orders)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Shipper)
                .WithMany(s => s.Orders)
                .HasForeignKey(e => e.ShipVia)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // OrderDetail - composite key, matches standard Northwind schema
        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("Order Details");
            entity.HasKey(e => new { e.OrderId, e.ProductId });
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.UnitPrice).HasColumnType("money");
            entity.Property(e => e.Quantity).HasColumnType("smallint");
            entity.Property(e => e.Discount).HasDefaultValue(0f).HasColumnType("real");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PortalUserCustomerMap
        modelBuilder.Entity<PortalUserCustomerMap>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.CustomerId).IsRequired().HasMaxLength(5);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.CustomerId });

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OrderStatusHistory
        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChangedByUserId).HasMaxLength(450);
            entity.Property(e => e.Comment).HasMaxLength(500);

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderStatusHistories)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderFulfillment
        modelBuilder.Entity<OrderFulfillment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PickedByUserId).HasMaxLength(450);

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderFulfillments)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.OrderFulfillments)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OrderMeta
        modelBuilder.Entity<OrderMeta>(entity =>
        {
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.PoNumber).HasMaxLength(50);
            entity.Property(e => e.TrackingNumber).HasMaxLength(100);
            entity.Property(e => e.InternalNotes).HasMaxLength(1000);

            entity.HasOne(e => e.Order)
                .WithOne(o => o.OrderMeta)
                .HasForeignKey<OrderMeta>(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CartHeader
        modelBuilder.Entity<CartHeader>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(e => e.UserId);
        });

        // CartLine
        modelBuilder.Entity<CartLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasColumnType("money");

            entity.HasOne(e => e.CartHeader)
                .WithMany(ch => ch.CartLines)
                .HasForeignKey(e => e.CartHeaderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.CartLines)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Apply MySQL type conversions AFTER all entity configurations
        // This ensures SQL Server types are converted to MySQL equivalents
        MySqlTypeConverter.ApplyMySqlTypeConversions(modelBuilder);
    }
}
