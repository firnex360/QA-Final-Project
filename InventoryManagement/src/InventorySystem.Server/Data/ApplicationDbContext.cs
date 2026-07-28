using Audit.EntityFramework;
using InventorySystem.Server.Models;
using InventorySystem.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Server.Data;

// #2.4-audit-dbcontext
// Inheriting from Audit.NET's AuditDbContext (instead of plain DbContext) is what makes
// auditing automatic: every SaveChanges/SaveChangesAsync on any tracked entity produces
// an AuditLog row. No service or controller has to remember to record anything.
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : AuditDbContext(options)
{
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Product>().Property(p => p.CodeSKU).IsRequired();
        modelBuilder.Entity<Product>().HasIndex(p => p.CodeSKU).IsUnique();

        // Default products for databsae
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop Pro 15", CodeSKU = "LAP-001", Description = "Business laptop, 16 GB RAM.", Category = "Electronics", Price = 1299.99m, Quantity = 12, MinimumStockLevel = 5, IsActive = true },
            new Product { Id = 2, Name = "Wireless Mouse", CodeSKU = "ACC-002", Description = "Bluetooth optical mouse.", Category = "Accessories", Price = 24.50m, Quantity = 3, MinimumStockLevel = 10, IsActive = true },
            new Product { Id = 3, Name = "Standing Desk", CodeSKU = "FUR-003", Description = "Height-adjustable desk.", Category = "Furniture", Price = 549.00m, Quantity = 20, MinimumStockLevel = 4, IsActive = true });
    }
}
