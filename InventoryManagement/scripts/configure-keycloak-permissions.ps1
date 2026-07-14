param(
    [string]$KeycloakUrl = "http://localhost:8080",
    [string]$Realm = "inventory-realm",
    [string]$ClientId = "inventory-client",
    [string]$AdminUser = "admin",
    [string]$AdminPassword = "admin"
)

$ErrorActionPreference = "Stop"

# ── Permission matrix (from the project guidelines) ──────────────────
$permissions = @(
    @{ name = "product:view";   description = "Ver productos" },
    @{ name = "product:manage"; description = "Crear, editar y eliminar productos" },
    @{ name = "stock:view";     description = "Ver existencia e historial" },
    @{ name = "stock:manage";   description = "Registrar entradas, salidas y ajustes" },
    @{ name = "report:view";    description = "Ver reportes y dashboard" },
    @{ name = "user:manage";    description = "Gestionar usuarios, roles y permisos" },
    @{ name = "audit:view";     description = "Consultar auditoria del sistema" }
)

# ── Role → permission bundles (edit HERE or in the admin console, never in code) ──
$roleBundles = @{
    "adminY"   = @("product:view", "product:manage", "stock:view", "stock:manage", "report:view", "user:manage", "audit:view")
    "managerY" = @("product:view", "product:manage", "stock:view", "stock:manage", "report:view", "audit:view")
    "staffY"   = @("product:view", "stock:view", "report:view")
}

# ── Authenticate as admin ────────────────────────────────────────────
$tokenBody = @{
    client_id  = "admin-cli"
    username   = $AdminUser
    password   = $AdminPassword
    grant_type = "password"
}

$token = Invoke-RestMethod -Method Post `
    -Uri "$KeycloakUrl/realms/master/protocol/openid-connect/token" `
    -Body $tokenBody `
    -ContentType "application/x-www-form-urlencoded"

$headers = @{
    Authorization  = "Bearer $($token.access_token)"
    "Content-Type" = "application/json"
}

# ── Locate the client ────────────────────────────────────────────────
$clients = Invoke-RestMethod `
    -Uri "$KeycloakUrl/admin/realms/$Realm/clients?clientId=$ClientId" `
    -Headers $headers

if ($clients.Count -eq 0) {
    throw "Client '$ClientId' was not found in realm '$Realm'."
}
$clientUuid = $clients[0].id

$rolesUrl = "$KeycloakUrl/admin/realms/$Realm/clients/$clientUuid/roles"

# ── 1. Create each permission role if it doesn't exist ───────────────
foreach ($perm in $permissions) {
    $encoded = [uri]::EscapeDataString($perm.name)
    $exists = $true
    try {
        Invoke-RestMethod -Uri "$rolesUrl/$encoded" -Headers $headers | Out-Null
    } catch {
        $exists = $false
    }

    if ($exists) {
        Write-Host "Permission role already exists: $($perm.name)"
    } else {
        Invoke-RestMethod -Method Post -Uri $rolesUrl -Headers $headers `
            -Body (@{ name = $perm.name; description = $perm.description } | ConvertTo-Json)
        Write-Host "Created permission role: $($perm.name)"
    }
}

# ── 2. Make adminY/managerY/staffY composites of their permission bundles ──
foreach ($roleName in $roleBundles.Keys) {
    $encoded = [uri]::EscapeDataString($roleName)

    # The role must exist (it holds the user assignments)
    try {
        Invoke-RestMethod -Uri "$rolesUrl/$encoded" -Headers $headers | Out-Null
    } catch {
        Write-Warning "Role '$roleName' not found in client '$ClientId' - skipping."
        continue
    }

    # Fetch full role representations for the permissions in this bundle
    $bundleRoles = @()
    foreach ($permName in $roleBundles[$roleName]) {
        $permEncoded = [uri]::EscapeDataString($permName)
        $bundleRoles += Invoke-RestMethod -Uri "$rolesUrl/$permEncoded" -Headers $headers
    }

    # Adding composites is additive and idempotent in Keycloak
    Invoke-RestMethod -Method Post `
        -Uri "$rolesUrl/$encoded/composites" `
        -Headers $headers `
        -Body (ConvertTo-Json @($bundleRoles) -Depth 5)

    Write-Host "Set '$roleName' -> [$($roleBundles[$roleName] -join ', ')]"
}

Write-Host ""
Write-Host "Done. Users must log out and back in (or refresh their token) to receive the new permission claims."
