using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace e2e_testing;

public class ProductsCrudTests : PageTest
{
    private const string BaseUrl = "http://localhost:9090";
    private const string TestUsername = "e2e-testing";
    private const string TestPassword = "12345";

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
        var targetDir = Path.Combine(projectDir, "test-results");
        Directory.CreateDirectory(targetDir);
        return Path.Combine(targetDir, fileName);
    }

    /// <summary>
    /// Helper method to log in via Keycloak before test execution.
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
    /// Complete E2E Lifecycle Test:
    ///   1. Create Product (Form entry & submission)
    ///   2. Read Product (Search & verify in card list)
    ///   3. Update Product (Edit price/quantity & verify changes)
    ///   4. Delete Product (Confirm dialog & verify removal)
    /// </summary>
    [Fact]
    public async Task FullProductLifecycle_CreateReadUpdateDelete()
    {
        // Generate a unique SKU code per test run to prevent strict mode violations from leftover DB data
        var skuCode = $"PW-CRUD-{Random.Shared.Next(1000, 9999)}";
        var productName = $"Playwright CRUD {skuCode}";

        await LoginAsync();

        // Step 1: CREATE
        await Page.GotoAsync($"{BaseUrl}/create");

        var nameInput = Page.Locator("input[placeholder='Enter product name...']");
        await Expect(nameInput).ToBeVisibleAsync(new() { Timeout = 15000 });

        await nameInput.FillAsync(productName);
        await Page.Locator("input[placeholder='Enter SKU...']").FillAsync(skuCode);
        await Page.Locator("textarea[placeholder='Enter description...']").FillAsync("Item created by Playwright E2E CRUD test");
        await Page.Locator("select.form-control").SelectOptionAsync(new[] { "Electronics" });
        await Page.Locator("input[placeholder='Enter price...']").FillAsync("99.99");
        await Page.Locator("input[placeholder='Enter quantity...']").FillAsync("50");
        await Page.Locator("input[placeholder='Enter minimum stock level...']").FillAsync("5");

        await Page.Locator("button:has-text('Create Product!')").ClickAsync();

        var successAlert = Page.Locator("div.alert-success");
        await Expect(successAlert).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Expect(successAlert).ToContainTextAsync("Product created successfully");

        await Page.ScreenshotAsync(new() { Path = GetScreenshotPath("05-product-created.png") });

        // Step 2: READ / SEARCH 
        await Page.GotoAsync($"{BaseUrl}/read");
        var searchInput = Page.Locator("input[aria-label='Search products']");
        await Expect(searchInput).ToBeVisibleAsync(new() { Timeout = 15000 });
        await searchInput.FillAsync(skuCode);

        var productCard = Page.Locator(".product-card", new() { HasTextString = skuCode }).First;
        await Expect(productCard).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Expect(productCard).ToContainTextAsync(productName);

        await Page.ScreenshotAsync(new() { Path = GetScreenshotPath("06-product-found-in-list.png") });

        //  Step 3: UPDATE / EDIT 
        var editButton = productCard.Locator("button:has-text('Edit')");
        await editButton.ClickAsync();

        await Page.WaitForURLAsync($"{BaseUrl}/edit/**", new() { Timeout = 15000 });

        var priceInput = Page.Locator("input[type='number']").First;
        await Expect(priceInput).ToBeVisibleAsync(new() { Timeout = 15000 });

        await priceInput.FillAsync("149.99");
        var quantityInput = Page.Locator("input[type='number']").Nth(1);
        await quantityInput.FillAsync("75");

        await Page.ScreenshotAsync(new() { Path = GetScreenshotPath("07-product-editing.png") });

        await Page.Locator("button:has-text('Save Changes')").ClickAsync();

        await Page.WaitForURLAsync($"{BaseUrl}/read", new() { Timeout = 15000 });

        var searchInputAfterEdit = Page.Locator("input[aria-label='Search products']");
        await Expect(searchInputAfterEdit).ToBeVisibleAsync(new() { Timeout = 15000 });
        await searchInputAfterEdit.FillAsync(skuCode);

        var updatedCard = Page.Locator(".product-card", new() { HasTextString = skuCode }).First;
        await Expect(updatedCard).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Expect(updatedCard).ToContainTextAsync("149.99");
        await Expect(updatedCard).ToContainTextAsync("75");

        await Page.ScreenshotAsync(new() { Path = GetScreenshotPath("08-product-edited.png") });

        // Step 4: DELETE 
        Page.Dialog += async (_, dialog) =>
        {
            await dialog.AcceptAsync();
        };

        var deleteButton = updatedCard.Locator("button:has-text('Delete')");
        await deleteButton.ClickAsync();

        await Expect(updatedCard).Not.ToBeVisibleAsync(new() { Timeout = 15000 });

        await Page.ScreenshotAsync(new() { Path = GetScreenshotPath("09-product-deleted.png") });
    }
}
