# ── Build Stage ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching on restore
COPY InventorySystem.Shared/InventorySystem.Shared.csproj InventorySystem.Shared/
COPY InventorySystem.Client/InventorySystem.Client.csproj InventorySystem.Client/
RUN dotnet restore InventorySystem.Client/InventorySystem.Client.csproj

# Copy the rest of the source and publish
COPY InventorySystem.Shared/ InventorySystem.Shared/
COPY InventorySystem.Client/ InventorySystem.Client/
RUN dotnet publish InventorySystem.Client/InventorySystem.Client.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Serve Stage ───────────────────────────────────────────────────────
# Blazor WASM compiles to static files — serve them with nginx
FROM nginx:alpine AS runtime

# Copy the published Blazor WASM output into nginx's serve directory
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Custom nginx config for SPA routing (fallback to index.html)
COPY InventorySystem.Client/nginx.conf /etc/nginx/nginx.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
