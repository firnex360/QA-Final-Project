# Configures the inventory-client Keycloak client for Blazor WASM auth.
# Requires Keycloak running at http://localhost:8080 with admin/admin credentials.

param(
    [string]$KeycloakUrl = "http://localhost:8080",
    [string]$Realm = "inventory-realm",
    [string]$ClientId = "inventory-client",
    [string]$AdminUser = "admin",
    [string]$AdminPassword = "admin"
)

$ErrorActionPreference = "Stop"

$tokenBody = @{
    client_id = "admin-cli"
    username  = $AdminUser
    password  = $AdminPassword
    grant_type = "password"
}

$token = Invoke-RestMethod -Method Post `
    -Uri "$KeycloakUrl/realms/master/protocol/openid-connect/token" `
    -Body $tokenBody `
    -ContentType "application/x-www-form-urlencoded"

$headers = @{
    Authorization = "Bearer $($token.access_token)"
    "Content-Type" = "application/json"
}

$clients = Invoke-RestMethod `
    -Uri "$KeycloakUrl/admin/realms/$Realm/clients?clientId=$ClientId" `
    -Headers $headers

if ($clients.Count -eq 0) {
    throw "Client '$ClientId' was not found in realm '$Realm'."
}

$clientUuid = $clients[0].id
$client = Invoke-RestMethod `
    -Uri "$KeycloakUrl/admin/realms/$Realm/clients/$clientUuid" `
    -Headers $headers

$client.redirectUris = @(
    "http://localhost:5167/authentication/login-callback",
    "https://localhost:7141/authentication/login-callback"
)
$client.webOrigins = @(
    "http://localhost:5167",
    "https://localhost:7141",
    "+"
)

if (-not $client.attributes) {
    $client.attributes = @{}
}

$client.attributes["post.logout.redirect.uris"] =
    "http://localhost:5167/authentication/logout-callback##https://localhost:7141/authentication/logout-callback"
$client.publicClient = $true
$client.standardFlowEnabled = $true

Invoke-RestMethod -Method Put `
    -Uri "$KeycloakUrl/admin/realms/$Realm/clients/$clientUuid" `
    -Headers $headers `
    -Body ($client | ConvertTo-Json -Depth 10)

Write-Host "Updated client '$ClientId' in realm '$Realm'."
Write-Host "Redirect URIs:"
$client.redirectUris | ForEach-Object { Write-Host "  $_" }
Write-Host "Web origins:"
$client.webOrigins | ForEach-Object { Write-Host "  $_" }
