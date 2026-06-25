# Instructions for AI Coding Agents

This repository builds and publishes Docker images for Network Optix VMS products (Nx Witness, Nx Meta, Nx Go, DW Spectrum, Wisenet WAVE). It includes base images (nx-base, nx-base-lsio) and derived product images that use the base images, plus a .NET tooling project that generates Dockerfiles and build matrices and scripts/templates for packaging.

For comprehensive coding and formatting standards, follow:

- `CODESTYLE.md`
- `.editorconfig`
- `.husky/task-runner.json`
- `.vscode/tasks.json`

## Solution Structure

### Projects

- `CreateMatrix/CreateMatrix.csproj`
  - .NET 10 console app.
  - Fetches product release metadata and generates build inputs such as Dockerfiles and matrix data used by CI.
- `CreateMatrixTests/CreateMatrixTests.csproj`
  - xUnit v3 test project with AwesomeAssertions.

### Key Directories

- `Docker/`
  - Generated and static Dockerfiles for base images and product variants.
- `Make/`
  - Build orchestration scripts and test compose files.
- `Unraid/`
  - Unraid container templates.
- `version.json`, `Make/Version.json`, `Make/Matrix.json`
  - Version and matrix inputs consumed by the build pipeline.

## Build and Validation Workflow

- Primary developer entry points are the `CreateMatrix` CLI commands invoked directly or via the scripts in `Make/`:
  - `version --versionpath=./Make/Version.json`.
  - `matrix --versionpath=./Make/Version.json --matrixpath=./Make/Matrix.json --updateversion`.
  - `make --versionpath=./Make/Version.json --makedirectory=./Make --dockerdirectory=./Docker --versionlabel=Beta`.
- Formatting and style checks are enforced by Husky.Net and VS Code tasks.
- Required tasks are documented in `CODESTYLE.md` and `.husky/task-runner.json`.
- C# code should be formatted with CSharpier, then verified with `dotnet format` (style).
- The `.NET Format` VS Code task in `.vscode/tasks.json` must be clean and warning-free at all times.

### Workspace and linting

- VS Code settings, extension recommendations, and spell-check words live in the workspace file `NxWitness.code-workspace`; add new editor settings or recommended extensions there rather than in `.vscode/`. (Build/debug tasks still live in `.vscode/tasks.json` and `.vscode/launch.json`.) Open the workspace file in VS Code (not the folder) so its settings and recommendations apply.
- Linting is editor-only (no CI lint job); the extensions recommended in `NxWitness.code-workspace` cover the project's structured files: C# (Roslyn + CSharpier), Markdown, Dockerfiles/Compose, GitHub Actions workflows, and spelling. Lint changed files and clear reported problems before pushing.
- For workflow files, the GitHub Actions extension covers schema/expression checks in-editor; run the `actionlint` CLI for deeper checks (including shellcheck on `run:` steps).

## Image Architecture

- Base images (`nx-base`, `nx-base-lsio`) are built and pushed, then reused as `FROM` images for derived product Dockerfiles.
- Derived product images should stay aligned with the base image changes and tags (for example, the Ubuntu distro tag).

## CI Pipeline (GitHub Actions)

- Pull requests (`test-pull-request.yml`) run unit tests and code style, plus a fast smoke build only when image files (`Docker/**`, `Make/Matrix.json`, `Make/Version.json`) change. The smoke build (`build-docker-task.yml` with `smoke: true`) builds a representative subset (NxMeta and NxMeta-LSIO, amd64 only, no push), not the full matrix.
- Publishing happens only on a schedule or manual dispatch (`publish-release.yml`): it builds the base images once, then builds and pushes the full matrix for both the `main` and `develop` branches (`build-docker-task.yml` with `ref:` and `build_base: false`), and updates the GitHub release, Docker Hub readme, and date badge.
- Merges to `main`/`develop` do not build or publish images. Auto-merged Dependabot and codegen PRs simply land commits that the next scheduled publish picks up. Do not reintroduce push-triggered publishing or full-matrix PR builds.
- Lint workflow edits before pushing (see [Workspace and linting](#workspace-and-linting)); there is no CI lint job.

## Versioning

The `version` (major.minor) in [version.json](./version.json) is the NBGV version floor; NBGV appends the git height. **`develop` leads `main` by a minor:** after a `develop -> main` release lands and main's publish completes, bump the minor in `version.json` on `develop` in an isolated `bump-version-X.Y` PR (X.Y = the new minor), so develop's NBGV prerelease version stays numerically above main's last stable. A **maintenance** `develop -> main` promotion (dependency bumps, CI/doc fixes, template re-syncs) holds main's version - `git checkout main -- version.json` on the promotion branch - so `main` advances only its NBGV height, not its minor. (NBGV's version is the GitHub release tag on `main` and the `LABEL_VERSION` build arg baked into the images; the Docker image *tags* carry the Nx product version from `Make/Matrix.json` - see [CI Pipeline](#ci-pipeline-github-actions).)

## Git and Commit Rules

- **Default to staging, not committing.** Stage changes with `git add` and leave `git commit` to the developer unless the developer has explicitly authorized the agent to commit for the current ask ("commit this", "open a PR", etc.). Authorization is scope-bound - it covers the commits needed for that specific task, not a blanket commit license for the rest of the session.
- **All commits must be cryptographically signed (SSH or GPG).** Branch protection enforces this on both branches; unsigned commits are rejected on push. Signing depends on environment configuration - `git config commit.gpgsign true`, a configured `user.signingkey`, and a working signing agent (loaded `ssh-agent` for SSH, or `gpg-agent` for GPG). If signing is not configured in the environment, **do not commit** - surface the missing config to the developer and stop at `git add`. Verify before any agent-authored commit (`git config --get commit.gpgsign && ssh-add -L` or the GPG equivalent). **Signing must be live before the *first* commit, not retrofitted.** Turning on `Require signed commits` against a branch that already has unsigned commits forces a rewrite of that entire history to re-sign it - changing every commit SHA and making whoever does the rewrite the committer and signer of every commit (a rebase preserves the `author` field but not the original signatures; you cannot sign another contributor's commits for them). During new-repo setup, never create commits until signing is verified.
- **Never force push.** Do not run `git push --force` or `git push --force-with-lease` under any circumstances. Force pushing rewrites shared history and can cause data loss.
- **Never run destructive git commands** (`git reset --hard`, `git checkout .`, `git restore .`, `git clean -f`) without explicit developer instruction.

## Pull Request Title and Commit Message Conventions

### Format

- Imperative subject summarizing the change, <=72 characters, no trailing period. ("Add 24-hour PM2.5 average sensor", not "Added X" or "Adds X".)
- Optional body, blank-line separated, explaining *why* the change is being made when that's non-obvious. The diff shows *what*.

### Rules

- Don't write `update stuff`, `wip`, or other vague titles. (Dependabot's default `Bump X from Y to Z` titles are fine - keep them.)
- Don't add `Co-Authored-By:` lines unless the developer explicitly asks.
- Don't put release-bump magnitude in the title - no "minor", "patch", "release v0.2.0", etc. Nerdbank.GitVersioning computes the next release version from `version.json` + git history. Dependency versions in dependency-bump titles are fine and expected.
- Use US English spelling and match the existing heading style of the file you're editing: title case with lowercase short bind words (a, an, the, and, but, or, of, in, on, at, to, by, for, from); hyphenated compounds capitalize both parts unless the second is a short preposition (*Built-in*, *EPA-Corrected*, *24-Hour*).

### Examples

```text
Add structured logging extensions to library
Pin softprops/action-gh-release to commit SHA
Drop net8.0 multi-targeting from console project
Bump xunit.v3 from 3.2.2 to 3.3.0
Clarify devcontainer setup steps in README
```

## PR Review Etiquette

> **Mandatory in every derived repo.** This entire "PR Review Etiquette" section is the provider-agnostic review-loop *contract* and must be carried **verbatim** into every repo derived from this template, alongside the [`.github/copilot-instructions.md`](./.github/copilot-instructions.md) "GitHub Copilot Review Runbook" that implements it. Without both in-repo, an agent working in the derived repo has no pointer to the reliable Copilot mechanics and falls back to ad-hoc (and known-broken) behavior.

The repo runs a review loop on every PR: local agent iteration plus remote automated review (GitHub Copilot is the configured reviewer). Treat this as a contract regardless of which local agent authored the changes.

### Merge Gate (read this first)

**Do not merge - and do not enable auto-merge - unless ALL of these hold:**

1. Required status checks are green (`mergeStateStatus: CLEAN`), **and**
2. A Copilot review is confirmed on the **current head SHA** (not an earlier push), **and**
3. **Every** Copilot finding on that head SHA is closed out - all review threads resolved, **and** any issue-level Copilot comments (which have no resolve action) triaged and replied to - so zero outstanding findings remain, **and**
4. The maintainer has given **explicit** permission to merge.

`mergeStateStatus: CLEAN` reflects **only** required statuses - it never reflects open bot review comments, so `CLEAN` alone is **never** sufficient to merge. A green/`CLEAN` PR with an unresolved Copilot finding fails this gate; treat it as "not mergeable" no matter what the merge-state field says. The agent never merges on its own (consistent with "default to staging"; merging is maintainer-authorized).

**Merging is not releasing.** A merge to a release branch does **not** by itself publish; publishing is a separate step in the repo's release pipeline (a scheduled run or a manual dispatch), not an automatic consequence of merging. Never describe a merge as cutting a release, and never trigger a publish without explicit maintainer instruction.

### Expected Review Loop

1. Push changes to the PR branch.
2. Re-request a review for the **current head SHA**. Auto-trigger is unreliable, so request it explicitly via the `requestReviews` GraphQL mutation (now reliable end-to-end - see the runbook); the UI is only a fallback.
3. Wait for review activity on that head. A completed review that raises **no findings** is a valid terminal outcome for that head - proceed; do not re-trigger it or treat the absence of comments as a missing review.
4. Triage findings.
5. Apply fixes or write a rationale for declines.
6. Reply to each thread and resolve what was addressed.
7. Re-run the loop after every fix push until no actionable findings remain.

Drive the loop to green - review confirmed on the latest head SHA and every actionable finding closed - then stop and apply the **Merge Gate** above: all four preconditions must hold, and `mergeStateStatus: CLEAN` alone never satisfies it.

For provider-specific mechanics (how to request review, query review state, post replies, resolve threads), see the **GitHub Copilot Review Runbook** in [.github/copilot-instructions.md](./.github/copilot-instructions.md). This file owns the contract; that file owns the mechanics.

### Triaging Review Comments

For each comment, classify before responding:

- **Bug** - wrong behavior, missing test coverage, or a real divergence between code and docs. Fix it. Reply with the fixing commit SHA when done.
- **Style/convention** - the comment cites a rule from this file or a language-specific style guide. Two cases:
  - The cited rule matches what the existing codebase already does -> fix the offending code.
  - The cited rule contradicts what's in the tree, or industry norm -> **update the rule instead of the code**. The rule is wrong, not the code. Bouncing the same code across rounds is the symptom of a wrong rule. Heuristic: three rounds on the same style category means the rule needs adjusting and the user should authorize the rule change.
- **Architectural opinion** - the comment proposes a different design ("constrain this to disabled-by-default", "move it elsewhere", "add a runtime guardrail"). This is judgment, not a bug. Surface it to the user with a recommendation; don't apply unilaterally.

### Responding and Resolution Expectations

Reply inline with either the fixing commit SHA (for accepted issues) or a concise rationale (for declines). Resolve review threads when addressed or intentionally declined with rationale. Issue-level comments (those at `repos/.../issues/<N>/comments` rather than tied to a specific line) have no resolution action - acknowledge with a reply if needed and move on.

After the final push on a PR, sweep older threads from earlier rounds whose code paths no longer exist; otherwise stale unresolved markers remain in the review UI.

### Escalating to the User

Bring the user in when:

- **Genuine design trade-off** surfaces (fail-open vs fail-closed, narrow vs broad refactor scope, "should we add a guardrail or trust the docstring"). Triage, recommend, ask.
- **Repeated friction** across rounds without convergence - that's the rule-needs-updating signal. Stop, summarize the pattern, and let the user authorize the rule change.
- **Architectural redesign** is requested rather than a bug fix. Surface with a recommendation; never apply unilaterally.

Anti-pattern: don't keep flipping the code on the same style point. Flip the rule once and stick to the rule.

## Coding Conventions (Highlights)

- Do not use `var`; use explicit types.
- File-scoped namespaces, Allman braces, and modern C# features are preferred.
- No `#region` usage.
- UTF-8 encoding without BOM.
- Respect line ending rules in `.editorconfig`.

## Notes for Changes

- When modifying Dockerfiles or build scripts, ensure generated outputs stay in sync with `CreateMatrix` behavior.
- Keep base image definitions and derived image Dockerfiles aligned, since derived images build on the base images.
- Keep `README.md` and release documentation aligned with build outputs and product variants.

## Template adaptations

This repo derives its CI and conventions from [ptr727/ProjectTemplate](https://github.com/ptr727/ProjectTemplate). Carried artifacts are taken by full-file replacement; the deliberate deviations below are documented so they are not mistaken for drift.

- **Base + per-branch Docker build structure.** `publish-release.yml` keeps a `build-base` job plus separate `build-main` / `build-develop` legs (calling repo-owned `build-base-images-task.yml` and `build-docker-task.yml`) instead of the template's single per-target branch matrix. This is a multi-image Docker product: the shared `nx-base` / `nx-base-lsio` images are built once from `main` and reused by both branch legs (`build_base: false`), and each leg builds every product image (NxMeta, DWSpectrum, NxGo, WisenetWAVE, ...) from `Matrix.json`. The template branch matrix cannot express the shared-base fan-out, so the build layer stays repo-owned.
- **Docker-only GitHub release (no `release-asset-*` files).** The `github-release` job follows the template's generic release semantics (tag on the built commit, auto source zip + README + LICENSE, `target_commitish` pinned, skip-existing guard, main-only `Verify public release version` backstop), but this repo ships no binary/package release assets - the published artifacts are the Docker Hub images. So there is no `release-asset-*` download step and `fail_on_unmatched_files` is omitted (it has no files to guard).
- **Docker Hub readme repositories derived from `Matrix.json`.** `publish-docker-readme-task.yml` is carried verbatim (generic: `repositories` or `manifest` + `manifest-jq` input, `ref` gate, optional transform). Because the image set is repo-specific, the `publish-release.yml` caller passes `manifest: ./Make/Matrix.json` plus the `manifest-jq` program (lowercased `ptr727/<image>` plus the shared base repos) and lets the task's own `get-repos` job resolve the list.
- **Husky.Net pre-commit hooks.** This repo installs Husky.Net Git hooks (pre-commit formatting/codegen), inverting the template's no-hooks default. The hooks run the same checks CI enforces, surfaced earlier.
- **.vscode Benchmark -> Husky.Net Run task.** The carried `.vscode/tasks.json` swaps the template's Benchmark task for a Husky.Net Run task, matching this repo's hook tooling.
- **Build-layer leaves own build specifics but follow the shared action-pin + cache rules.** The build-layer leaves (`build-docker-task.yml`, `build-base-images-task.yml`, `test-release-task.yml`) own their per-image Dockerfiles, build args, and target matrix, but their actions are **SHA-pinned** like the orchestration layer (Dependabot still bumps SHA pins, updating the SHA + version comment), and their Docker layer cache uses **registry-tag caches** (`docker.io/ptr727/<repo>:buildcache-<branch>`, plus the base image's own tag and inline cache) rather than `type=gha`, per the template's cache policy.
- **No `merge-upstream-version` merge-bot job.** This repo tracks the upstream NX version through codegen (`run-codegen-pull-request-task.yml` updating `Matrix.json`), so the merge-bot keeps `merge-codegen` and omits the template's `merge-upstream-version` job (it uses the separate `check-upstream-version-task.yml` mechanism this repo does not ship).
