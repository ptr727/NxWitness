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

- CI runs on **push to every branch** ([test-pull-request.yml][test-pull-request]): it validates ([validate-task.yml][validate-task]) on every push, and runs a fast smoke build ([build-docker-task.yml][build-docker-task] with `smoke: true`, meaning NxMeta and NxMeta-LSIO, amd64, no push) only when image files (`Docker/**`, `Make/Matrix.json`, `Make/Version.json`) change, via an inline `git diff` change-gate. One aggregator job, `Check pull request workflow status job`, is the ruleset-bound required check.
- Publishing is **triggered-Docker, one branch per run** ([publish-release.yml][publish-release]): the triggers are the weekly schedule (rebuilds `main` only), a path-scoped push to `main` on `Make/Matrix.json` (publishes a new codegen product pin at once), and manual dispatch (publishes the started-from branch). One run computes the version once ([get-version-task.yml][get-version-task]), builds the shared base once (main only, with a develop dispatch reusing it via `build_base: false`), builds the full product matrix from `Make/Matrix.json`, and on `main` cuts the GitHub release and pushes the Docker Hub overviews.
- Merges to `main` or `develop` do not build or publish images by themselves. Only the matrix-pin push to `main`, a schedule, or a dispatch publishes. Auto-merged Dependabot and codegen pull requests land commits the next publish picks up.
- Do not reintroduce a two-branch publish matrix, a nested `get-version` in the build task, the date-badge or standalone docker-readme workflows, or `dorny/paths-filter`.

## Versioning Labels and Tags

Two version streams reach the images, and confusing them is the common error:

- **The NBGV version labels the image.** The orchestrator's single `get-version` run computes the Nerdbank.GitVersioning version once and threads its `semver2` output down into `build-docker-task.yml` as the `LABEL_VERSION` build arg, so one classification labels every product leg and no second NBGV run can reclassify it. The same version is the GitHub release tag on `main`.
- **The Nx product version tags the image.** The Docker Hub tags come from `Make/Matrix.json`, not from NBGV. A row's `Tags` carry the upstream product version and its channel alias (`stable`, `latest`, `rc`, `beta`, and the `develop-` prefixed forms). The NBGV version appears only as the image label and the release tag, never as a Docker tag.

## Template Adaptations

This repo derives its CI and conventions from the fleet template, `ptr727/ProjectTemplate`. Carried artifacts are taken by full-file replacement, and the deliberate deviations below are documented so they are not mistaken for drift.

- **Triggered-Docker publisher, one branch per run.** `publish-release.yml` is `workflow_dispatch` plus a weekly `schedule` (main only) plus a path-scoped `push` to `main` on `Make/Matrix.json`. It builds exactly one branch, the trigger ref (`github.ref_name`), so NBGV classifies natively with no cross-branch leg and no `IGNORE_GITHUB_REF`. The jobs are a single `get-version` -> `build-base` (main only) -> `build-docker` -> `github-release` (main only) -> `docker-readme` (main only) -> `cleanup-artifacts` chain. A develop dispatch refreshes the `:develop` images only, with no GitHub release, and there is no two-leg `build-main` / `build-develop` combined run.
- **Multi-image, shared-base build layer.** The shared `nx-base` and `nx-base-lsio` images are built once on the `main` run and reused by a develop dispatch (`build_base: false`, so it never overwrites the branch-agnostic `nx-base` tag), and `build-docker-task.yml` builds every product image from `Make/Matrix.json` with `max-parallel: 4`. The template's single-target branch matrix cannot express the shared-base fan-out, so the build layer stays repo-owned.
- **Single NBGV run threaded to the build task.** `build-docker-task.yml` has no nested `get-version`. The orchestrator's single `get-version` run threads `semver2` down as the image `LABEL_VERSION`, so one classification feeds every product leg and no second NBGV run can reclassify or collide a tag.
- **Docker-only GitHub release, with no `release-asset-*` files.** The `github-release` job follows the template's generic release semantics (tag on the built commit, auto source zip plus README and LICENSE, `target_commitish` pinned, skip-existing guard, main-only `Verify public release version` backstop), but this repo ships no binary or package release assets because the published artifacts are the Docker Hub images. So there is no `release-asset-*` download step, and `fail_on_unmatched_files` is omitted since it has no files to guard.
- **Folded Docker Hub readme.** There is no standalone docker-readme task. `publish-release.yml` carries `docker-readme-repos` and `docker-readme` jobs gated to `main` that derive the repository list inline from `Make/Matrix.json` (lowercased `ptr727/<image>` plus the shared base repos) and matrix `peter-evans/dockerhub-description` over it.
- **No date badge.** The repo ships no `build-datebadge-task.yml` workflow and no publisher job for it, and `README.md` carries no "Last Build" badge pointing at a BYOB gist.
- **Husky.Net pre-commit hooks.** This repo runs its local hook through Husky.Net, configured in `.husky/task-runner.json`, rather than through the Python `pre-commit` framework. The hook runs the same CSharpier and `dotnet format style` checks CI enforces, surfaced earlier, and `validate-task.yml`, which stands in for the template's `test-release-task.yml`, runs that same Husky lint plus `dotnet test` in CI as the required check's quality gate.
- **Repo-owned build-layer leaves.** The build-layer leaves (`build-docker-task.yml`, `build-base-images-task.yml`) own their per-image Dockerfiles, build args, and target matrix, which the template's build layer does not express. Owning those specifics is not an exemption from the shared workflow conventions: their actions are SHA-pinned like the orchestration layer, and their Docker layer cache uses per-image registry-tag caches (`docker.io/ptr727/<repo>:buildcache-<branch>`, plus the base image's own tag and inline cache).
- **No `merge-upstream-version` merge-bot job.** This repo tracks the upstream Nx version through codegen, with `run-codegen-pull-request-task.yml` updating `Make/Version.json` and `Make/Matrix.json`, so the merge bot keeps `merge-codegen` and omits the template's `merge-upstream-version` job, which uses the separate `check-upstream-version-task.yml` mechanism this repo does not ship.

The `.vscode` task-set deviation is recorded in [OPERATIONS.md][operations], with the tooling it belongs to.

<!-- Repo -->

[build-docker-task]: ./.github/workflows/build-docker-task.yml
[get-version-task]: ./.github/workflows/get-version-task.yml
[operations]: ./OPERATIONS.md
[publish-release]: ./.github/workflows/publish-release.yml
[test-pull-request]: ./.github/workflows/test-pull-request.yml
[validate-task]: ./.github/workflows/validate-task.yml
[workflow]: ./WORKFLOW.md
