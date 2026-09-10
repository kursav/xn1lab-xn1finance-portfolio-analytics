$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
# Library import declares pure helpers; no Git, filesystem scan, build or receipt.
. "$PSScriptRoot/Invoke-LocalValidation.ps1" -Library
function Reject([scriptblock]$Action) { try { & $Action; throw 'UNEXPECTED_PASS' } catch { if ($_.Exception.Message -eq 'UNEXPECTED_PASS') { throw } } }
$valid = "name: x`non:`n  push:`n    branches: [develop]`njobs:`n  x: {}"
Assert-WorkflowPolicy $valid
Assert-WorkflowPolicy ($valid.Replace('    branches: [develop]', "    branches:`n      - main`n      - develop"))
foreach ($bad in @($valid.Replace('  push:', '  pull_request:'), $valid.Replace('[develop]', '[feature/*]'),
    $valid.Replace('[develop]', '[develop, develop]'), $valid.Replace('[develop]', '[main, feature]'),
    $valid.Replace('  push:', "  schedule:`n    - cron: '* * * * *'"), $valid.Replace('on:', '"on":'),
    $valid.Replace('  push:', "  workflow_run:`n  push:"), $valid.Replace('  push:', '  push: &alias'))) {
    Reject { Assert-WorkflowPolicy $bad }
}
foreach ($path in @('src/App.razor','src/deleted.cs','tests/ChangedTests.cs','.github/platform-revision.txt','Directory.Build.props','Dockerfile','docs/authority.json')) {
    if ((Get-ValidationScope @($path)) -cne 'LOCAL_FIXTURE_REQUIRED') { throw 'Non-policy change must require a separately approved local fixture.' }
}
if ((Get-ValidationScope @('.github/workflows/a.yml', '.githooks/pre-push')) -cne 'policy-only') { throw 'Lightweight scope misclassified.' }
if ((Get-ValidationScope @()) -cne 'no-change') { throw 'Empty scope misclassified.' }
Reject { Get-ValidationScope @('../outside') }
Reject { Get-ValidationScope @('src/app.cs', '../outside') }
Reject { Assert-WorkflowPolicy ("on: {pull_request: {}}`n" + $valid) }
Reject { Assert-WorkflowPolicy ($valid.Replace('    branches: [develop]', '    branches:')) }
$head = '1' * 40; $zero = '0' * 40; $branch = 'ci/ECODEV-94/local-first-delivery'
$row = "refs/heads/$branch $head refs/heads/$branch $zero"
if ((Get-PushTarget $row $head $branch).BaseRef -cne 'origin/develop') { throw 'New branch base mismatch.' }
foreach ($bad in @("$row`n$row", $row.Replace($head, ('2' * 40)), $row.Replace("refs/heads/$branch", 'refs/tags/v1'), $row.Replace(" $head ", " $zero "), $row.Replace("refs/heads/$branch $zero", "refs/heads/main $zero"))) {
    Reject { Get-PushTarget $bad $head $branch }
}
foreach ($taskBranch in @('hotfix/ECODEV-94/local-first-main', 'release/ECODEV-94/production-policy')) {
    if ((Get-BranchBase $taskBranch) -cne 'origin/main') { throw 'Hotfix/release base regression.' }
    $taskRow = "refs/heads/$taskBranch $head refs/heads/$taskBranch $zero"
    if ((Get-PushTarget $taskRow $head $taskBranch).BaseRef -cne 'origin/main') { throw 'Pre-push main base mismatch.' }
    if ((Get-PushTarget ($taskRow.Replace($zero, ('2' * 40))) $head $taskBranch).BaseRef -cne 'origin/main') { throw 'Existing topic update must retain integration base.' }
    Reject { Resolve-BranchBase $taskBranch 'origin/develop' }
}
if ((Resolve-BranchBase 'main' '') -cne 'origin/main') { throw 'Main default mismatch.' }
if ((Resolve-BranchBase 'develop' '') -cne 'origin/develop') { throw 'Develop default mismatch.' }
foreach ($bad in @('', 'hotfix/ECODEV-94/../x', 'hotfixfoo/ECODEV-94/x', 'codex/x')) { Reject { Get-BranchBase $bad } }
foreach ($path in @('README.md', 'docs/guide.md', 'unknown.json', 'tools/other.ps1', 'src/app.md', 'Dockerfile', 'tests/new.cs')) {
    if ((Get-ValidationScope @($path)) -cne 'LOCAL_FIXTURE_REQUIRED') { throw 'Unknown/non-policy path falsely admitted.' }
}
Write-Output 'ECODEV-94 local policy self-tests passed; no stack build was run by these tests.'
