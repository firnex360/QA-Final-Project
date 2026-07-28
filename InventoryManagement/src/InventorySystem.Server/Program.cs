using System.Text.Json;
using Audit.Core;
using InventorySystem.Server.Authorization;
using InventorySystem.Server.Data;
using InventorySystem.Server.Models;
using InventorySystem.Server.OpenApi;
using InventorySystem.Server.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// #3.1-openapi-config
// Generates the OpenAPI document that documents the whole REST API.
// The transformer declares the JWT Bearer scheme (#3.3-bearer-scheme); without it the
// interactive docs have no "Authorize" box and never send the Authorization header,
// so every protected endpoint would answer 401.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// Register controllers
builder.Services.AddControllers();

// Register PostgreSQL DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services
builder.Services.AddScoped<IProductService, ProductService>();

// Register IHttpContextAccessor so Audit.NET can read the current user
builder.Services.AddHttpContextAccessor();

// #5.9-cors
// Allow-list of origins permitted to call this API from a browser: the Blazor client
// in dev (5167/7141) and in Docker (9090). Any other origin is refused by the browser.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5167",
                  "https://localhost:7141",
                  "http://localhost:9090",
                  "http://host.docker.internal:9090")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// #5.1-jwt-auth
// Authentication: validates the Keycloak-issued JWT on every request (OAuth2 + JWT).
// Key settings:
//   Authority/MetadataAddress — where the signing keys are fetched from. These differ
//     in Docker because the browser reaches Keycloak at localhost:8080 while the API
//     must use the container name.
//   MapInboundClaims = false — keeps claim names as Keycloak sends them, so
//     RoleClaimType "roles" actually matches instead of being remapped to a legacy URI.
//   ValidateAudience = false — the token is minted for the client, not the API; the API
//     authorizes by asking Keycloak for a decision instead (#5.2-policy-middleware).
// Token lifetime and session expiry are configured in Keycloak, not here.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.MetadataAddress = builder.Configuration["Keycloak:MetadataAddress"]
            ?? $"{builder.Configuration["Keycloak:Authority"]}/.well-known/openid-configuration";
        options.RequireHttpsMetadata = false;
        // Keep JWT claim names as-is (don't remap "roles" to the legacy MS schema URI),
        // so RoleClaimType = "roles" below actually matches the Keycloak roles claim.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Keycloak:Authority"],
            RoleClaimType = "roles",
            NameClaimType = "preferred_username",
            ValidateAudience = false
        };
    });

// #5.0-authz-registration
// Authorization is delegated to Keycloak Authorization Services: Resources (matched by
// request URI), Scopes, Policies and Permissions all live in the Keycloak admin console.
// PolicyEnforcementMiddleware asks Keycloak for a decision on every API request, so
// changing who can do what needs no code change and takes effect immediately.
// This is the granular model the brief demands — no endpoint checks a role name.
builder.Services.AddAuthorization();
builder.Services.Configure<KeycloakAuthorizationOptions>(
    builder.Configuration.GetSection(KeycloakAuthorizationOptions.SectionName));
builder.Services.AddHttpClient<IAuthorizationDecisionService, KeycloakDecisionService>();

// #6.1-otel-config
// OpenTelemetry — the instrumentation required by the brief. All three signals are
// exported over OTLP to Grafana Alloy (OTLP:Endpoint, http://alloy:4317 in Docker),
// which fans them out to Tempo/Loki/Prometheus (#6.3-alloy-pipeline).
//
//   Traces  → ASP.NET Core (with exceptions recorded), HttpClient (external calls),
//             and EF Core with SQL statement text (database tracing).
//   Metrics → ASP.NET Core, HttpClient, .NET runtime (CPU/GC/threads) and the Npgsql
//             meter, which supplies the required database connection-pool figures.
//   Logs    → exported with formatted message and scopes, so traceId/spanId travel
//             with each record and Loki can link back to Tempo.
//
// The service is identified as "InventoryServer" v1.0.0 in every signal.
var otelResource = ResourceBuilder.CreateDefault()
    .AddService(serviceName: "InventoryServer", serviceVersion: "1.0.0");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: "InventoryServer", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .SetResourceBuilder(otelResource)
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
        })
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["OTLP:Endpoint"] ?? "http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(otelResource)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        // Npgsql publishes connection-pool metrics (db.client.connections.*) under this
        // meter — surfaces the required "Database pool" figures in Prometheus/Grafana.
        .AddMeter("Npgsql")
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["OTLP:Endpoint"] ?? "http://localhost:4317");
        }));

// Configure OpenTelemetry Logging Exporter
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(otelResource);
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.ParseStateValues = true;
    logging.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri(builder.Configuration["OTLP:Endpoint"] ?? "http://localhost:4317");
    });
});


// #6.2-business-metric
// Custom Prometheus counter: one increment per audited entity change, labelled by
// action and entity. This is the business metric behind the Grafana "Business"
// dashboard (products created, stock updates, deletions).
// Exposed on /metrics (#6.2-metrics-endpoint) and collected by Alloy.
var auditEventsCounter = Metrics.CreateCounter(
    "inventory_audit_events_total",
    "Total number of audited entity changes.",
    new CounterConfiguration { LabelNames = ["action", "entity"] });

// #2.4-audit-config
// Audit.NET configuration — maps every tracked entity change onto an AuditLog row
// (#2.4-audit-entity) in the same ApplicationDbContext.
// For each change it records: entity name, primary key, action (Insert/Update/Delete),
// UTC timestamp, the acting user taken from the Keycloak JWT ("anonymous" if absent),
// and JSON snapshots — OldValues/NewValues/AffectedColumns, populated per action type.
// It also increments the Prometheus counter that feeds the business dashboard.
Audit.Core.Configuration.Setup()
    .UseEntityFramework(ef => ef
        .AuditTypeMapper(_ => typeof(AuditLog))
        .AuditEntityAction<AuditLog>((auditEvent, entry, auditEntity) =>
        {
            auditEntity.EntityName = entry.EntityType.Name;
            auditEntity.EntityId = entry.PrimaryKey.First().Value?.ToString() ?? "";
            auditEntity.Action = entry.Action;
            auditEntity.Timestamp = DateTime.UtcNow;

            // Feed the Prometheus counter (Grafana reads this).
            auditEventsCounter.WithLabels(entry.Action, entry.EntityType.Name).Inc();

            // Extract the authenticated user from the Keycloak JWT (if present)
            var httpContextAccessor = auditEvent.CustomFields.TryGetValue("HttpContextAccessor", out object? value)
                ? value as IHttpContextAccessor
                : null;
            auditEntity.UserId = httpContextAccessor?.HttpContext?.User?.Identity?.Name
                ?? httpContextAccessor?.HttpContext?.User?.FindFirst("preferred_username")?.Value
                ?? "anonymous";

            // Capture old values, new values, and affected columns
            auditEntity.OldValues = entry.Action == "Update"
                ? JsonSerializer.Serialize(entry.Changes?.ToDictionary(c => c.ColumnName, c => c.OriginalValue))
                : entry.Action == "Delete"
                    ? JsonSerializer.Serialize(entry.ColumnValues)
                    : null;

            auditEntity.NewValues = entry.Action == "Update"
                ? JsonSerializer.Serialize(entry.Changes?.ToDictionary(c => c.ColumnName, c => c.NewValue))
                : entry.Action == "Insert"
                    ? JsonSerializer.Serialize(entry.ColumnValues)
                    : null;

            auditEntity.AffectedColumns = entry.Action == "Update"
                ? JsonSerializer.Serialize(entry.Changes?.Select(c => c.ColumnName))
                : null;
        })
        .IgnoreMatchedProperties(true));

// Inject the IHttpContextAccessor into every audit event so the action above can read it
Audit.EntityFramework.Configuration.Setup()
    .ForContext<ApplicationDbContext>(config => config
        .IncludeEntityObjects()
        .AuditEventType("{context}:{database}"));

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
    await next();
});

// Configure the HTTP request pipeline.
// #3.2-openapi-ui
// Interactive API documentation, development only:
//   /openapi/v1.json — the raw OpenAPI document
//   /scalar          — the browsable UI (Scalar, equivalent to Swagger UI) where every
//                      endpoint can be executed after pasting a Keycloak token.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// Enable CORS
app.UseCors("AllowBlazorClient");

// #6.2-metrics-endpoint (middleware half)
// Records HTTP metrics per request — rate, duration histogram and status code —
// under http_request_duration_seconds_*. These power the throughput, latency
// percentile and error-rate panels, and the alert rules (#6.5-alert-rules).
app.UseHttpMetrics();

// Middleware to inject IHttpContextAccessor into Audit.NET's custom fields
app.Use(async (context, next) =>
{
    var accessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();
    Audit.Core.Configuration.AddOnCreatedAction(scope =>
    {
        scope.SetCustomField("HttpContextAccessor", accessor);
    });
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// Per-request policy evaluation against Keycloak (must run after authentication so the
// bearer token is available, and after routing so [AllowAnonymous] metadata is visible).
app.UseMiddleware<PolicyEnforcementMiddleware>();

// Map controller endpoints
app.MapControllers();

// #6.2-metrics-endpoint
// Exposes /metrics in Prometheus text format. NOTE: Prometheus does not scrape this
// directly — Alloy does, and forwards it by remote-write (#6.3-alloy-pipeline), so all
// telemetry reaches Prometheus through the collector.
app.MapMetrics();

app.Run();
