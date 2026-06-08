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
- The `.Net Format` VS Code task in `.vscode/tasks.json` must be clean and warning-free at all times.

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

## PR Review Etiquette

The repo runs a review loop on every PR: local agent iteration plus remote automated review (GitHub Copilot is the configured reviewer). Treat this as a contract regardless of which local agent authored the changes.

### Expected Review Loop

1. Push changes to the PR branch.
2. Re-request a review for the **current head SHA**. Auto-trigger is unreliable, so request it explicitly via the `requestReviews` GraphQL mutation (now reliable end-to-end - see the runbook); the UI is only a fallback.
3. Wait for review activity on that head.
4. Triage findings.
5. Apply fixes or write a rationale for declines.
6. Reply to each thread and resolve what was addressed.
7. Re-run the loop after every fix push until no actionable findings remain.

`mergeStateStatus: CLEAN` only checks required statuses; it does not block on bot review comments. Drive the loop to green - review confirmed on the latest head SHA and every actionable finding closed - and then **wait for the maintainer's explicit permission to merge**. The agent does not merge on its own (consistent with "default to staging"; merging is maintainer-authorized).

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
