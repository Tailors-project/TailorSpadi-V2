$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$environmentFile = Join-Path $projectRoot '.env'

if (-not (Test-Path -LiteralPath $environmentFile)) {
    $bytes = New-Object byte[] 32
    $random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $random.GetBytes($bytes) } finally { $random.Dispose() }
    $password = [BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()
    $template = Get-Content -Raw (Join-Path $projectRoot '.env.example')
    $template.Replace('replace-with-a-local-password', $password) |
        Set-Content -LiteralPath $environmentFile -Encoding ASCII
}

$settings = @{}
foreach ($line in Get-Content -LiteralPath $environmentFile) {
    if ($line -match '^([A-Z_]+)=(.*)$') { $settings[$Matches[1]] = $Matches[2] }
}
foreach ($key in @('POSTGRES_DB', 'POSTGRES_USER', 'POSTGRES_PASSWORD', 'POSTGRES_PORT')) {
    if (-not $settings[$key]) { throw "Missing $key in .env." }
}
if ($settings['POSTGRES_PASSWORD'] -eq 'replace-with-a-local-password') {
    throw 'Replace the password placeholder in .env, then rerun this script.'
}

$connection = 'Host=localhost;Port={0};Database={1};Username={2};Password={3};Timeout=5' -f
    $settings['POSTGRES_PORT'], $settings['POSTGRES_DB'], $settings['POSTGRES_USER'], $settings['POSTGRES_PASSWORD']
$apiProject = Join-Path $projectRoot 'src/Backend/TailorSpadi.Api'
dotnet user-secrets set 'ConnectionStrings:Postgres' $connection --project $apiProject | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not save the development connection string.' }
Write-Host 'Local .env and API user secrets are configured. No credentials were printed.'
