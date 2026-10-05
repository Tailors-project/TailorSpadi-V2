param(
    [string]$BaseUrl = 'http://localhost:8080',
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
        @{ Url = 'http://127.0.0.1:5173/'; Text = 'TailorSpadi is connected.' },
        @{ Url = 'http://127.0.0.1:5174/login'; Text = 'Welcome back' },
        @{ Url = 'http://127.0.0.1:5174/backoffice/orders'; Text = 'Your workspace' },
        @{ Url = 'http://127.0.0.1:5175/admin/users'; Text = 'The platform management workspace is coming soon.' }
    )
} else {
    $tenantUrl = $BaseUrl.Replace('localhost', 'johnfashion-001.localhost')
    $checks = @(
        @{ Url = $BaseUrl + '/'; Text = 'TailorSpadi is connected.' },
        @{ Url = $BaseUrl + '/register'; Text = 'Create your account' },
        @{ Url = $BaseUrl + '/onboarding'; Text = 'Set up your business' },
        @{ Url = $BaseUrl + '/admin/users'; Text = 'The platform management workspace is coming soon.' },
        @{ Url = $tenantUrl + '/'; Text = 'TailorSpadi is connected.' },
        @{ Url = $tenantUrl + '/login'; Text = 'Welcome back' },
        @{ Url = $tenantUrl + '/backoffice/orders'; Text = 'Your workspace' }
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
