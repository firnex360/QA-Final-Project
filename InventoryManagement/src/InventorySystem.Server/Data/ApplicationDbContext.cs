using Audit.EntityFramework;
using InventorySystem.Server.Models;
using InventorySystem.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : AuditDbContext(options)
{
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
}
