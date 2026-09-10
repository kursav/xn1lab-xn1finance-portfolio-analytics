$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'Verify-FinanceSameDigestPromotion.ps1') -Library

$workflow = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '../../.github/workflows/finance-same-digest-main-promotion.yml')
foreach ($marker in @(
    'branches: [main]',
    'refs/heads/develop:refs/remotes/origin/develop',
    'candidate_sha^{tree}',
    'TARGET_SHA',
    'No build and no deployment run on main'
)) {
    if (-not $workflow.Contains($marker, [StringComparison]::Ordinal)) {
        throw "Main promotion workflow marker is missing: $marker"
    }
}
foreach ($forbidden in @('pull_request:', 'docker/build-push-action', 'Deploy exact image', 'gh workflow run')) {
    if ($workflow.Contains($forbidden, [StringComparison]::Ordinal)) {
        throw "Main promotion workflow must not contain: $forbidden"
    }
}

$source = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
$target = 'cccccccccccccccccccccccccccccccccccccccc'
$targetTree = 'dddddddddddddddddddddddddddddddddddddddd'
$digest = 'sha256:' + ('b' * 64)
$repository = 'kursav/xn1lab-xn1finance-portfolio-analytics'
$developmentWorkflow = '.github/workflows/finance-development.yml'
$image = 'ghcr.io/kursav/xn1lab-xn1finance-portfolio-analytics-dev'

function New-Receipt {
    return @{
        schemaVersion = 1
        environment = 'development'
        source = $source
        sourceSha = $source
        image = $image
        digest = $digest
        certifiesDeployment = $false
        productionAuthorized = $false
        artifact = @{ kind = 'oci'; reference = "$image@$digest"; digest = $digest }
        provenance = @{ repository = $repository; workflowPath = $developmentWorkflow; runId = 123; event = 'push'; ref = 'refs/heads/develop'; conclusion = 'success' }
        promotion = @{ developmentValidated = $true; sourceVerified = $true; certifiesProduction = $false }
    }
}

$positive = Test-FinanceSameDigestPromotion -Receipt (New-Receipt) -SourceSha $source -TargetSha $target -TargetTreeSha $targetTree -Repository $repository -WorkflowPath $developmentWorkflow -RunId '123' -Image $image
if ($positive.status -cne 'verified-no-rebuild-no-deploy') { throw 'Positive receipt was not accepted.' }

foreach ($mutation in @(
    @{ name = 'source'; apply = { param($receipt) $receipt.sourceSha = ('c' * 40) } },
    @{ name = 'digest'; apply = { param($receipt) $receipt.artifact.digest = 'sha256:' + ('e' * 64) } },
    @{ name = 'provenance'; apply = { param($receipt) $receipt.provenance.workflowPath = '.github/workflows/other.yml' } },
    @{ name = 'authority'; apply = { param($receipt) $receipt.productionAuthorized = $true } }
)) {
    $receipt = New-Receipt
    & $mutation.apply $receipt
    try {
        $null = Test-FinanceSameDigestPromotion -Receipt $receipt -SourceSha $source -TargetSha $target -TargetTreeSha $targetTree -Repository $repository -WorkflowPath $developmentWorkflow -RunId '123' -Image $image
        throw "Negative $($mutation.name) receipt was accepted."
    } catch {
        if ($_.Exception.Message -like "Negative $($mutation.name)*") { throw }
    }
}

Write-Host 'Finance same-digest promotion tests passed.'
