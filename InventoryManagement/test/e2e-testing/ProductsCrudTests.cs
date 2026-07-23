using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace e2e_testing;

public class ProductsCrudTests : PageTest
{
    private const string BaseUrl = "http://localhost:9090";
    private const string TestUsername = "e2e-testing";
    private const string TestPassword = "12345";

    private const string SkuCode = "PW-CRUD-99";
    private const string ProductName = "Playwright CRUD Item";

    public ProductsCrudTests()
    {
        // To change enviroment variables (1 to show browser)
        Environment.SetEnvironmentVariable("HEADED", "0");
        //Environment.SetEnvironmentVariable("SLOWMO", "100");
    }

    /// <summary>
    /// Helper to save screenshots directly in 'test/e2e-testing/test-results/'
    /// (at the same level as bin/ and obj/) for easy accessibility.
    /// </summary>
    private static string GetScreenshotPath(string fileName)
    {
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../"));
        var targetDir = Path.Combine(projectDir, "CRUD-test-results");
        Directory.CreateDirectory(targetDir);
        return Path.Combine(targetDir, fileName);
    }

    /// <summary>
    /// Helper method to log in via Keycloak before each test.
    /// </summary>
    private async Task LoginAsync()
    {
        await Page.GotoAsync(BaseUrl);

        var loginLink = Page.Locator("a[href='authentication/login']");
        await Expect(loginLink).ToBeVisibleAsync(new() { Timeout = 15000 });
        await loginLink.ClickAsync();

        await Page.WaitForURLAsync("**/realms/inventory-realm/**", new() { Timeout = 15000 });

        await Page.Locator("#username").FillAsync(TestUsername);
        await Page.Locator("#password").FillAsync(TestPassword);
        await Page.Locator("#kc-login").ClickAsync();

        await Page.WaitForURLAsync($"{BaseUrl}/**", new() { Timeout = 20000 });
        var greeting = Page.GetByText($"Hello, {TestUsername}!");
        await Expect(greeting).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    /// <summary>
    /// Test 1: Create a new product and verify it appears in the list.
    /// </summary>
    [Fact]
    public async Task CreateProduct_AppearsInList()
    {
        await LoginAsync();

        // Navigate to Create Product page
        await Page.GotoAsync($"{BaseUrl}/create");

        // Wait for form
        var nameInput = Page.Locator("input[placeholder='Enter product name...']");
        await Expect(nameInput).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Fill out product form
        await nameInput.FillAsync(ProductName);
        await Page.Locator("input[placeholder='Enter SKU...']").FillAsync(SkuCode);
        await Page.Locator("textarea[placeholder='Enter description...']").FillAsync("Item created by Playwright E2E CRUD test");

        // Select category
        await Page.Locator("select.form-control").SelectOptionAsync(new[] { "Electronics" });

        await Page.Locator("input[placeholder='Enter price...']").FillAsync("99.99");
        await Page.Locator("input[placeholder='Enter quantity...']").FillAsync("50");
        await Page.Locator("input[placeholder='Enter minimum stock level...']").FillAsync("5");

        // Submit form
        await Page.Locator("button:has-text('Create Product!')").ClickAsync();

        // Assert success message is displayed
        var successAlert = Page.Locator("div.alert-success");
        await Expect(successAlert).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Expect(successAlert).ToContainTextAsync("Product created successfully");

        await Page.ScreenshotAsync(new() { Path = GetScreenshotPath("05-product-created.png") });

        // Navigate to Read page to verify product exists in product cards
        await Page.GotoAsync($"{BaseUrl}/read");
        var searchInput = Page.Locator("input[aria-label='Search products']");
        await Expect(searchInput).ToBeVisibleAsync(new() { Timeout = 15000 });
        await searchInput.FillAsync(SkuCode);

        // Verify product name and SKU appear in product card
        var productCard = Page.Locator(".product-card", new() { HasTextString = SkuCode });
        await Expect(productCard).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Expect(productCard).ToContainTextAsync(ProductName);
    }

    /// <summary>
    /// Test 2: Edit the created product's price and quantity and verify updates.
    /// </summary>
    [Fact]
    public async Task EditProduct_UpdatesValues()
    {
        await LoginAsync();

        // Go to Read page and search for created product
        await Page.GotoAsync($"{BaseUrl}/read");
        var searchInput = Page.Locator("input[aria-label='Search products']");
        await Expect(searchInput).ToBeVisibleAsync(new() { Timeout = 15000 });
        await searchInput.FillAsync(SkuCode);

        var productCard = Page.Locator(".product-card", new() { HasTextString = SkuCode });
        await Expect(productCard).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Click Edit button for this product
        var editButton = productCard.Locator("button:has-text('Edit')");
        await editButton.ClickAsync();

        // Assert redirect to Edit page
        await Page.WaitForURLAsync($"{BaseUrl}/edit/**", new() { Timeout = 15000 });

        // Wait for price input
        var priceInput = Page.Locator("input[type='number']").First;
        await Expect(priceInput).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Update Price to 149.99 and Quantity to 75
        await priceInput.FillAsync("149.99");
        var quantityInput = Page.Locator("input[type='number']").Nth(1);
        await quantityInput.FillAsync("75");

        await Page.ScreenshotAsync(new() { Path = GetScreenshotPath("06-product-editing.png") });

        // Click Save Changes button
        await Page.Locator("button:has-text('Save Changes')").ClickAsync();

        // Assert redirect back to Read page
        await Page.WaitForURLAsync($"{BaseUrl}/read", new() { Timeout = 15000 });

        // Search again to verify updated values
        var searchInputAfterEdit = Page.Locator("input[aria-label='Search products']");
        await Expect(searchInputAfterEdit).ToBeVisibleAsync(new() { Timeout = 15000 });
        await searchInputAfterEdit.FillAsync(SkuCode);

        var updatedCard = Page.Locator(".product-card", new() { HasTextString = SkuCode });
        await Expect(updatedCard).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Expect(updatedCard).ToContainTextAsync("149.99");
        await Expect(updatedCard).ToContainTextAsync("75");

        await Page.ScreenshotAsync(new() { Path = GetScreenshotPath("07-product-edited.png") });
    }

    /// <summary>
    /// Test 3: Delete the product, accept confirmation dialog, and verify removal.
    /// </summary>
    [Fact]
    public async Task DeleteProduct_RemovesFromList()
    {
        await LoginAsync();

        // Go to Read page and search for product
        await Page.GotoAsync($"{BaseUrl}/read");
        var searchInput = Page.Locator("input[aria-label='Search products']");
        await Expect(searchInput).ToBeVisibleAsync(new() { Timeout = 15000 });
        await searchInput.FillAsync(SkuCode);

        var productCard = Page.Locator(".product-card", new() { HasTextString = SkuCode });
        await Expect(productCard).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Set up automatic dialog handler for JS confirm window
        Page.Dialog += async (_, dialog) =>
        {
            await dialog.AcceptAsync();
        };

        // Click Delete button
        var deleteButton = productCard.Locator("button:has-text('Delete')");
        await deleteButton.ClickAsync();

        // Assert card is removed from view
        await Expect(productCard).Not.ToBeVisibleAsync(new() { Timeout = 15000 });

        await Page.ScreenshotAsync(new() { Path = GetScreenshotPath("08-product-deleted.png") });
    }
}
