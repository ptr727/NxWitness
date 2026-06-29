# Instructions for AI Coding Agents

This repository builds and publishes Docker images for Network Optix VMS products (Nx Witness, Nx Meta, Nx Go, DW Spectrum, Wisenet WAVE). It includes base images (nx-base, nx-base-lsio) and derived product images that use the base images, plus a .NET tooling project (`CreateMatrix`) that generates Dockerfiles and build matrices and scripts/templates for packaging. There is **no NuGet publish**: the .NET project is a build-time matrix generator only; the published artifacts are exclusively the Docker Hub images.

This file is the canonical reference for cross-cutting AI-agent rules. The CI/CD workflow contract and conventions live in [`WORKFLOW.md`](./WORKFLOW.md); the repository configuration-as-code (branch rulesets, settings, required secrets) lives in [`repo-config/`](./repo-config/); C# code-style conventions live in [`CODESTYLE.md`](./CODESTYLE.md). Copilot review *mechanics* are owned by [`.github/copilot-instructions.md`](./.github/copilot-instructions.md) - this file delegates them there explicitly (see "PR Review Etiquette" below).

**Where rules live.** A durable project, code, or style rule belongs in this file (or `WORKFLOW.md` / `CODESTYLE.md` as appropriate), so it is versioned and read by every session and every agent. An agent's own session memory or scratch state is private and lost on restart, so it is never the system of record for a rule: when you learn or are corrected on a rule, write it into the right doc in the same change. Memory may also note it, but the committed docs are the source of truth.

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

The full CI/CD contract - triggers, jobs, the one-branch publish model, versioning, and the multi-image build layer - is specified in [`WORKFLOW.md`](./WORKFLOW.md), the canonical guide. The summary below is a pointer; do not duplicate those rules.

- CI runs on **push to every branch** (`test-pull-request.yml`): it validates (`validate-task.yml`: Husky lint + `dotnet test`) on every push, and runs a fast smoke build (`build-docker-task.yml` with `smoke: true` - NxMeta + NxMeta-LSIO, amd64, no push) only when image files (`Docker/**`, `Make/Matrix.json`, `Make/Version.json`) change, via an inline `git diff` change-gate. One aggregator job, `Check pull request workflow status job`, is the ruleset-bound required check.
- Publishing is **triggered-Docker, one branch per run** (`publish-release.yml`): triggers are the weekly schedule (rebuilds `main` only), a path-scoped push to `main` on `Make/Matrix.json` (publishes a new codegen product pin at once), and manual dispatch (publishes the started-from branch). One run computes the version once (`get-version-task.yml`), builds the shared base once (main only; develop reuses it with `build_base: false`), builds the full product matrix from `Make/Matrix.json`, and on `main` cuts the GitHub release and pushes the Docker Hub overviews.
- Merges to `main`/`develop` do not build or publish images by themselves; only the matrix-pin push (main), a schedule, or a dispatch publishes. Auto-merged Dependabot and codegen PRs land commits the next publish picks up. Do not reintroduce a two-branch publish matrix, a nested `get-version` in the build task, the date-badge or standalone docker-readme workflows, or `dorny/paths-filter`.
- Lint workflow edits before pushing (see [Workspace and linting](#workspace-and-linting)); `validate-task.yml` runs the unit tests + Husky style checks in CI.

## Versioning

The `version` (major.minor) in [version.json](./version.json) is the NBGV version floor; NBGV appends the git height. **`develop` leads `main` by a minor:** after a `develop -> main` release lands and main's publish completes, bump the minor in `version.json` on `develop` in an isolated `bump-version-X.Y` PR (X.Y = the new minor), so develop's NBGV prerelease version stays numerically above main's last stable. A **maintenance** `develop -> main` promotion (dependency bumps, CI/doc fixes, template re-syncs) holds main's version - `git checkout main -- version.json` on the promotion branch - so `main` advances only its NBGV height, not its minor. (NBGV's version is the GitHub release tag on `main` and the `LABEL_VERSION` build arg baked into the images; the Docker image *tags* carry the Nx product version from `Make/Matrix.json` - see [CI Pipeline](#ci-pipeline-github-actions).)

- A significant one-time overhaul of the build/release process (such as the branch-scoped CI/CD migration) is a deliberate maintainer-directed floor bump in the PR that introduces it, distinct from the routine cadence above; routine dependency, CI/workflow, and doc edits leave `version.json` untouched.
- **`dotnet/nbgv` is consumed via `@master`, never SHA-pinned.** Its tag stream lags `master` such that Dependabot tag-tracking would only propose downgrades to stale tags; this is the sole [`WORKFLOW.md`](./WORKFLOW.md) D9.1 action-pinning exception (rationale inline in `get-version-task.yml`). Do not SHA-pin it.

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

## Documentation Style Conventions

### Markdown

- Use reference-style links for any URL referenced more than once or appearing in lists; alphabetize the reference definitions block.
- Inline single-use relative links (e.g. `[CODESTYLE.md](./CODESTYLE.md)`) are fine.
- One logical paragraph per line; no hard-wrap line-length limit. For an intentional hard line break within a block - stacked badges, status, or license lines - end the line with a trailing backslash (`\`); this explicit form is preferred over trailing whitespace and is not treated as a paragraph split.
- Headings follow the title-case-with-short-bind-words rule from the PR-title section.
- **Write docs in the current state, not as a change from a prior one.** The reader has no memory of the previous behavior, so describe what *is*: "X does Y", never "X *now* does Y", "X *no longer* does Z", or "changed/switched/restored to Y". Before/after framing belongs in changelogs, commit messages, and PR descriptions - not in `README.md` or other living docs.

### Comments

Applies to code and workflow (`#`) comments alike.

- Comment only when the code is non-obvious or important. Self-evident code needs no comment.
- Judge "obvious" in context, not line by line. A note that reads as redundant on its own line can be essential in the larger flow - a comment marking a workflow step's exit condition, for example, even though the line itself plainly does a `return` or `exit`.
- State the non-obvious *why*, not what the code already shows. No cross-project references (do not name other repos), no historic or design narrative, no rule citations - governance lives in this file, not echoed inline.
- **One line if it fits in ~120 columns.** Do not wrap a comment at 75-80 columns; a short two-line comment that would fit on one line looks sloppy - collapse it. Go multi-line only when the content genuinely exceeds ~120, filling each line rather than narrow-wrapping. For a multi-point comment, prefer short structured lines or `-` bullets over one prose paragraph.
- **Workflows: prefer one short summary description at the top of the file** over scattering rationale across steps; comment an individual step only when its purpose is non-obvious.
- **Do not accumulate comments.** When you change code or a comment, rewrite the whole comment fresh; never bolt a new comment onto an existing one or layer explanations across edits. Comment volume should stay flat or shrink over time, not grow.
- **Leave human-authored comments and emojis exactly as written** - do not reword, trim, reflow, or "clean" them, even if they seem to bend a rule. Revise only agent-authored comments, and match the surrounding voice when you do.

### Character Set

- **Write ASCII in all agent-authored text** - documentation, code, comments, commit messages, and PR descriptions. The agent does not introduce non-ASCII characters. Replace typographic Unicode with its ASCII equivalent on sight:
  - em dash (U+2014) and en dash (U+2013) -> hyphen `-` (use a spaced ` - ` for an em-dash-style clause break)
  - right arrow (U+2192) -> `->`; double arrow (U+21D2) -> `=>`
  - less-than-or-equal (U+2264) -> `<=`; greater-than-or-equal (U+2265) -> `>=`
  - curly quotes (U+2018/U+2019/U+201C/U+201D) -> straight `'` and `"`; ellipsis (U+2026) -> `...`
- **Allowed non-ASCII (two narrow exceptions):**
  - **Scientific or technical symbols with no clean ASCII equivalent** - e.g. ohm, micro, degree, pi. Keep the symbol; do not approximate it away.
  - **Unicode the developer deliberately typed** - emoji used for emphasis or as callout markers (for example the warning/info markers a maintainer placed in `README.md`). Preserve it; never strip the developer's own characters. This carve-out is for developer-authored text, not a license for the agent to add emoji.

### Line Endings

- [`.editorconfig`](./.editorconfig) defines the correct ending per file type (CRLF for `.md`, `.cs`, XML/`.csproj`/`.props`, `.yml`/`.yaml`, `.json`, `.slnx`, `.cmd`/`.bat`/`.ps1`; LF for `.sh`, `Dockerfile`), and [`.gitattributes`](./.gitattributes) stops git from normalizing.
- **Editing an existing file: preserve its current line endings** - do not reflow them as a side effect of a content change, even if the file is already non-compliant. After any programmatic edit, verify with `git diff --stat` (only changed lines) and `grep -c $'\r'` (CRLF count), since `file` does not report CRLF for JSON. Bring a non-compliant file to its `.editorconfig` ending only as a deliberate, isolated EOL-only change.

### Quantitative Claims

- Any quantitative claim in `README.md` (counts, sizes, version floors, supported products) must be verified against current code/config. If a doc number is derived from a code or matrix constant, mark the dependency in a source-code comment so the next editor knows to update both.

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

**Merging is not releasing.** A merge to a release branch does **not** by itself publish; publishing is a separate, explicitly configured step in the repo's release pipeline (e.g. a scheduled run, a manual dispatch, or an opted-in publish-on-merge trigger), not an automatic consequence of merging. Never describe a merge as cutting a release, and never trigger a publish without explicit maintainer instruction.

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

- **Triggered-Docker publisher, one branch per run.** `publish-release.yml` is `workflow_dispatch` + weekly `schedule` (main only) + a path-scoped `push` to `main` on `Make/Matrix.json` (publishes a new codegen product pin at once). It builds exactly one branch - the trigger ref (`github.ref_name`) - so NBGV classifies natively with no cross-branch leg and no `IGNORE_GITHUB_REF`. The jobs are a single `get-version` -> `build-base` (main only) -> `build-docker` -> `github-release` (main only) -> `docker-readme` (main only) -> `cleanup-artifacts` chain. A develop dispatch refreshes the `:develop` images only (no GitHub release); the earlier two-leg `build-main` / `build-develop` combined run is removed.
- **Multi-image, shared-base build layer.** This is a multi-image Docker product: the shared `nx-base` / `nx-base-lsio` images are built once on the `main` run and reused by a develop dispatch (`build_base: false`, so it never overwrites the branch-agnostic `nx-base` tag), and `build-docker-task.yml` builds every product image (NxMeta, DWSpectrum, NxGo, WisenetWAVE, ...) from `Make/Matrix.json` (`max-parallel: 4`). The template's single-target branch matrix cannot express the shared-base fan-out, so the build layer stays repo-owned.
- **Single NBGV run threaded to the build task.** `build-docker-task.yml` has no nested `get-version`; the orchestrator's single `get-version` run threads `semver2` down as the image `LABEL_VERSION`, so one classification feeds every product leg (no second NBGV run can reclassify or collide a tag).
- **Docker-only GitHub release (no `release-asset-*` files).** The `github-release` job follows the template's generic release semantics (tag on the built commit, auto source zip + README + LICENSE, `target_commitish` pinned, skip-existing guard, main-only `Verify public release version` backstop), but this repo ships no binary/package release assets - the published artifacts are the Docker Hub images. So there is no `release-asset-*` download step and `fail_on_unmatched_files` is omitted (it has no files to guard).
- **Folded Docker Hub readme.** The standalone docker-readme task is removed; `publish-release.yml` carries `docker-readme-repos` + `docker-readme` jobs gated to `main` that derive the repository list inline from `Make/Matrix.json` (lowercased `ptr727/<image>` plus the shared base repos) and matrix `peter-evans/dockerhub-description` over it.
- **Dropped date badge.** The `build-datebadge-task.yml` workflow and its publisher job are removed, along with the README "Last Build" badge that pointed at the BYOB gist.
- **Husky.Net pre-commit hooks.** This repo installs Husky.Net Git hooks (pre-commit formatting/codegen), inverting the template's no-hooks default. The hooks run the same checks CI enforces, surfaced earlier. `validate-task.yml` (the rename of `test-release-task.yml`) runs the same Husky lint + `dotnet test` in CI as the required-check's quality gate.
- **.vscode Benchmark -> Husky.Net Run task.** The carried `.vscode/tasks.json` swaps the template's Benchmark task for a Husky.Net Run task, matching this repo's hook tooling.
- **Build-layer leaves own build specifics but follow the shared action-pin + cache rules.** The build-layer leaves (`build-docker-task.yml`, `build-base-images-task.yml`) own their per-image Dockerfiles, build args, and target matrix, but their actions are **SHA-pinned** like the orchestration layer (Dependabot still bumps SHA pins, updating the SHA + version comment), and their Docker layer cache uses **registry-tag caches** (`docker.io/ptr727/<repo>:buildcache-<branch>`, plus the base image's own tag and inline cache) rather than `type=gha`, per the template's cache policy.
- **No `merge-upstream-version` merge-bot job.** This repo tracks the upstream NX version through codegen (`run-codegen-pull-request-task.yml` updating `Make/Version.json` + `Make/Matrix.json`), so the merge-bot keeps `merge-codegen` and omits the template's `merge-upstream-version` job (it uses the separate `check-upstream-version-task.yml` mechanism this repo does not ship).
