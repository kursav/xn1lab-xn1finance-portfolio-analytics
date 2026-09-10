# ECODEV-94 local-first validation

Hosted workflows run only on their existing exact integration push branch (develop or main). There are no PR, feature-push, schedule or workflow-run triggers. Existing publish/deploy jobs, permissions and production authority are unchanged. This branch does not change GitHub required-check settings or fabricate PR check success; the owner must reconcile the integration required-check policy before merging.

Run from the repository root with PowerShell 7:

```powershell
pwsh -NoProfile -File tools/ci/Invoke-LocalValidation.ps1 -BaseRef origin/develop
```

Fetch integration refs explicitly before branching/validation. The validator never fetches, checks out dependencies, starts hosted jobs, containers or deployment. Deleted, tracked and untracked changed paths are considered. Docs/CI-only changes run policy, syntax, self-tests and existing boundary scripts without .NET restore/build. Other changes run the actual src/XN1Lab.XN1Finance.PortfolioAnalytics.Web/XN1Lab.XN1Finance.PortfolioAnalytics.sln .NET 9 build; this repository currently has no xUnit test project, so its existing development-boundary script is the test gate. Unknown file types escalate to that application build. Container/image/runtime and full ecosystem tests are NOT certified. Local NuGet restore may access configured package sources.

If a Platform pin exists, code validation refuses unless the actual relative ProjectReference checkout is clean at that pin. It never changes shared Platform state; coordinate its owner instead. Select a .NET 9 SDK for code validation.

Opt in per clone only after reviewing the hook and checking any existing hooks:

```sh
git config --local core.hooksPath .githooks
```

Installation is not automatic and must not overwrite an existing hook policy without owner coordination. Hook validation rejects non-HEAD, tag, deletion, cross-branch and multi-ref pushes. New topic refs use fetched origin/develop; updates use the exact remote SHA supplied by Git. A clean HEAD and unchanged inputs are rechecked after validation. The successful exact-head receipt is written and read back at Git's `ecodev94-local-validation.json` path. It is local evidence, not an independent approval or an unbypassable server gate. Any failure blocks the opt-in hook. No receipt grants production authority.
