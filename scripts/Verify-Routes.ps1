param([string]$BaseUrl = 'https://tailorspadi.localhost:8443')
$ErrorActionPreference = 'Stop'
# Windows DNS does not resolve wildcard localhost names; keep Host routing while connecting to loopback.
$connectionUrl = New-Object System.UriBuilder($BaseUrl)
if ($connectionUrl.Host.EndsWith('.localhost')) { $connectionUrl.Host = 'localhost' }
$connectionBase = $connectionUrl.Uri.AbsoluteUri.TrimEnd('/')

$checks = @(
    @{ HostName = ([uri]$BaseUrl).Host; Path = '/'; Title = 'TailorSpadi' },
    @{ HostName = ([uri]$BaseUrl).Host; Path = '/register'; Title = 'TailorSpadi Business' },
    @{ HostName = ([uri]$BaseUrl).Host; Path = '/login'; Title = 'TailorSpadi Business' },
    @{ HostName = ([uri]$BaseUrl).Host; Path = '/onboarding'; Title = 'TailorSpadi Business' },
    @{ HostName = ([uri]$BaseUrl).Host; Path = '/admin/users'; Title = 'TailorSpadi Administration' }
)
foreach ($check in $checks) {
    $response = Invoke-WebRequest ($connectionBase + $check.Path) -Headers @{ Host = $check.HostName } -UseBasicParsing
    if ($response.StatusCode -ne 200 -or $response.Content -notmatch ('<title>' + [regex]::Escape($check.Title) + '</title>')) {
        throw "Wrong frontend for $($check.HostName)$($check.Path)"
    }
    $assetPaths = [regex]::Matches($response.Content, '(?:src|href)="(/[^"]+\.(?:js|css))"')
    foreach ($asset in $assetPaths) {
        $assetResponse = Invoke-WebRequest ($connectionBase + $asset.Groups[1].Value) -Headers @{ Host = $check.HostName } -UseBasicParsing
        if ($assetResponse.StatusCode -ne 200 -or $assetResponse.Headers['Content-Type'] -match 'text/html') {
            throw "Asset failed: $($asset.Groups[1].Value)"
        }
    }
    Write-Host "PASS $($check.HostName)$($check.Path) and assets"
}

$metadata = Invoke-RestMethod ($connectionBase + '/api/system-info') -Headers @{ Host = ([uri]$BaseUrl).Host }
if ($metadata.applicationName -ne 'TailorSpadi') { throw 'Unexpected API response.' }
Write-Host 'PASS localhost/api/system-info'

foreach ($check in @(
    @{ HostName = ('nobody-here.' + ([uri]$BaseUrl).Host); Path = '/api/tenant/current'; Status = 404 },
    @{ HostName = ([uri]$BaseUrl).Host; Path = '/api/tenant/current'; Status = 404 },
    @{ HostName = 'example.invalid'; Path = '/'; Status = 400 },
    @{ HostName = ([uri]$BaseUrl).Host; Path = '/api/missing'; Status = 404 }
)) {
    $status = 0
    try {
        $response = Invoke-WebRequest ($connectionBase + $check.Path) -Headers @{ Host = $check.HostName } -UseBasicParsing
        $status = [int]$response.StatusCode
    } catch {
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode } else { throw }
    }
    if ($status -ne $check.Status) { throw "Expected $($check.Status), received $status." }
    Write-Host "PASS rejection of $($check.HostName)$($check.Path)"
}
