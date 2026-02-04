# GitHub Copilot Guidance

## Purpose
This file summarizes the solution and defines the hierarchy of guidance for AI-assisted contributions.

## Guidance Hierarchy (Must Follow)
1. [CODESTYLE.md](../CODESTYLE.md) is the master code style and formatting authority.
2. [AGENTS.md](../AGENTS.md) is secondary guidance describing the solution, workflows, and conventions.
3. Repository configuration files such as [`.editorconfig`](../.editorconfig) and [`.vscode/tasks.json`](../.vscode/tasks.json) define enforced formatting, line endings, and task expectations.

If any instruction conflicts, follow CODESTYLE.md first, then AGENTS.md.

## Solution Summary
This repository builds and publishes Docker images for Network Optix VMS products (Nx Witness, Nx Meta, Nx Go, DW Spectrum, Wisenet WAVE). It includes a .NET tooling project that generates Dockerfiles, matrices, and version inputs used by CI and packaging scripts.

### Core Projects
- `CreateMatrix` (.NET 10 console app): Generates Dockerfiles and build matrix data using version and release metadata.
- `CreateMatrixTests` (xUnit v3 + AwesomeAssertions): Validates release handling and version forwarding.

### Key Inputs and Outputs
- Inputs: version and matrix data in `version.json`, [Make/Version.json](../Make/Version.json), and [Make/Matrix.json](../Make/Matrix.json).
- Outputs: Dockerfiles in [Docker/](../Docker/) and compose/test artifacts in [Make/](../Make/).
- Templates: Unraid container templates in [Unraid/](../Unraid/).

### Build and Validation Workflow (High Level)
- The primary entry points are VS Code launch configurations that run CreateMatrix commands to generate versions, matrix data, schemas, and Docker/compose files (see [`.vscode/launch.json`](../.vscode/launch.json)).
- Formatting and style verification are enforced by CSharpier and dotnet format, with Husky.Net hooks.
- The `.Net Format` VS Code task in [`.vscode/tasks.json`](../.vscode/tasks.json) must be clean and warning-free at all times.

## What to Keep in Sync
- Generated Dockerfiles and scripts must reflect CreateMatrix behavior.
- Documentation in [README.md](../README.md) and release notes should align with current outputs and supported product variants.

## Expectations for Changes
- Follow the zero-warnings policy and formatting requirements in [CODESTYLE.md](../CODESTYLE.md).
- Use explicit types (no `var`), Allman braces, file-scoped namespaces, and other conventions as defined in the master style guide.
- Respect line endings and encoding rules from the repository configuration, including UTF-8 without BOM.
