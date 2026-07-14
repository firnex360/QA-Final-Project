using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using InventorySystem.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
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
}).AddAccountClaimsPrincipalFactory<KeycloakRolesClaimsPrincipalFactory>();

// Granular permission model: mirrors the server. The UI only checks permissions
// (product:manage, stock:manage, ...) — never role names. Keycloak expands each
// user's composite roles (adminY/managerY/staffY) into these permissions in the token.
string[] permissions =
[
    "product:view",
    "product:manage",
    "stock:view",
    "stock:manage",
    "report:view",
    "user:manage",
    "audit:view"
];

builder.Services.AddAuthorizationCore(options =>
{
    foreach (var permission in permissions)
        options.AddPolicy(permission, policy => policy.RequireRole(permission));
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

    /// <summary>
    /// Keycloak sends the "roles" claim as a JSON array. Blazor WASM's default
    /// claims factory keeps it as ONE claim whose value is the raw JSON string
    /// ("[\"adminY\",\"product:view\",...]"), which breaks IsInRole/AuthorizeView.
    /// This factory unpacks the array into one claim per role.
    /// </summary>
    internal sealed class KeycloakRolesClaimsPrincipalFactory
        : AccountClaimsPrincipalFactory<RemoteUserAccount>
    {
        public KeycloakRolesClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor)
            : base(accessor)
        {
        }

        public override async ValueTask<System.Security.Claims.ClaimsPrincipal> CreateUserAsync(
            RemoteUserAccount account,
            RemoteAuthenticationUserOptions options)
        {
            var user = await base.CreateUserAsync(account, options);

            if (user.Identity is System.Security.Claims.ClaimsIdentity identity && identity.IsAuthenticated)
            {
                // Find role claims whose value is still a JSON array and split them up.
                var packedClaims = identity.FindAll(options.RoleClaim)
                    .Where(c => c.Value.StartsWith('['))
                    .ToList();

                foreach (var packed in packedClaims)
                {
                    identity.RemoveClaim(packed);

                    var roles = System.Text.Json.JsonSerializer.Deserialize<string[]>(packed.Value) ?? [];
                    foreach (var role in roles)
                        identity.AddClaim(new System.Security.Claims.Claim(options.RoleClaim, role));
                }
            }

            return user;
        }
    }
}
