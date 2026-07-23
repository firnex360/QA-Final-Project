using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integration.Fixtures;

public class FakeAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "FakeScheme";

    public FakeAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "integration-test-user"),
            new("preferred_username", "integration-test-user"),
            // Composite role name (informational, not used for authorization)
            new("roles", "adminY"),
            // Granular permissions — what Keycloak expands the composite into
            new("roles", "product:view"),
            new("roles", "product:manage"),
            new("roles", "stock:view"),
            new("roles", "stock:manage"),
            new("roles", "report:view"),
            new("roles", "user:manage"),
            new("roles", "audit:view"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName, "preferred_username", "roles");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
