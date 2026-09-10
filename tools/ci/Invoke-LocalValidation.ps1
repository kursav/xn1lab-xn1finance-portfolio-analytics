[CmdletBinding()]
param([string]$BaseRef = 'origin/develop', [string]$ExpectedHead = '', [switch]$ForPush, [switch]$PrePush, [switch]$Library)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Assert-WorkflowPolicy([string]$Text) {
    $lines = $Text.Replace("`r", '').Split("`n")
    foreach ($line in $lines) {
        if ($line -match '^\S' -and $line -notmatch '^#' -and $line -notmatch '^(name|on|permissions|env|concurrency|jobs|defaults):') { throw 'Unsupported top-level workflow syntax.' }
    }
    # Only this deliberately small trigger grammar is admitted; no aliases, tags or implicit events.
    $on = @($lines | Where-Object { $_ -cmatch '^on:' })
    if ($on.Count -ne 1 -or $on[0] -cne 'on:') { throw 'Workflow needs one literal on: mapping.' }
    $start = [Array]::IndexOf($lines, 'on:') + 1
    $body = @()
    for ($i = $start; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^\S') { break }
        if ($lines[$i].Trim()) { $body += $lines[$i] }
    }
    if ($body.Count -lt 2 -or $body[0] -cne '  push:') { throw 'Only integration push is automatic.' }
    $branches = @()
    if ($body[1] -cmatch '^    branches: \[(main|develop)(, (main|develop))?\]$') {
        if ($body.Count -ne 2) { throw 'Extra trigger fields are not supported.' }
        $branches = @($body[1].Substring(15).TrimEnd(']').Split(',') | ForEach-Object { $_.Trim() })
    } elseif ($body[1] -ceq '    branches:' -and $body.Count -gt 2) {
        foreach ($line in $body[2..($body.Count - 1)]) {
            if ($line -cnotmatch '^      - (main|develop)$') { throw 'Only exact develop/main branches are supported.' }
            $branches += $Matches[1]
        }
    } else { throw 'Unsupported workflow trigger grammar.' }
    if ($branches.Count -eq 0 -or @($branches | Select-Object -Unique).Count -ne $branches.Count) { throw 'Missing/duplicate integration branches.' }
}

function Get-ValidationScope([string[]]$Paths) {
    if (!$Paths.Count) { return 'no-change' }
    $needsBuild = $false
    foreach ($path in $Paths) {
        if ($path -match '[\x00-\x1f"\\]' -or $path.StartsWith('/') -or $path -match '(^|/)\.\.(/|$)') { throw 'Unsafe changed path.' }
        if ($path -notmatch '(^docs/.*\.md$|^[^/]+\.md$|^\.github/(workflows/[^/]+\.ya?ml|CODEOWNERS)$|^tools/ci/((Invoke-LocalValidation|Test-LocalValidation)\.ps1|README\.md)$|^\.githooks/pre-push$|^\.gitattributes$)') { $needsBuild = $true }
    }
    if ($needsBuild) { return 'dotnet' }
    return 'policy-only'
}

function Get-PushTarget([string]$InputText, [string]$Head, [string]$Branch) {
    $rows = @($InputText.Split("`n") | Where-Object { $_.Trim() })
    if ($rows.Count -ne 1) { throw 'Pre-push requires exactly one current-HEAD branch update.' }
    if ($rows[0].Trim() -cnotmatch '^(refs/heads/[A-Za-z0-9][A-Za-z0-9/._-]*) ([0-9a-f]{40}) (refs/heads/[A-Za-z0-9][A-Za-z0-9/._-]*) ([0-9a-f]{40})$') { throw 'Unsupported push ref record.' }
    $localRef = $Matches[1]; $localSha = $Matches[2]; $remoteRef = $Matches[3]; $remoteSha = $Matches[4]
    if ($localSha -cne $Head -or $localSha -eq ('0' * 40) -or $localRef -cne "refs/heads/$Branch" -or $remoteRef -cne $localRef) { throw 'Non-HEAD, deletion, tag or cross-branch push refused.' }
    return @{ ExpectedHead = $Head; BaseRef = $(if ($remoteSha -eq ('0' * 40)) { 'origin/develop' } else { $remoteSha }) }
}

function Invoke-Checked([string]$Executable, [string[]]$Arguments) {
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Local command failed: $Executable ($LASTEXITCODE)" }
}
function Read-Git([string[]]$Arguments) {
    $result = @(& git @Arguments)
    if ($LASTEXITCODE -ne 0) { throw 'Git inspection failed.' }
    return $result
}
function Assert-CleanHead([string]$Head) {
    if ((Read-Git @('rev-parse', 'HEAD')) -cne $Head -or @(Read-Git @('status', '--porcelain')).Count) { throw 'Exact clean HEAD is required for a push receipt.' }
}
function Read-ChangedPaths([string]$Base) {
    return @(@(Read-Git @('-c', 'core.quotePath=false', 'diff', '--name-only', '--no-renames', $Base, '--')) +
        @(Read-Git @('-c', 'core.quotePath=false', 'ls-files', '--others', '--exclude-standard')) | Sort-Object -Unique)
}
function Get-ChangeHash([string[]]$Paths) {
    $rows = foreach ($path in $Paths) {
        $hash = if (Test-Path -LiteralPath $path -PathType Leaf) { (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash.ToLowerInvariant() } else { 'DELETED' }
        "$path`t$hash"
    }
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes(($rows -join "`n")))).ToLowerInvariant()
}
if ($Library) { return }

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $root
try {
    $solution = 'src/XN1Lab.XN1Finance.PortfolioAnalytics.Web/XN1Lab.XN1Finance.PortfolioAnalytics.sln'
    $project = 'src/XN1Lab.XN1Finance.PortfolioAnalytics.Web/XN1Lab.XN1Finance.PortfolioAnalytics.Web.csproj'
    $testProject = ''
    $head = [string](Read-Git @('rev-parse', 'HEAD'))
    if ($PrePush) {
        $target = Get-PushTarget ([Console]::In.ReadToEnd()) $head ([string](Read-Git @('branch', '--show-current')))
        $BaseRef = $target.BaseRef; $ExpectedHead = $target.ExpectedHead; $ForPush = $true
    }
    if ($BaseRef -cnotmatch '^(origin/(develop|main)|[0-9a-f]{40})$') { throw 'Base must be a fetched integration ref or exact remote SHA.' }
    $base = [string](Read-Git @('rev-parse', '--verify', "$BaseRef^{commit}"))
    Invoke-Checked git @('merge-base', '--is-ancestor', $base, $head)
    if ($ForPush) {
        if ($ExpectedHead -cnotmatch '^[0-9a-f]{40}$' -or $ExpectedHead -cne $head) { throw 'Missing/mismatching pushed SHA.' }
        Assert-CleanHead $head
    }
    $paths = @(Read-ChangedPaths $base)
    $scope = Get-ValidationScope $paths
    $changeHash = Get-ChangeHash $paths
    Invoke-Checked git @('diff', '--check', $base, '--')
    foreach ($required in @('README.md', '.github/CODEOWNERS', $solution, $project)) {
        if (!(Test-Path -LiteralPath $required -PathType Leaf)) { throw 'Required repository boundary file missing.' }
    }
    if ([IO.File]::ReadAllText((Join-Path $root '.github/CODEOWNERS')) -notmatch '(^|\s)@kursav(\s|$)') { throw 'Required repository owner is missing.' }
    $tracked = @(Read-Git @('ls-files'))
    # Mirror this repository's existing governance baseline; do not import a different product's file policy.
    if (@($tracked | Where-Object { $_ -match '(^|/)\.git($|/)' }).Count) { throw 'Tracked nested Git metadata refused.' }
    foreach ($workflow in Get-ChildItem -LiteralPath '.github/workflows' -File) {
        if ($workflow.Extension -notin @('.yml', '.yaml')) { throw 'Unexpected workflow file.' }
        Assert-WorkflowPolicy ([IO.File]::ReadAllText($workflow.FullName))
    }
    foreach ($script in Get-ChildItem -LiteralPath 'tools/ci' -Filter '*.ps1' -File) {
        $tokens = $null; $errors = $null
        $null = [Management.Automation.Language.Parser]::ParseFile($script.FullName, [ref]$tokens, [ref]$errors)
        if ($errors.Count) { throw 'Local validator PowerShell syntax error.' }
    }
    & "$PSScriptRoot/Test-LocalValidation.ps1"
    if (Test-Path -LiteralPath 'tests/Verify-DevelopmentBoundary.ps1') {
        & './tests/Verify-DevelopmentBoundary.ps1'
    }
    if ($scope -eq 'dotnet') {
        if ((& dotnet --version) -notmatch '^9\.0\.') { throw 'Select a .NET 9 SDK before local code validation.' }
        if (Test-Path -LiteralPath '.github/platform-revision.txt') {
            $pin = ([IO.File]::ReadAllText((Join-Path $root '.github/platform-revision.txt'))).Trim()
            if ($pin -cnotmatch '^[0-9a-f]{40}$') { throw 'Invalid Platform pin.' }
            [xml]$projectXml = [IO.File]::ReadAllText((Join-Path $root $project))
            $refs = @($projectXml.SelectNodes('//ProjectReference') | ForEach-Object { $_.Include.Replace('\', '/') } | Where-Object { $_ -match '/platform/' })
            if (!$refs.Count) { throw 'Cannot prove the actual Platform project-reference root.' }
            $projectDir = Split-Path -Parent (Join-Path $root $project)
            foreach ($reference in $refs) {
                $platform = [IO.Path]::GetFullPath((Join-Path $projectDir ($reference -replace '/platform/.*$', '/platform')))
                if ([string](Read-Git @('-C', $platform, 'rev-parse', 'HEAD')) -cne $pin -or @(Read-Git @('-C', $platform, 'status', '--porcelain')).Count) { throw 'Actual referenced Platform checkout is not clean at the exact pin; no automatic checkout/fetch.' }
            }
        }
        # Single application solution; no hosted runs, Docker build, publish, broker or deploy.
        Invoke-Checked dotnet @('restore', $solution)
        Invoke-Checked dotnet @('build', $solution, '-c', 'Release', '--no-restore', '-m:1', '/nodeReuse:false', '/p:UseSharedCompilation=false')
        if ($testProject) { Invoke-Checked dotnet @('test', $testProject, '-c', 'Release', '--no-build', '--no-restore', '-m:1', '/nodeReuse:false') }
    }
    if ([string](Read-Git @('rev-parse', 'HEAD')) -cne $head -or (Get-ChangeHash @(Read-ChangedPaths $base)) -cne $changeHash) { throw 'Inputs changed during validation.' }
    $receipt = [ordered]@{ schemaVersion = 1; task = 'ECODEV-94'; headSha = $head; baseSha = $base; changedInputsSha256 = $changeHash; changedPathCount = $paths.Count; scope = $scope; status = 'PASS'; applicationBuildRun = ($scope -eq 'dotnet'); repositoryTestsRun = ($scope -eq 'dotnet' -and [bool]$testProject); ecosystemFullSuiteRun = $false; artifactCertified = $false; productionAuthorized = $false; pushEligible = [bool]$ForPush; completedAtUtc = [DateTime]::UtcNow.ToString('o') }
    if ($ForPush) {
        Assert-CleanHead $head
        $receiptPath = [string](Read-Git @('rev-parse', '--git-path', 'ecodev94-local-validation.json'))
        $receiptText = $receipt | ConvertTo-Json -Compress
        [IO.File]::WriteAllText([IO.Path]::GetFullPath($receiptPath), $receiptText, [Text.UTF8Encoding]::new($false))
        if ([IO.File]::ReadAllText([IO.Path]::GetFullPath($receiptPath)) -cne $receiptText) { throw 'Receipt readback failed.' }
        Assert-CleanHead $head
    }
    $receipt | ConvertTo-Json -Compress
} finally { Pop-Location }
