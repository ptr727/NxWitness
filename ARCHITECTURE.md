# Architecture

How this repo is built: what it publishes, how the codegen data flows from an upstream product version to a built image, how the base and derived images relate, and which of its structures are deliberate deviations from the fleet template rather than drift.

## Products and Published Artifacts

This repository builds and publishes Docker images for Network Optix VMS products: Nx Witness, Nx Meta, Nx Go, DW Spectrum, and Wisenet WAVE. Each product ships in two variants, a plain Ubuntu image and a LinuxServer.io (LSIO) image, giving ten product images built on top of two shared base images, `nx-base` and `nx-base-lsio`.

There is **no NuGet publish**. `CreateMatrix` is a build-time code generator, not a shipped package, and the published artifacts are exclusively the Docker Hub images.

## Codegen Data Flow

The build inputs are generated rather than hand-maintained, and the generator is the .NET side of the repo:

- `CreateMatrix` is a .NET 10 console app. It fetches product release metadata from the upstream Network Optix release feeds and writes this repo's build inputs.
- `CreateMatrixTests` is its xUnit v3 test project, using AwesomeAssertions.

One upstream version becomes a built image along this path:

1. `CreateMatrix version` reads the upstream release feeds and refreshes `Make/Version.json`, which holds, per product, the version list with its x64 and arm64 download URIs and its labels (`Stable`, `Latest`, `RC`, `Beta`).
2. `CreateMatrix matrix` expands `Make/Version.json` into `Make/Matrix.json`, the build matrix. Each row is exactly one image build, carrying its image `Name`, `Product`, `Branch` (`main` or `develop`), `Base` (`ubuntu` or `lsio`), the Docker Hub `Tags` it publishes, and the `Args` (download URLs and version) the build consumes.
3. `CreateMatrix make` renders the per-product Dockerfiles into `Docker/` and the test Compose files into `Make/` from those inputs.
4. CI builds `Make/Matrix.json` row by row, so a product version reaches an image only by being pinned in that file first.

`Make/Matrix.json` is therefore the pin: it is what the publisher reads, what the codegen pull request updates, and what a path-scoped push to it publishes. `version.json` at the repo root is unrelated to product versions; it is the Nerdbank.GitVersioning input.

Because `Docker/` and the Compose files under `Make/` are generated, a change to the generator is incomplete until the regenerated output is committed alongside it. Keep generated outputs in sync with `CreateMatrix` behavior, and keep `README.md` and the release documentation aligned with the build outputs and product variants.

## Image Architecture

- The base images `nx-base` and `nx-base-lsio` are hand-written (`Docker/NxBase.Dockerfile`, `Docker/NxBase-LSIO.Dockerfile`). They are built and pushed first, then reused as the `FROM` image for every derived product Dockerfile.
- The base tag is branch-agnostic (`nx-base:ubuntu-noble` and `nx-base-lsio:ubuntu-noble`), so it is built once on the `main` publish run and reused rather than rebuilt by a develop run, which would otherwise overwrite it.
- Derived product images stay aligned with base image changes and tags, the Ubuntu distro tag in particular. A base change that a derived image is not aligned to is the failure this rule exists to prevent, and nothing in the pull request pipeline catches it because the pull request smoke build covers only two of the ten product images.
- The two variants differ only in their base and the user model that follows from it. `nx-base` is built from `ubuntu:noble`, and `nx-base-lsio` is built from `lsiobase/ubuntu:noble` and follows the LinuxServer.io user conventions (`Docker/lsio-rename-user.sh`). The s6-overlay service tree under `Docker/s6-overlay` and the shared scripts `Docker/download.sh` and `Docker/entrypoint.sh` are common to both.
- Every image is built for `linux/amd64` and `linux/arm64`.

## CI Pipeline (GitHub Actions)

The full CI/CD contract, meaning triggers, jobs, the one-branch publish model, versioning, and the multi-image build layer, is specified in [WORKFLOW.md][workflow], the canonical guide. The summary below is a pointer and does not duplicate those rules.

- Every task is hub-hosted in `ptr727/ProjectTemplate` and reached by a SHA-pinned `uses:`, so this repo carries only its entry workflows and three hooks under [`.github/actions/`][actions]: `docker-prepare` maps `Make/Matrix.json` onto the hub's Docker matrix, `docker-build-base` builds the shared bases, and `codegen` runs the generator.
- CI runs on **push to every branch** ([test-pull-request.yml][test-pull-request]): it validates (the hub's `validate-task.yml`) on every push, and runs a fast smoke build (the hub's `build-release-task.yml` with `smoke: true`, meaning NxMeta and NxMeta-LSIO, amd64, no push) only when image files (`Docker/**`, `Make/Matrix.json`, `Make/Version.json`, the Docker hooks, the workflows) change, via an inline `git diff` change-gate. One aggregator job, `Check pull request workflow status job`, is the ruleset-bound required check.
- Publishing is **triggered-Docker, one branch per run** ([publish-release.yml][publish-release]): the triggers are the weekly schedule (rebuilds `main` only), a path-scoped push to `main` on `Make/Matrix.json` (publishes a new codegen product pin at once), and manual dispatch (publishes the started-from branch). One run builds the shared base once (main only, with a develop dispatch reusing it), then the hub's `build-release-task.yml` computes the version once, builds the branch's product matrix from `Make/Matrix.json`, and on `main` cuts the GitHub release, and the hub's `publish-docker-readme-task.yml` pushes the Docker Hub overviews.
- Merges to `main` or `develop` do not build or publish images by themselves. Only the matrix-pin push to `main`, a schedule, or a dispatch publishes. Auto-merged Dependabot and codegen pull requests land commits the next publish picks up.
- Do not reintroduce a two-branch publish matrix, a carried copy of a hub task, the date-badge workflow, or `dorny/paths-filter`.

## Versioning Labels and Tags

Two version streams reach the images, and confusing them is the common error:

- **The NBGV version labels the image.** The hub release chain's single `get-version` run computes the Nerdbank.GitVersioning version once and threads its `semver2` output down into the hub's `build-docker-task.yml` as the `LABEL_VERSION` build arg, so one classification labels every product leg and no second NBGV run can reclassify it. The same version is the GitHub release tag on `main`.
- **The Nx product version tags the image.** The Docker Hub tags come from `Make/Matrix.json`, not from NBGV. A row's `Tags` carry the upstream product version and its channel alias (`stable`, `latest`, `rc`, `beta`, and the `develop-` prefixed forms). The NBGV version appears only as the image label and the release tag, never as a Docker tag.

## Template Adaptations

This repo derives its CI and conventions from the fleet template, `ptr727/ProjectTemplate`. Carried artifacts are taken by full-file replacement, and the deliberate deviations below are documented so they are not mistaken for drift.

- **Triggered-Docker publisher, one branch per run.** `publish-release.yml` is `workflow_dispatch` plus a weekly `schedule` (main only) plus a path-scoped `push` to `main` on `Make/Matrix.json`, where the hub's Docker shape is dispatch plus schedule alone. It builds exactly one branch, the trigger ref (`github.ref_name`), so NBGV classifies natively with no cross-branch leg and no `IGNORE_GITHUB_REF`. The jobs are a `plan` -> `validate` -> `build-base` (main only) -> `publish` -> `publish-docker-readme` (main only) chain. A develop dispatch refreshes the `:develop` images only, with no GitHub release.
- **Multi-image, shared-base build layer through hooks.** This repo is the hub Docker family's matrix and `build-base` case. The `docker-prepare` hook emits the branch's `Make/Matrix.json` rows as the hub's matrix, and takes the smoke build's `docker_image` input as a name filter rather than as a Docker Hub repository. The hub core owns the cache policy, the platform selection, and the login, and runs the matrix without the `max-parallel: 4` cap the carried copy had.
- **Repo-owned pushing base build.** The hub's `build-base` leg holds no Docker Hub login, and a composite hook cannot read secrets, so the publisher's own `build-base` job logs in and runs the `docker-build-base` hook to push the shared `nx-base` and `nx-base-lsio` images, and `publish` sets `docker_build_base: false`. Only the non-pushing smoke build reaches the hook through the hub. The hook takes `push` as its branch signal, since no branch reaches it: a push is a `main` publish, built multi-arch against `buildcache-main` plus the inline cache on the base tag.
- **Docker-only GitHub release, with no `release-asset-*` files.** The publisher sets `expect_release_assets: false`, so the hub's release carries the tag, the auto source zip, README, and LICENSE, and sets `github: true` only on `main`.
- **Docker Hub readme from `Docker/README.md`.** The hub's `publish-docker-readme-task.yml` pushes [`Docker/README.md`][docker-readme], its default's first choice, to every product and base repository, the list derived from `Make/Matrix.json` by its `manifest-jq` input.
- **No date badge.** The repo ships no `build-datebadge-task.yml` workflow and no publisher job for it, and `README.md` carries no "Last Build" badge pointing at a BYOB gist.
- **Husky.Net pre-commit hooks.** This repo runs its local hook through Husky.Net, configured in `.husky/task-runner.json`, rather than through the Python `pre-commit` framework. The hook runs the same CSharpier and `dotnet format style` checks the hub's `validate-task.yml` enforces in CI, surfaced earlier.
- **No upstream-version tracker.** This repo tracks the upstream Nx version through codegen, the `codegen` hook updating `Make/Version.json` and `Make/Matrix.json`, so it carries no `check-upstream-version-task.yml` caller, and the hub merge-bot's built-in `codegen-main` and `codegen-develop` rules cover its App pull requests.

The `.vscode` task-set deviation is recorded in [OPERATIONS.md][operations], with the tooling it belongs to.

<!-- Repo -->

[actions]: ./.github/actions/
[docker-readme]: ./Docker/README.md
[operations]: ./OPERATIONS.md
[publish-release]: ./.github/workflows/publish-release.yml
[test-pull-request]: ./.github/workflows/test-pull-request.yml
[workflow]: ./WORKFLOW.md
