using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace e2e_testing;

public class LoginTests : PageTest
{
    private const string BaseUrl = "http://localhost:9090";
    private const string TestUsername = "e2e-testing";
    private const string TestPassword = "12345";

    /// <summary>
    /// Test 1: An unauthenticated user visiting the app sees the "Access Denied" alert.
    /// </summary>
    [Fact]
    public async Task UnauthenticatedUser_SeesAccessDenied()
    {
        await Page.GotoAsync(BaseUrl);

        var accessDeniedAlert = Page.Locator("div.alert-warning");
        await Expect(accessDeniedAlert).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Expect(accessDeniedAlert).ToContainTextAsync("Access Denied");
        await Expect(accessDeniedAlert).ToContainTextAsync("Click here to login");

        await Page.ScreenshotAsync(new() { Path = "test-results/01-access-denied.png" });
    }

    /// <summary>
    /// Test 2: Clicking "login" redirects to the Keycloak login form.
    /// </summary>
    [Fact]
    public async Task LoginLink_RedirectsToKeycloak()
    {
        await Page.GotoAsync(BaseUrl);

        var loginLink = Page.Locator("a[href='authentication/login']");
        await Expect(loginLink).ToBeVisibleAsync(new() { Timeout = 15000 });
        await loginLink.ClickAsync();

        await Page.WaitForURLAsync("**/realms/inventory-realm/**", new() { Timeout = 15000 });

        var usernameField = Page.Locator("#username");
        var passwordField = Page.Locator("#password");
        await Expect(usernameField).ToBeVisibleAsync();
        await Expect(passwordField).ToBeVisibleAsync();

        await Page.ScreenshotAsync(new() { Path = "test-results/02-keycloak-login-form.png" });
    }

    /// <summary>
    /// Test 3: A valid login flow ends on the Dashboard with the user greeting visible.
    /// </summary>
    [Fact]
    public async Task ValidLogin_RedirectsToDashboard()
    {
        await Page.GotoAsync(BaseUrl);

        var loginLink = Page.Locator("a[href='authentication/login']");
        await Expect(loginLink).ToBeVisibleAsync(new() { Timeout = 15000 });
        await loginLink.ClickAsync();

        await Page.WaitForURLAsync("**/realms/inventory-realm/**", new() { Timeout = 15000 });

        await Page.Locator("#username").FillAsync(TestUsername);
        await Page.Locator("#password").FillAsync(TestPassword);

        await Page.ScreenshotAsync(new() { Path = "test-results/03-credentials-filled.png" });

        await Page.Locator("#kc-login").ClickAsync();

        await Page.WaitForURLAsync($"{BaseUrl}/**", new() { Timeout = 20000 });

        var dashboardHeading = Page.Locator("h1", new() { HasTextString = "Dashboard" });
        await Expect(dashboardHeading).ToBeVisibleAsync(new() { Timeout = 15000 });

        var greeting = Page.GetByText($"Hello, {TestUsername}!");
        await Expect(greeting).ToBeVisibleAsync(new() { Timeout = 10000 });

        await Page.ScreenshotAsync(new() { Path = "test-results/04-authenticated-dashboard.png" });
    }
}
