$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot 'Verify-FinanceSameDigestPromotion.ps1') -Library

$source = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
$digest = 'sha256:' + ('b' * 64)
$repository = 'kursav/xn1lab-xn1finance-portfolio-analytics'
$workflow = '.github/workflows/finance-development.yml'
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
        provenance = @{ repository = $repository; workflowPath = $workflow; runId = 123; event = 'push'; ref = 'refs/heads/develop'; conclusion = 'success' }
        promotion = @{ developmentValidated = $true; sourceVerified = $true; certifiesProduction = $false }
    }
}

$positive = Test-FinanceSameDigestPromotion -Receipt (New-Receipt) -SourceSha $source -Repository $repository -WorkflowPath $workflow -RunId '123' -Image $image
if ($positive.status -cne 'verified-no-rebuild-no-deploy') { throw 'Positive receipt was not accepted.' }

foreach ($mutation in @(
    @{ name = 'source'; apply = { param($receipt) $receipt.sourceSha = ('c' * 40) } },
    @{ name = 'digest'; apply = { param($receipt) $receipt.artifact.digest = 'sha256:' + ('d' * 64) } },
    @{ name = 'provenance'; apply = { param($receipt) $receipt.provenance.workflowPath = '.github/workflows/other.yml' } },
    @{ name = 'authority'; apply = { param($receipt) $receipt.productionAuthorized = $true } }
)) {
    $receipt = New-Receipt
    & $mutation.apply $receipt
    try {
        $null = Test-FinanceSameDigestPromotion -Receipt $receipt -SourceSha $source -Repository $repository -WorkflowPath $workflow -RunId '123' -Image $image
        throw "Negative $($mutation.name) receipt was accepted."
    } catch {
        if ($_.Exception.Message -like "Negative $($mutation.name)*") { throw }
    }
}

Write-Host 'Finance same-digest promotion tests passed.'
