using InventorySystem.Server.Data;
using InventorySystem.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DataTesting;

/// <summary>
/// Data Testing — one test per area required by the project brief:
/// Migraciones, Constraints, Datos duplicados, Seeds, Integridad de datos.
/// </summary>
public class DataTests(DataTests.DatabaseFixture fixture) : IClassFixture<DataTests.DatabaseFixture>
{
    /// <summary>
    /// Starts one PostgreSQL container for the whole file and migrates it once. If a migration were
    /// broken, this would throw and every test below would fail — which is the behaviour we want.
    /// </summary>
    public class DatabaseFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:18.4")
            .Build();

        public async ValueTask InitializeAsync()
        {
            await _postgres.StartAsync();

            await using var db = CreateContext();
            await db.Database.MigrateAsync();
        }

        public async ValueTask DisposeAsync() => await _postgres.DisposeAsync();

        public ApplicationDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options);
    }

    // 1. MIGRACIONES 
    // Every migration in the project applied to an empty database, and nothing was left over.
    [Fact]
    public async Task Migrations_ApplyCleanlyToAnEmptyDatabase()
    {
        await using var db = fixture.CreateContext();

        var defined = db.Database.GetMigrations();
        var applied = await db.Database.GetAppliedMigrationsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(defined, applied);
        Assert.Empty(pending);
    }

    // 2. SEEDS (or has default values test)
    // The HasData catalogue was written by the migration, so a fresh database is not empty.
    [Fact]
    public async Task Seed_PopulatesTheProductCatalogue()
    {
        await using var db = fixture.CreateContext();

        var laptop = await db.Products.SingleAsync(p => p.CodeSKU == "LAP-001", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, await db.Products.CountAsync(p => p.Id <= 3, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("Laptop Pro 15", laptop.Name);
        Assert.Equal(1299.99m, laptop.Price);
    }

    // 3. CONSTRAINTS 
    // CodeSKU is NOT NULL in the database, so a product without one cannot be stored at all.
    [Fact]
    public async Task Constraint_ProductWithoutSkuIsRejected()
    {
        await using var db = fixture.CreateContext();

        db.Products.Add(NewProduct(sku: null));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    // 4. DATOS DUPLICADOS 
    // The unique index refuses a second product reusing a SKU that already exists.
    [Fact]
    public async Task Duplicate_ReusingAnExistingSkuIsRejected()
    {
        await using var db = fixture.CreateContext();

        // "LAP-001" is already in the database: it came from the seed data.
        db.Products.Add(NewProduct(sku: "LAP-001"));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    // 5. INTEGRIDAD DE DATOS 
    // Seeding wrote Ids 1-3 by hand, so the migration had to push PostgreSQL's id counter past
    // them. Without that, this insert would be given Id 1 and collide with the seeded laptop.
    [Fact]
    public async Task Integrity_NewProductDoesNotCollideWithSeededIds()
    {
        await using var db = fixture.CreateContext();

        var product = NewProduct(sku: "NEW-001");
        db.Products.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.True(product.Id > 3, $"Expected an Id above the seeded range (1-3) but got {product.Id}.");
    }

    private static Product NewProduct(string? sku) => new()
    {
        Name = "Test Product",
        CodeSKU = sku,
        Description = "Created by the data tests.",
        Category = "Electronics",
        Price = 10.00m,
        Quantity = 1,
        MinimumStockLevel = 1,
        IsActive = true
    };
}
