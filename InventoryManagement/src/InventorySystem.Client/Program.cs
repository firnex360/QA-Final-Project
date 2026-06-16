using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using InventorySystem.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using ApexCharts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API base URL: reads from appsettings.json ("ApiBaseUrl"), falls back to same-origin for Docker
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Keycloak", options.ProviderOptions);
    options.ProviderOptions.ResponseType = "code";
    options.UserOptions.RoleClaim = "roles";
    options.UserOptions.NameClaim = "preferred_username";
});

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("CanCreate", policy =>
        policy.RequireRole("adminY", "managerY"));

    options.AddPolicy("CanRead", policy =>
        policy.RequireRole("adminY", "managerY", "staffY"));

    options.AddPolicy("CanUpdate", policy =>
        policy.RequireRole("adminY", "managerY"));

    options.AddPolicy("CanDelete", policy =>
        policy.RequireRole("adminY"));
});

builder.Services.AddApexCharts();

builder.Services.AddScoped<ApiAuthorizationMessageHandler>();

// Point HttpClient at the Server API base address (from launch settings on .Server project)
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<ApiAuthorizationMessageHandler>();
    handler.ConfigureHandler(authorizedUrls: [apiBaseUrl]);
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
});

await builder.Build().RunAsync();

namespace InventorySystem.Client
{
    internal sealed class ApiAuthorizationMessageHandler : AuthorizationMessageHandler
    {
        public ApiAuthorizationMessageHandler(IAccessTokenProvider provider, NavigationManager navigation)
            : base(provider, navigation)
        {
        }
    }
}
