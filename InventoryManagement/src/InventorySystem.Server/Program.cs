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

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Declare the JWT Bearer scheme so Scalar shows an "Authorize" box and actually
// sends the Authorization header (otherwise every protected endpoint returns 401).
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

// Configure CORS to allow the Blazor client to call this API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins("http://localhost:5167", "https://localhost:7141", "http://localhost:9090")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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

// Authorization is delegated to Keycloak Authorization Services: Resources (matched by
// request URI), Scopes, Policies and Permissions all live in the Keycloak admin console.
// PolicyEnforcementMiddleware asks Keycloak for a decision on every API request, so
// changing who can do what needs no code change and takes effect immediately.
builder.Services.AddAuthorization();
builder.Services.Configure<KeycloakAuthorizationOptions>(
    builder.Configuration.GetSection(KeycloakAuthorizationOptions.SectionName));
builder.Services.AddHttpClient<IAuthorizationDecisionService, KeycloakDecisionService>();

// OpenTelemetry Configuration Stuff
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


// Custom Prometheus counter: one increment per audited entity change,
// labelled by action and entity so Grafana can chart business activity.
var auditEventsCounter = Metrics.CreateCounter(
    "inventory_audit_events_total",
    "Total number of audited entity changes.",
    new CounterConfiguration { LabelNames = ["action", "entity"] });

// Audit.NET Configuration 
// Tell Audit.NET to store audit events in the AuditLogs table
// via the same ApplicationDbContext. Every SaveChanges/SaveChangesAsync
// call on any audited entity will automatically produce an AuditLog record.
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

// Configure the HTTP request pipeline.
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

// Record HTTP request metrics (rate, duration, status) for Prometheus
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

// Expose Prometheus metrics at /metrics for scraping
app.MapMetrics();

app.Run();
