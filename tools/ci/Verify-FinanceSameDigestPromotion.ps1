[CmdletBinding()]
param(
    [string]$ReceiptPath = '',
    [string]$ExpectedSourceSha = '',
    [string]$ExpectedRepository = '',
    [string]$ExpectedWorkflowPath = '',
    [string]$ExpectedRunId = '',
    [string]$ExpectedImage = '',
    [switch]$Library
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Stop-Promotion([string]$Message) { throw "Finance main promotion refused: $Message" }
function Require-String($Value, [string]$Name) {
    if ($Value -isnot [string] -or [string]::IsNullOrWhiteSpace($Value)) { Stop-Promotion "$Name is required." }
    return $Value
}
function Require-Property($Object, [string]$Name) {
    if ($Object -isnot [hashtable] -or -not $Object.ContainsKey($Name)) { Stop-Promotion "$Name is required." }
    return $Object[$Name]
}
function Assert-Sha([string]$Value, [string]$Name) {
    if ($Value -cnotmatch '^[a-f0-9]{40}$') { Stop-Promotion "$Name must be a lowercase 40-character Git SHA." }
}
function Assert-Digest([string]$Value, [string]$Name) {
    if ($Value -cnotmatch '^sha256:[a-f0-9]{64}$') { Stop-Promotion "$Name must be an immutable sha256 digest." }
}

function Test-FinanceSameDigestPromotion {
    param(
        [Parameter(Mandatory)][hashtable]$Receipt,
        [Parameter(Mandatory)][string]$SourceSha,
        [Parameter(Mandatory)][string]$Repository,
        [Parameter(Mandatory)][string]$WorkflowPath,
        [Parameter(Mandatory)][string]$RunId,
        [Parameter(Mandatory)][string]$Image
    )

    Assert-Sha $SourceSha 'ExpectedSourceSha'
    if ($Repository -cnotmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') { Stop-Promotion 'ExpectedRepository is malformed.' }
    if ($WorkflowPath -cnotmatch '^\.github/workflows/[A-Za-z0-9_.-]+\.ya?ml$') { Stop-Promotion 'ExpectedWorkflowPath is malformed.' }
    if ($RunId -cnotmatch '^[1-9][0-9]*$') { Stop-Promotion 'ExpectedRunId is malformed.' }
    if ($Image -cnotmatch '^ghcr\.io/[a-z0-9._/-]+$') { Stop-Promotion 'ExpectedImage is malformed.' }

    if ((Require-Property $Receipt 'schemaVersion') -ne 1) { Stop-Promotion 'schemaVersion must be 1.' }
    if ((Require-String (Require-Property $Receipt 'environment') 'environment') -cne 'development') { Stop-Promotion 'receipt must be from development.' }
    if ((Require-Property $Receipt 'productionAuthorized') -ne $false) { Stop-Promotion 'development receipt must not grant production authority.' }
    if ((Require-Property $Receipt 'certifiesDeployment') -ne $false) { Stop-Promotion 'development receipt must not certify deployment.' }

    $recordedSource = Require-String (Require-Property $Receipt 'sourceSha') 'sourceSha'
    Assert-Sha $recordedSource 'sourceSha'
    if ($recordedSource -cne $SourceSha -or (Require-String (Require-Property $Receipt 'source') 'source') -cne $SourceSha) { Stop-Promotion 'source SHA does not match main.' }

    $digest = Require-String (Require-Property $Receipt 'digest') 'digest'
    Assert-Digest $digest 'digest'
    if ((Require-String (Require-Property $Receipt 'image') 'image') -cne $Image) { Stop-Promotion 'image does not match the Finance development image.' }

    $artifact = Require-Property $Receipt 'artifact'
    if ($artifact -isnot [hashtable] -or (Require-String (Require-Property $artifact 'kind') 'artifact.kind') -cne 'oci') { Stop-Promotion 'artifact must be an OCI object.' }
    if ((Require-String (Require-Property $artifact 'digest') 'artifact.digest') -cne $digest) { Stop-Promotion 'artifact digest drifted.' }
    $reference = Require-String (Require-Property $artifact 'reference') 'artifact.reference'
    if ($reference -cne "$Image@$digest") { Stop-Promotion 'OCI reference must exactly bind the Finance image and digest.' }

    $provenance = Require-Property $Receipt 'provenance'
    if ($provenance -isnot [hashtable]) { Stop-Promotion 'provenance must be an object.' }
    if ((Require-String (Require-Property $provenance 'repository') 'provenance.repository') -cne $Repository) { Stop-Promotion 'provenance repository drifted.' }
    if ((Require-String (Require-Property $provenance 'workflowPath') 'provenance.workflowPath') -cne $WorkflowPath) { Stop-Promotion 'provenance workflow drifted.' }
    if ((Require-String ([string](Require-Property $provenance 'runId')) 'provenance.runId') -cne $RunId) { Stop-Promotion 'provenance run drifted.' }
    if ((Require-String (Require-Property $provenance 'event') 'provenance.event') -cne 'push' -or
        (Require-String (Require-Property $provenance 'ref') 'provenance.ref') -cne 'refs/heads/develop' -or
        (Require-String (Require-Property $provenance 'conclusion') 'provenance.conclusion') -cne 'success') {
        Stop-Promotion 'provenance is not a successful develop push.'
    }

    $promotion = Require-Property $Receipt 'promotion'
    if ($promotion -isnot [hashtable] -or
        (Require-Property $promotion 'developmentValidated') -ne $true -or
        (Require-Property $promotion 'sourceVerified') -ne $true -or
        (Require-Property $promotion 'certifiesProduction') -ne $false) {
        Stop-Promotion 'promotion boundary is invalid.'
    }

    return [ordered]@{
        status = 'verified-no-rebuild-no-deploy'
        sourceSha = $SourceSha
        image = $Image
        digest = $digest
        developmentRunId = $RunId
    }
}

if ($Library) { return }
if (-not (Test-Path -LiteralPath $ReceiptPath -PathType Leaf)) { Stop-Promotion 'development receipt is missing.' }
try { $receipt = ConvertFrom-Json -AsHashtable -Depth 16 ([IO.File]::ReadAllText((Resolve-Path -LiteralPath $ReceiptPath))) }
catch { Stop-Promotion 'development receipt is not valid JSON.' }
(Test-FinanceSameDigestPromotion -Receipt $receipt -SourceSha $ExpectedSourceSha -Repository $ExpectedRepository -WorkflowPath $ExpectedWorkflowPath -RunId $ExpectedRunId -Image $ExpectedImage) | ConvertTo-Json -Compress
