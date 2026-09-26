# Code Style and Formatting Rules

This is the single code-style guide for the fleet. The **General** section applies to every language. Each **language section** (.NET, Python, Shell) is self-contained: a repo follows only the section(s) for the languages it ships and ignores the rest. A repo keeps the whole file rather than trimming it. An unused-language section costs nothing, the same whole-file model as [`.editorconfig`][root], whose inert `[*.cs]` block a non-.NET repo keeps.

Cross-cutting *process* rules (PR titles, branching, US English, Markdown style, comments philosophy, workflow YAML, PR review etiquette, and the verification discipline that defines the pre-push lint gate) live in [GOVERNANCE.md][governance] and are not repeated here.

## General

These rules apply to every language in the repo.

### Tooling Names and Casing

Use each tool's official casing in task labels, docs, and prose, per the `comment-and-doc-style` Skill at `.agents/skills/comment-and-doc-style/SKILL.md` in the hub (not a repo-relative link, that path is hub-local and not carried into every fleet repo).

### Clean-Compile Verification

Each language defines a **clean-compile** verification: the combination of build, formatter, linter, and code-analysis tools that must report clean before a commit. It is exposed as one or more **named** VS Code tasks (or, where a language ships no tasks, documented commands), and those definitions are the same across the fleet. The concrete names live in each language section below.

- **Run it after every code change, and it is not the whole gate.** The relevant language's clean-compile must pass before you commit. CI runs those same language checks as a backstop **plus everything else its validation workflow runs**, and all of it reports into the one required status, so a green clean-compile does not predict a green CI. That remainder is at least the doc-lint set (markdownlint, cspell, actionlint, `editorconfig-checker`) and whatever spec, config, and script gates the repo carries, so read the workflow for the full list rather than assuming this sentence enumerates it. What has to pass before a push is the repo's **whole** lint gate, per [GOVERNANCE.md "Verification Discipline"][governance-verification-discipline]. Each linter's known-working invocation is in `GOVERNANCE.md` "Running the Linters Locally (Known-Working Invocations)", a hub-only section read in a hub checkout rather than carried into every fleet repo.
- **The named task definition is the canonical spec** - its exact command sequence, arguments, and strictness. You may run it through the VS Code task **or** by invoking the equivalent native commands directly, and either is fine **only if the sequence, arguments, and strictness match exactly**. No shortcuts and no more-lenient options (for example, never drop `--verify-no-changes` or loosen a `--severity`).
- **A working local commit/pre-commit gate is strongly suggested, not the repo's free choice to skip.** The *mechanism* is bounded by the toolchain the repo already keeps rather than by which languages its checks cover, and two canonical shapes carry it. Husky.Net runs from a .NET tool manifest declaring it, so a repo that keeps no such manifest uses the `pre-commit` framework instead. Any repo may also wire an equivalent hook of its own at `.husky/pre-commit`, enabled with `core.hooksPath` and sourcing nothing. Canonical shapes for both live in `catalog/snippets/` in the hub, not a repo-relative path since it is hub-local and not carried into every fleet repo. What that gate must cover, its per-clone enablement steps, and what its absence means for the audit, is `GOVERNANCE.md` "Running the Linters Locally (Known-Working Invocations)", the same hub-only section, not restated here. Keeping a working gate is not drift.

### Analyzer Diagnostics and Suppressions

- **A new port is not a license to silence diagnostics.** Brownfield / just-ported status never justifies relaxing analyzer or linter severities or muting newly surfaced warnings. Fix them. (The only brownfield allowance is the one-time git-signing / line-ending migration described in [GOVERNANCE.md][governance] and [README.md][readme], which has nothing to do with code analysis.)
- **Suppress only genuine false-positives or deliberate, documented exceptions**, always at the **narrowest scope that fits**, in this order of preference:
  1. An **in-code annotation on the specific symbol**, with a justification, in the language's attribute/comment form, never a blanket pragma spanning a region.
  2. The **owning project's local config** when the exception is project-wide for one project (e.g. a test project's own `.editorconfig` / `pyproject.toml`).
  3. The **root / shared config** only when the suppression is genuinely applicable to **every** project in the repo.
- **Never blanket-relax a batch of rules project-wide** to get a port to build. The per-language mechanics (which attribute, which config key) are in each language section.

### Markdown and Spelling

These apply repo-wide, in every directory: Markdown lints clean via `markdownlint-cli2` against the shared config, spelling is US English via CSpell against the shared `cspell.json`, the CI spelling gate covers `README.md` and `HISTORY.md` only, `HISTORY.md` mirrors the README's opening, and "Markdown" is a proper noun in prose. A repo excluding a subtree of its own that it does not treat as authored prose, a committed data archive, a vendored theme, or a hand-maintained record, puts a `.markdownlint-cli2.jsonc` carrying its own `ignores` beside that content rather than editing the shared root config, whose contents are fleet-fixed. Those `ignores` patterns resolve against the directory holding them rather than against the repo root, so a repo-root-relative entry there matches nothing and reports no error saying so, and excluding through the CI workflow's own negated Markdown glob input instead is a CI-only fix that leaves the same files flagged for anyone running the linter locally. The full rules are in the `comment-and-doc-style` Skill referenced above.

### NxWitness Conventions

The conventions below are this repo's own, beyond the carried rules.

- **Encoding is UTF-8 without a BOM**, per the root [`.editorconfig`][root] `charset = utf-8`.
- **Leave human-authored comments exactly as written.** Do not reword, trim, reflow, or "clean" a comment a person wrote, even where it bends a carried comment rule. Revise only agent-authored comments, and match the surrounding voice when you do. The comment-growth rule that says to rewrite a comment fresh rather than bolt onto it governs agent-authored comments, and it does not license collapsing a maintainer's.

## .NET

*This section applies only to the .NET side. A repo with no .NET projects still carries it (the file is carried whole) and ignores it.*

The style guide for any .NET projects in this repo: the zero-warnings build policy and its three-task clean-compile chain, central `Directory.Build.props`/`Directory.Packages.props` configuration, C# language and naming conventions, XML documentation, analyzer suppression scope, the library-versus-application logging split, async and error-handling patterns, xUnit v3 + AwesomeAssertions testing conventions, the runner declaration, package references and version floor that an MTP-based test project needs under `WORKFLOW.md` D1.6, with the local diagnostic for a run that reports no tests, and AOT-compatible project configuration.

This is packaged as the `dotnet-codestyle` Skill at `.agents/skills/dotnet-codestyle/SKILL.md` in the hub, not a repo-relative link since that path is hub-local and not carried into every fleet repo. The summary above sketches the scope. Read the skill for the full rules, code examples, and mechanics.

### NxWitness .NET Conventions

The conventions below are this repo's own, beyond the carried rules.

- **The clean-compile is the `.NET Format` VS Code task**, which chains `CSharpier Format` -> `.NET Build` -> `dotnet format style --verify-no-changes --severity=info`. The task definitions in [`.vscode/tasks.json`][tasks] are the canonical command spec, and `Lint: All` runs the document linters locally.
- **This repo wires Husky.Net as its local commit gate.** `.husky/pre-commit` runs `dotnet husky run`, which runs the `.husky/task-runner.json` task set: CSharpier on the staged `.cs` files, then the `dotnet format style` verify. The hook runs no Docker, and CI enforces everything regardless.
- **AOT is opt-in.** `<PublishAot>false</PublishAot>` is the default in `CreateMatrix.csproj`, and publishing with `-p:PublishAot=true` builds an AOT binary. `IsAotCompatible` holds for every build, while `VerifyReferenceAotCompatibility` applies only to an AOT publish, since `System.CommandLine`, the Serilog sinks, and the Polly assemblies that `Microsoft.Extensions.Http.Resilience` brings in are not built as AOT-compatible, and verifying them on every build fails it with `IL3058`.
- **Internals are visible to the test project.** `CreateMatrix.csproj` declares `<InternalsVisibleTo Include="CreateMatrixTests" />`.

## Python

*This section applies only to the Python side. A repo with no Python projects still carries it (the file is carried whole) and ignores it.*

The style guide for any Python project(s) in this repo: the build-versus-lint-only profile split, the uv/ruff/pyright/mypy/pytest toolchain, `src` layout, formatting and linting, comment and docstring conventions, type hints, naming, imports, patterns to avoid, test conventions including the `pytest-cov` dependency and coverage selector a build-profile repo with tests owes under `WORKFLOW.md` D1.6, and versioning.

This is packaged as the `python-codestyle` Skill at `.agents/skills/python-codestyle/SKILL.md` in the hub, not a repo-relative link since that path is hub-local and not carried into every fleet repo. The summary above sketches the scope. Read the skill for the full rules and the profile-adaptation guidance.

## Shell

Bash, and only where a program cannot be Python: a bootstrap that installs the interpreter cannot be written in it, and a host tool that must run before a development toolchain exists cannot depend on Python either. Everything else is Python, with a test under its own scripts tree's `tests/` directory. The mandatory `set -Eeuo pipefail` header, the pipefail-versus-early-reader pitfall, self-locating scripts, the `shellcheck`-plus-`shfmt` clean-compile, and the why-not-what comment rule are packaged as the `shell-codestyle` Skill at `.agents/skills/shell-codestyle/SKILL.md` in the hub, not a repo-relative link since that path is hub-local and not carried into every fleet repo. Read the skill for the full rules. Run the clean-compile check itself per `GOVERNANCE.md` "Running the Linters Locally (Known-Working Invocations)", a hub-only section read in a hub checkout rather than carried into every fleet repo, not by probing `command -v shellcheck`.

<!-- Repo -->

[governance]: ./GOVERNANCE.md
[governance-verification-discipline]: ./GOVERNANCE.md#verification-discipline
[readme]: ./README.md
[root]: ./.editorconfig
[tasks]: ./.vscode/tasks.json
