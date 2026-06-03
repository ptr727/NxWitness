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

## Pull Request Review Process

- Open PRs against `develop` (the integration branch); `develop` is forward-only and ships to `main` via release merges.
- The repo is configured to automatically request a GitHub Copilot review when a PR is opened. Respond to every Copilot comment: either address it with a change, or justify why it does not apply. Either way, reply on the comment stating what you did, then resolve (close) the comment.
- After you push a new commit, a Copilot re-review does not reliably fire on its own. In practice the re-review can be requested via the GraphQL `requestReviews` mutation, passing the Copilot bot's node id in `botIds`:
  - Get the bot id once from the existing review author: `pullRequest { reviews(first:1){ nodes { author { ... on Bot { id } } } } }` (the Copilot reviewer login is `copilot-pull-request-reviewer`).
  - Trigger: `mutation { requestReviews(input: {pullRequestId: "<PR node id>", botIds: ["<bot id>"], union: true}) { pullRequest { id } } }`.
  - This is observed-but-not-guaranteed; if a re-review still does not appear, ask the maintainer to start one in the GitHub UI. Repeat until both Copilot and the author are satisfied.

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
