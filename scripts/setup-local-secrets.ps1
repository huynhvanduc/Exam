<#
.SYNOPSIS
Generate (or reuse) dev secrets, write them to .env (for docker compose) and sync them into
.NET User Secrets for Identity.Server + Exam.API (for "dotnet run" without Docker).

Run once after cloning the repo, or any time you want to rotate the dev secrets.
#>

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $repoRoot ".env"

function New-Secret {
    -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 24 | ForEach-Object { [char]$_ })
}

# Reuse existing values from .env if present (so we don't invalidate secrets already in use by
# running services), only generate new ones for whatever is missing.
$existing = @{}
if (Test-Path $envPath) {
    Get-Content $envPath | ForEach-Object {
        if ($_ -match '^\s*([A-Z_]+)=(.*)$') {
            $existing[$matches[1]] = $matches[2]
        }
    }
}

function Get-OrCreateSecret([string]$key, [string]$default = $null) {
    if ($existing.ContainsKey($key) -and $existing[$key] -and $existing[$key] -ne "change-me") {
        return $existing[$key]
    }
    if ($default) { return $default }
    return New-Secret
}

# DOMAIN không phải secret (không sinh ngẫu nhiên) - chỉ giữ lại giá trị đã có, mặc định "localhost".
$domain = if ($existing.ContainsKey("DOMAIN") -and $existing["DOMAIN"]) { $existing["DOMAIN"] } else { "localhost" }

$sqlSaPassword = Get-OrCreateSecret "SQL_SA_PASSWORD"
$mongoRootUser = Get-OrCreateSecret "MONGO_ROOT_USER" "admin"
$mongoRootPassword = Get-OrCreateSecret "MONGO_ROOT_PASSWORD"
$internalClientSecret = Get-OrCreateSecret "IDENTITY_INTERNAL_CLIENT_SECRET"
$warehouseClientSecret = Get-OrCreateSecret "IDENTITY_WAREHOUSE_CLIENT_SECRET"
$testerClientSecret = Get-OrCreateSecret "IDENTITY_TESTER_CLIENT_SECRET"

@"
DOMAIN=$domain

SQL_SA_PASSWORD=$sqlSaPassword

MONGO_ROOT_USER=$mongoRootUser
MONGO_ROOT_PASSWORD=$mongoRootPassword

IDENTITY_INTERNAL_CLIENT_SECRET=$internalClientSecret
IDENTITY_WAREHOUSE_CLIENT_SECRET=$warehouseClientSecret
IDENTITY_TESTER_CLIENT_SECRET=$testerClientSecret
"@ | Set-Content -Path $envPath -Encoding utf8

Write-Host "Wrote $envPath"

$identityServerProject = Join-Path $repoRoot "src/Services/Identity/Identity.Server/Identity.Server.csproj"
$examApiProject = Join-Path $repoRoot "src/Services/Exam/Exam.API/Exam.API.csproj"

# Index must match the order of the IdentityServer:Clients array in appsettings.json
# (0=exam.api.internal, 1=exam.warehouse.worker, 2=exam.tester).
dotnet user-secrets set "IdentityServer:Clients:0:ClientSecret" $internalClientSecret --project $identityServerProject | Out-Null
dotnet user-secrets set "IdentityServer:Clients:1:ClientSecret" $warehouseClientSecret --project $identityServerProject | Out-Null
dotnet user-secrets set "IdentityServer:Clients:2:ClientSecret" $testerClientSecret --project $identityServerProject | Out-Null
Write-Host "Synced User Secrets for Identity.Server"

dotnet user-secrets set "IdentityServer:InternalClientSecret" $internalClientSecret --project $examApiProject | Out-Null
Write-Host "Synced User Secrets for Exam.API"

Write-Host "`nDone. Run 'docker compose up -d --build' or 'dotnet run' as usual."
