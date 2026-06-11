using InventorySystem.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{

    public DbSet<Product> Products { get; set; } = null!;
}
