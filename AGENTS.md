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

## Image Architecture

- Base images (`nx-base`, `nx-base-lsio`) are built and pushed, then reused as `FROM` images for derived product Dockerfiles.
- Derived product images should stay aligned with the base image changes and tags (for example, the Ubuntu distro tag).

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
