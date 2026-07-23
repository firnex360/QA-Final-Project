using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace InventorySystem.Server.OpenApi;

/// <summary>
/// Declares the JWT Bearer security scheme in the generated OpenAPI document.
/// Without this, Scalar/Swagger UI has no "Authorize" affordance and never sends
/// the Authorization header — every protected endpoint answers 401.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Keycloak access token. Paste ONLY the raw JWT — Scalar adds the 'Bearer ' prefix."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = scheme;

        // Apply the scheme to every operation so Scalar sends the token everywhere.
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        return Task.CompletedTask;
    }
}
