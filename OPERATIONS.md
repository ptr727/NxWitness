# Operations

How this repo is run: what verifying a change requires before it is pushed, the commands that regenerate and build the images, and where the repo-specific tooling is configured.

## Local Verification

Verifying a change here is two things: running the gates, and then running by hand the one part of the contract the gates never reach.

The gates are the .NET clean-compile (the `.NET Format` VS Code task, per [CODESTYLE.md](./CODESTYLE.md)), the unit tests, and the document linters. [GOVERNANCE.md "Running the Linters Locally"](./GOVERNANCE.md#running-the-linters-locally-known-working-invocations) carries the known-working invocation of each linter, and CI runs the same set in [validate-task.yml](./.github/workflows/validate-task.yml), so a local lint run buys an earlier failure rather than a different one. **Linting is not editor-only here**: `validate-task.yml` runs markdownlint, CSpell, `actionlint`, and `editorconfig-checker` alongside the Husky style checks and `dotnet test`, all inside the required check.

**What CI structurally cannot exercise is the product image matrix.** The pull-request pipeline builds a deliberate smoke subset: NxMeta and NxMeta-LSIO, amd64 only, never pushed, and only when `Docker/**`, `Make/Matrix.json`, or `Make/Version.json` changed. Eight of the ten product images, the arm64 leg of every image, the base image push path, and a container that actually starts and serves its web UI are all unbuilt at merge time. The publish run is the first thing that builds them, and a workflow-only edit is deliberately not smoke-built at all. So a change to a Dockerfile, a build arg, a base image, or the generated matrix is verified locally by running `./Create.sh` and `./Build.sh` from inside `Make/`, which build every product image for both `linux/amd64` and `linux/arm64`, and, when the change can affect a running server, `./Test.sh` followed by `./Instructions.sh` there to reach each product's web UI. Reading a green pipeline as coverage of the full matrix is the mistake this section exists to name.

## Runbooks

### Regenerate the Version, Matrix, and Dockerfiles

The primary developer entry points are the `CreateMatrix` CLI commands, invoked directly or through the scripts in `Make/`:

```sh
dotnet run --project ./CreateMatrix/CreateMatrix.csproj -- version --versionpath=./Make/Version.json
dotnet run --project ./CreateMatrix/CreateMatrix.csproj -- matrix --versionpath=./Make/Version.json --matrixpath=./Make/Matrix.json --updateversion
dotnet run --project ./CreateMatrix/CreateMatrix.csproj -- make --versionpath=./Make/Version.json --makedirectory=./Make --dockerdirectory=./Docker
```

`version`, `matrix` and `make` are subcommands of the `CreateMatrix` executable rather than programs on `PATH`, so they are reached through `dotnet run` from the repository root. `make` in particular is not the system `make`. `--versionlabel` is left off so this matches what `Make/Create.sh` runs, which takes the `Latest` default; passing a different label selects different product versions and so generates different Dockerfiles.

`Docker/` and the Compose files in `Make/` are generated output, so a change to the generator is committed together with its regenerated output.

### Build and Run the Images Locally

The scripts in `Make/` wrap the generator and the Docker build, and each is run from inside `Make/`:

- `Create.sh` updates `Version.json` and `Matrix.json` and writes the Dockerfiles.
- `Build.sh` builds the base images and every product image for `linux/amd64` and `linux/arm64`, and loads the amd64 targets. Setting `PUSH_BASE_IMAGES=true` pushes the base images, which needs a Docker Hub login.
- `Test.sh` runs `Create.sh`, then `Build.sh`, then `Up.sh`.
- `Up.sh`, `Up-latest.sh`, and `Up-develop.sh` bring up the locally built (`Test.yml`), released, and develop Compose stacks. `Down.sh`, `Down-latest.sh`, and `Down-develop.sh` bring the matching stack back down.
- `Instructions.sh` prints the web UI URL of each product in the running stack.
- `Clean.sh` shuts the stack down and deletes the images.

## Backup and Recovery

There is no state to back up. The repository is the record and GitHub holds it, every published image is rebuilt from the pinned inputs in `Make/Matrix.json` and `Make/Version.json`, and a local Compose stack is disposable by design, since `Clean.sh` deletes it outright. Recovering a published image means re-running the publisher against the commit that carries that pin, not restoring anything.

## Logs and Debugging

Workflow runs are the log for anything that happened in CI or in a publish. `gh run list --branch <branch>` and `gh run view <id> --log-failed` reach them. A gate failure reproduces locally, because CI runs the same commands against the same committed configuration, so reproduce it locally before reading workflow logs.

A failure that appears only in a built image needs a running container instead. Bring the stack up with `./Up.sh` from inside `Make/`, find the product's web UI with `./Instructions.sh` there, read `docker logs <container>`, and attach a shell with `docker exec --interactive --tty <container> /bin/bash`. The mediaserver's own logging is a product setting rather than a container one: set `logLevel=verbose` in `mediaserver.conf`, restart the server, and read `/config/var/log/log_file.log` inside the container.

## Tool Usage

**Tests run on the native Microsoft.Testing.Platform runner**, opted into by `global.json`. The invocation CI runs, and the one to reproduce locally, is `dotnet test --coverage --coverage-output-format cobertura --results-directory ./coverage`, which then prefixes each report to `coverage-<guid>.cobertura.xml` so codecov-cli's file finder matches it. Under MTP a run that discovers no tests exits 5 and reports `Zero tests ran` rather than passing silently, so read the count and not just the exit status. If `dotnet test` reports zero tests on a machine where the build succeeded, run the built test application directly, `dotnet CreateMatrixTests/bin/Debug/net10.0/CreateMatrixTests.dll`, which is the same MTP host without the `dotnet test` driver in front of it, and compare. A driver that reports the target as `net10.0` where the direct run reports `net10.0|x64` has not resolved an architecture, and its zero-test result says nothing about the tests. `--coverage-output` stays unset, because pinning one filename gives every test project in the solution the same path and the last to finish overwrites the rest. Running an MTP-based test project through the VSTest target is what fails, so the `--collect:"XPlat Code Coverage"` form is an error in this repo specifically because it opted in.

**C# formatting has an order**: CSharpier formats, then `dotnet format style` verifies. Running them the other way round means CSharpier rewrites what `dotnet format` just verified. The `.NET Format` VS Code task chains them correctly and must be clean and warning-free at all times; [CODESTYLE.md](./CODESTYLE.md) carries the full task chain and its exact arguments.

**Husky.Net runs the pre-commit hook.** `.husky/task-runner.json` defines the task set: `CSharpier Format` over the staged `.cs` files, then `.NET Format` running `dotnet format style --verify-no-changes`. `dotnet husky run` runs that set by hand. Matching this tooling, `.vscode/tasks.json` carries a Husky.Net Run task where the fleet template carries a Benchmark task.

**Workflow files get an editor check and a CLI check.** The GitHub Actions extension covers schema and expression checks while editing; run the `actionlint` CLI for the deeper checks, including shellcheck over `run:` steps. This matters more here than the lint gate alone suggests, because a workflow-only change is not smoke-built.

## Configuration Layout

- `NxWitness.code-workspace` is this repo's workspace file. Open it in VS Code rather than the folder so its settings and recommended extensions apply.
- `cspell.json` is the single source of truth for the spell-check dictionary and ignore paths. A new project word belongs in its `words` list.
- `.vscode/tasks.json` and `.vscode/launch.json` hold the build, debug, and lint tasks, including the `.NET Format` chain and the Husky.Net Run task.
- `.husky/task-runner.json` defines the pre-commit task set.
- `.markdownlint-cli2.jsonc` configures markdownlint, and `.editorconfig-checker.json` scopes the line-ending and whitespace check.
- `global.json` opts into the Microsoft.Testing.Platform test runner. It deliberately carries no `sdk` section, so SDK resolution and roll-forward stay at their defaults. `codecov.yml` configures the report-only coverage upload.
- `version.json` is the Nerdbank.GitVersioning input. `Make/Version.json` and `Make/Matrix.json` are the product version and build matrix inputs, and [ARCHITECTURE.md](./ARCHITECTURE.md) describes how they reach the images.
- `host-tools.json` declares the host tooling a development machine needs.
