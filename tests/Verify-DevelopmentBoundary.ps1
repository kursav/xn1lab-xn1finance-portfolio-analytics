$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$workflowPath = Join-Path $root '.github/workflows/finance-development.yml'
$dockerfilePath = Join-Path $root 'Dockerfile.development'
$nginxPath = Join-Path $root 'nginx/development.conf'
$platformPinPath = Join-Path $root '.github/platform-revision.txt'
$configPath = Join-Path $root 'config/development/runtime-config.template.json'
foreach ($path in @($workflowPath, $dockerfilePath, $nginxPath, $platformPinPath, $configPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing development boundary file: $path"
    }
}

$workflow = Get-Content -Raw -LiteralPath $workflowPath
$dockerfile = Get-Content -Raw -LiteralPath $dockerfilePath
$nginx = Get-Content -Raw -LiteralPath $nginxPath
$platformPin = (Get-Content -Raw -LiteralPath $platformPinPath).Trim()
$configText = Get-Content -Raw -LiteralPath $configPath
$config = $configText | ConvertFrom-Json

if ($platformPin -notmatch '^[0-9a-f]{40}$') {
    throw 'Platform revision must be a full immutable SHA.'
}
if ($config.Authentication.Keycloak.Authority -ne 'https://auth.dev.xn1lab.com' -or
    $config.Authentication.Keycloak.Realm -ne 'xn1lab-development' -or
    $config.CoreServicesApi.BaseUrl -ne 'https://api.dev.xn1lab.com') {
    throw 'Finance development runtime configuration must target only development identity and APIs.'
}
if ($configText -match 'https://(api|auth)\.xn1lab\.com') {
    throw 'Production endpoints are forbidden in Finance development configuration.'
}
foreach ($marker in @(
    'branches: [develop]',
    'ghcr.io/kursav/xn1lab-xn1finance-portfolio-analytics-dev',
    'org.opencontainers.image.xn1lab.environment=development',
    'productionAuthorized":false',
    'certifiesDeployment":false',
    "provenance: `${{ github.event_name == 'push' && 'mode=max' || 'false' }}",
    "sbom: `${{ github.event_name == 'push' }}"
)) {
    if (-not $workflow.Contains($marker, [StringComparison]::Ordinal)) {
        throw "Development workflow marker is missing: $marker"
    }
}
if ($workflow -match 'xn1lab-xn1finance-portfolio-analytics:(latest|production|prod)') {
    throw 'Development workflow must not publish mutable or production tags.'
}
if ([regex]::Matches($dockerfile, '(?m)^FROM .+@sha256:[0-9a-f]{64}(?:\s+AS\s+\w+)?\r?$').Count -ne 2) {
    throw 'Every Finance development base image must be pinned by digest.'
}
foreach ($marker in @('USER 101:101', 'EXPOSE 8080', 'ENTRYPOINT []', "-iname 'appsettings*.json' -delete")) {
    if (-not $dockerfile.Contains($marker, [StringComparison]::Ordinal)) {
        throw "Finance runtime boundary marker is missing: $marker"
    }
}
foreach ($marker in @('location = /health/live', 'location = /health/ready', 'listen 8080')) {
    if (-not $nginx.Contains($marker, [StringComparison]::Ordinal)) {
        throw "Finance health boundary marker is missing: $marker"
    }
}

Write-Output 'XN1Finance development boundary checks passed: 1/1.'
