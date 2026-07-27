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
}
