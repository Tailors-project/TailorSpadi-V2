param([string]$BaseUrl = 'http://localhost:8080')
$ErrorActionPreference = 'Stop'

$checks = @(
    @{ HostName = 'localhost'; Path = '/'; Title = 'TailorSpadi' },
    @{ HostName = 'localhost'; Path = '/register'; Title = 'TailorSpadi Business' },
    @{ HostName = 'localhost'; Path = '/onboarding'; Title = 'TailorSpadi Business' },
    @{ HostName = 'localhost'; Path = '/admin/users'; Title = 'TailorSpadi Administration' },
    @{ HostName = 'johnfashion-001.localhost'; Path = '/'; Title = 'TailorSpadi' },
    @{ HostName = 'johnfashion-001.localhost'; Path = '/login'; Title = 'TailorSpadi Business' },
    @{ HostName = 'johnfashion-001.localhost'; Path = '/backoffice/orders'; Title = 'TailorSpadi Business' }
)
foreach ($check in $checks) {
    $response = Invoke-WebRequest ($BaseUrl + $check.Path) -Headers @{ Host = $check.HostName } -UseBasicParsing
    if ($response.StatusCode -ne 200 -or $response.Content -notmatch ('<title>' + [regex]::Escape($check.Title) + '</title>')) {
        throw "Wrong frontend for $($check.HostName)$($check.Path)"
    }
    $assetPaths = [regex]::Matches($response.Content, '(?:src|href)="(/[^"]+\.(?:js|css))"')
    foreach ($asset in $assetPaths) {
        $assetResponse = Invoke-WebRequest ($BaseUrl + $asset.Groups[1].Value) -Headers @{ Host = $check.HostName } -UseBasicParsing
        if ($assetResponse.StatusCode -ne 200 -or $assetResponse.Headers['Content-Type'] -match 'text/html') {
            throw "Asset failed: $($asset.Groups[1].Value)"
        }
    }
    Write-Host "PASS $($check.HostName)$($check.Path) and assets"
}

foreach ($hostName in @('localhost', 'johnfashion-001.localhost')) {
    $metadata = Invoke-RestMethod ($BaseUrl + '/api/system-info') -Headers @{ Host = $hostName }
    if ($metadata.applicationName -ne 'TailorSpadi') { throw 'Unexpected API response.' }
    Write-Host "PASS $hostName/api/system-info"
}

foreach ($check in @(
    @{ HostName = 'unknown.localhost'; Path = '/'; Status = 400 },
    @{ HostName = 'localhost'; Path = '/api/missing'; Status = 404 }
)) {
    $status = 0
    try {
        $response = Invoke-WebRequest ($BaseUrl + $check.Path) -Headers @{ Host = $check.HostName } -UseBasicParsing
        $status = [int]$response.StatusCode
    } catch {
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode } else { throw }
    }
    if ($status -ne $check.Status) { throw "Expected $($check.Status), received $status." }
    Write-Host "PASS rejection of $($check.HostName)$($check.Path)"
}
