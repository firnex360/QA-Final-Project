using System.Text.Json;
using Audit.Core;
using InventorySystem.Server.Data;
using InventorySystem.Server.Models;
using InventorySystem.Server.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
        policy.WithOrigins("http://localhost:5167", "https://localhost:7141")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:8080/realms/inventory-realm";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            NameClaimType = "preferred_username",
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanCreate", policy =>
        policy.RequireRole( "adminY", "managerY"));

    options.AddPolicy("CanRead", policy =>
        policy.RequireRole("adminY", "managerY", "staffY"));

    options.AddPolicy("CanUpdate", policy =>
        policy.RequireRole("adminY", "managerY"));

    options.AddPolicy("CanDelete", policy =>
        policy.RequireRole("adminY"));
});


// ── Audit.NET Configuration ──────────────────────────────────────────
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

// Map controller endpoints
app.MapControllers();

app.Run();
