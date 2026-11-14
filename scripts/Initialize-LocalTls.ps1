param([switch]$Trust, [switch]$Renew)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$certDirectory = Join-Path $projectRoot 'deploy/certs'
New-Item -ItemType Directory -Force -Path $certDirectory | Out-Null
$certificate = Join-Path $certDirectory 'localhost.crt'
$privateKey = Join-Path $certDirectory 'localhost.key'
$openssl = (Get-Command openssl -ErrorAction SilentlyContinue).Source
if (-not $openssl) { $openssl = Join-Path $env:ProgramFiles 'Git/usr/bin/openssl.exe' }
if (-not (Test-Path -LiteralPath $openssl)) { throw 'Install OpenSSL (Git for Windows includes it), then rerun this script.' }
if ($Renew -or -not (Test-Path -LiteralPath $certificate) -or -not (Test-Path -LiteralPath $privateKey)) {
    $ErrorActionPreference = 'Continue'
    & $openssl req -x509 -newkey rsa:3072 -nodes -sha256 -days 90 -keyout $privateKey -out $certificate -subj '/CN=TailorSpadi local development' -addext 'subjectAltName=DNS:localhost,DNS:tailorspadi.localhost,DNS:*.tailorspadi.localhost,IP:127.0.0.1' -addext 'extendedKeyUsage=serverAuth' 2>$null
    $ErrorActionPreference = 'Stop'
    if ($LASTEXITCODE -ne 0) { throw 'Could not create the local TLS certificate.' }
}
if ($Trust) {
    Import-Certificate -FilePath $certificate -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
    Write-Host 'Trusted the local certificate for this Windows user.'
}
Write-Host 'Local TLS certificate is ready. Open https://tailorspadi.localhost:8443 after starting the gateway.'
