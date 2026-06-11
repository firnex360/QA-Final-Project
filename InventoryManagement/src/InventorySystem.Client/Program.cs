using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using InventorySystem.Client;
using InventorySystem.Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = "http://localhost:8080/realms/InventoryWebApiRealm";
    options.ProviderOptions.ClientId = "inventory-client";
    options.ProviderOptions.ResponseType = "code";

    options.ProviderOptions.DefaultScopes.Clear();
    options.ProviderOptions.DefaultScopes.Add("openid");
    options.ProviderOptions.DefaultScopes.Add("profile");
    options.ProviderOptions.DefaultScopes.Add("email");

    options.ProviderOptions.RedirectUri = $"{builder.HostEnvironment.BaseAddress}authentication/login-callback";
    options.ProviderOptions.PostLogoutRedirectUri = $"{builder.HostEnvironment.BaseAddress}authentication/logout-callback";

    options.AuthenticationPaths.LogInPath = "authentication/login";
    options.AuthenticationPaths.LogInCallbackPath = "authentication/login-callback";
    options.AuthenticationPaths.LogInFailedPath = "authentication/login-failed";
    options.AuthenticationPaths.LogOutPath = "authentication/logout";
    options.AuthenticationPaths.LogOutCallbackPath = "authentication/logout-callback";
    options.AuthenticationPaths.LogOutFailedPath = "authentication/logout-failed";
    options.AuthenticationPaths.LogOutSucceededPath = "/";
});

builder.Services.AddSingleton<ProductService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
