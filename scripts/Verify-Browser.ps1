param(
    [string]$BaseUrl = 'https://tailorspadi.localhost:8443',
    [switch]$Local
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $projectRoot 'artifacts/browser'
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$edge = Join-Path ${env:ProgramFiles(x86)} 'Microsoft/Edge/Application/msedge.exe'
if (-not (Test-Path -LiteralPath $edge)) { throw 'This optional browser check requires Microsoft Edge on Windows.' }

if ($Local) {
    $checks = @(
        @{ Url = 'http://127.0.0.1:5173/'; Text = 'A home for great tailoring.' },
        @{ Url = 'http://127.0.0.1:5174/register'; Text = 'Create your account' },
        @{ Url = 'http://127.0.0.1:5174/login'; Text = 'Welcome back' },
        @{ Url = 'http://127.0.0.1:5175/admin/login'; Text = 'Administrator sign in' }
    )
} else {
    # No tenant exists until one is onboarded, so a tenant host demonstrates the not-found page.
    $unknownTenant = New-Object System.UriBuilder($BaseUrl)
    $unknownTenant.Host = 'nobody-here.' + $unknownTenant.Host
    $unknownTenantUrl = $unknownTenant.Uri.AbsoluteUri.TrimEnd('/')
    $checks = @(
        @{ Url = $BaseUrl + '/'; Text = 'A home for great tailoring.' },
        @{ Url = $BaseUrl + '/register'; Text = 'Create your account' },
        @{ Url = $BaseUrl + '/login'; Text = 'Welcome back' },
        @{ Url = $BaseUrl + '/admin/login'; Text = 'Administrator sign in' },
        @{ Url = $unknownTenantUrl + '/'; Text = 'Store not found' }
    )
}

$index = 0
foreach ($check in $checks) {
    $index++
    $profile = Join-Path $artifacts "profile-$index"
    $outputFile = Join-Path $artifacts "page-$index.html"
    $errorFile = Join-Path $artifacts "page-$index.log"
    $browserArguments = @(
        '--headless=new', '--disable-gpu', '--no-first-run',
        '--no-default-browser-check',
        ('"--user-data-dir={0}"' -f $profile),
        '--virtual-time-budget=10000', '--dump-dom', $check.Url
    )
    $browserProcess = Start-Process -FilePath $edge -ArgumentList $browserArguments -WindowStyle Hidden -PassThru -RedirectStandardOutput $outputFile -RedirectStandardError $errorFile
    # Edge can keep background services alive after dumping a complete page.
    # Read the rendered result, then stop only the browser instance we started.
    for ($attempt = 0; $attempt -lt 90 -and -not $browserProcess.HasExited; $attempt++) {
        if ((Test-Path -LiteralPath $outputFile) -and
            (Get-Content -Raw -LiteralPath $outputFile) -match [regex]::Escape($check.Text)) { break }
        $browserProcess.WaitForExit(500) | Out-Null
    }
    if (-not $browserProcess.HasExited) {
        Stop-Process -Id $browserProcess.Id -Force -ErrorAction SilentlyContinue
    }
    $dom = Get-Content -Raw -LiteralPath $outputFile
    if ($dom -notmatch [regex]::Escape($check.Text)) { throw "Expected rendered content missing at $($check.Url)" }
    Write-Host "PASS rendered page $($check.Url)"
}
