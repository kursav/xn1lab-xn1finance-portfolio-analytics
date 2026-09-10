# ECODEV-94 main local-first CI policy gate

This main/default synchronization changes CI policy only. Automatic hosted runs
are restricted to develop/main. The development workflow is retained verbatim
from the reviewed ECODEV-94 source and triggers only on develop; existing main
publish/deploy bodies and authority gates are unchanged.

Run `pwsh -NoProfile -File tools/ci/Invoke-LocalValidation.ps1`.
The default base is origin/main for main, hotfix/* and release/*; other supported
task branches use origin/develop. A conflicting explicit base is refused.
Every pre-push revalidates the whole exact branch delta against that integration
base, including subsequent pushes, with one exact current-HEAD branch update.
Install the opt-in hook with `git config --local core.hooksPath .githooks`.
For an explicit push receipt, pass `-ForPush -ExpectedHead <full SHA>`.

This narrow main gate accepts only its CI/helper/hook/governance-policy paths.
Every other path (including application source, tests, dependencies, Docker,
arbitrary docs and unknown files) returns **LOCAL_FIXTURE_REQUIRED**. It does not
run or claim an application build, runtime fixture, ecosystem suite or production
approval. Main's existing solution/project boundary files are checked, but
develop-only test/runtime files are not copied or required. Appropriate
application fixtures require a separate reviewed change before those paths can
be admitted. No direct DB, broker, server, secret or runtime operations occur.

The self-test covers PR/topic/tag/schedule rejection, wrong hotfix/release base,
single exact-HEAD binding, and unknown/source/test paths failing closed.
