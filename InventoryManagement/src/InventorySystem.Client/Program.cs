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
    // Let the provider discover metadata from the Authority and use the app base address
    options.ProviderOptions.RedirectUri = $"{builder.HostEnvironment.BaseAddress}authentication/login-callback";
    options.ProviderOptions.PostLogoutRedirectUri = $"{builder.HostEnvironment.BaseAddress}authentication/logout-callback";
    options.ProviderOptions.DefaultScopes.Add("openid");
    options.ProviderOptions.DefaultScopes.Add("profile");
    options.ProviderOptions.DefaultScopes.Add("email");
    options.AuthenticationPaths.LogOutSucceededPath = "/";
    options.AuthenticationPaths.LogOutFailedPath = "/";
});

builder.Services.AddSingleton<ProductService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();