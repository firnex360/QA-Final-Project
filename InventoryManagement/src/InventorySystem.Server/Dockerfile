# Build Stage 
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching on restore
COPY InventorySystem.Shared/InventorySystem.Shared.csproj InventorySystem.Shared/
COPY InventorySystem.Server/InventorySystem.Server.csproj InventorySystem.Server/
RUN dotnet restore InventorySystem.Server/InventorySystem.Server.csproj

# Copy the rest of the source and publish
COPY InventorySystem.Shared/ InventorySystem.Shared/
COPY InventorySystem.Server/ InventorySystem.Server/
RUN dotnet publish InventorySystem.Server/InventorySystem.Server.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# ASP.NET Core listens on port 8080 by default in containers
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
EXPOSE 8080

ENTRYPOINT ["dotnet", "InventorySystem.Server.dll"]
