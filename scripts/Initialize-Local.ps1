$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$environmentFile = Join-Path $projectRoot '.env'

if (-not (Test-Path -LiteralPath $environmentFile)) {
    $bytes = New-Object byte[] 32
    $random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $random.GetBytes($bytes) } finally { $random.Dispose() }
    $password = [BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()
    $signingBytes = New-Object byte[] 48
    $random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $random.GetBytes($signingBytes) } finally { $random.Dispose() }
    $signingKey = [Convert]::ToBase64String($signingBytes)
    $template = Get-Content -Raw (Join-Path $projectRoot '.env.example')
    $template.Replace('replace-with-a-local-password', $password).Replace('replace-with-a-random-signing-key', $signingKey) |
        Set-Content -LiteralPath $environmentFile -Encoding ASCII
}

$settings = @{}
foreach ($line in Get-Content -LiteralPath $environmentFile) {
    if ($line -match '^([A-Z_]+)=(.*)$') { $settings[$Matches[1]] = $Matches[2] }
}
if (-not $settings['AUTH_SIGNING_KEY']) {
    $signingBytes = New-Object byte[] 48
    $random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $random.GetBytes($signingBytes) } finally { $random.Dispose() }
    $settings['AUTH_SIGNING_KEY'] = [Convert]::ToBase64String($signingBytes)
    Add-Content -LiteralPath $environmentFile -Value ("`nAUTH_SIGNING_KEY=" + $settings['AUTH_SIGNING_KEY']) -Encoding ASCII
}
foreach ($key in @('POSTGRES_DB', 'POSTGRES_USER', 'POSTGRES_PASSWORD', 'POSTGRES_PORT', 'AUTH_SIGNING_KEY')) {
    if (-not $settings[$key]) { throw "Missing $key in .env." }
}
if ($settings['POSTGRES_PASSWORD'] -eq 'replace-with-a-local-password') {
    throw 'Replace the password placeholder in .env, then rerun this script.'
}

if ($settings['AUTH_SIGNING_KEY'] -eq 'replace-with-a-random-signing-key' -or $settings['AUTH_SIGNING_KEY'].Length -lt 32) {
    throw 'Set AUTH_SIGNING_KEY in .env to at least 32 random characters, then rerun this script.'
}

$connection = 'Host=127.0.0.1;Port={0};Database={1};Username={2};Password={3};Timeout=30' -f
    $settings['POSTGRES_PORT'], $settings['POSTGRES_DB'], $settings['POSTGRES_USER'], $settings['POSTGRES_PASSWORD']
$apiProject = Join-Path $projectRoot 'src/Backend/TailorSpadi.Api'
dotnet user-secrets set 'ConnectionStrings:Postgres' $connection --project $apiProject | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not save the development connection string.' }
# Use the HTTPS gateway for browser authentication, including local verification.
dotnet user-secrets set 'Auth:SigningKey' $settings['AUTH_SIGNING_KEY'] --project $apiProject | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not save the development signing key.' }
dotnet user-secrets set 'Auth:CookieSecure' 'true' --project $apiProject | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not save the development cookie setting.' }
$storageRoot = Join-Path $env:LOCALAPPDATA 'TailorSpadi/storage'
dotnet user-secrets set 'Storage:Root' $storageRoot --project $apiProject | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not save the local storage directory.' }
Write-Host 'Local .env and API user secrets are configured. No credentials were printed.'
