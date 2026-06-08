# GitHub Copilot Guidance

## Purpose

This file summarizes the solution and defines the hierarchy of guidance for AI-assisted contributions.

## Guidance Hierarchy (Must Follow)

1. [CODESTYLE.md](../CODESTYLE.md) is the master code style and formatting authority.
2. [AGENTS.md](../AGENTS.md) is secondary guidance describing the solution, workflows, and conventions.
3. Repository configuration files such as [`.editorconfig`](../.editorconfig) and [`.vscode/tasks.json`](../.vscode/tasks.json) define enforced formatting, line endings, and task expectations.

If any instruction conflicts, follow CODESTYLE.md first, then AGENTS.md.

## Solution Summary

This repository builds and publishes Docker images for Network Optix VMS products (Nx Witness, Nx Meta, Nx Go, DW Spectrum, Wisenet WAVE). It includes base images (nx-base, nx-base-lsio) and derived product images, plus a .NET tooling project that generates Dockerfiles, matrices, and version inputs used by CI and packaging scripts.

### Core Projects

- `CreateMatrix` (.NET 10 console app): Generates Dockerfiles and build matrix data using version and release metadata.
- `CreateMatrixTests` (xUnit v3 + AwesomeAssertions): Validates release handling and version forwarding.

### Key Inputs and Outputs

- Inputs: version and matrix data in `version.json`, [Make/Version.json](../Make/Version.json), and [Make/Matrix.json](../Make/Matrix.json).
- Outputs: Dockerfiles in [Docker/](../Docker/) (base images and derived product images) and compose/test artifacts in [Make/](../Make/).
- Templates: Unraid container templates in [Unraid/](../Unraid/).

### Build and Validation Workflow (High Level)

- Primary entry points are the `CreateMatrix` CLI commands (version, matrix, make) run directly or via scripts in [Make/](../Make/).
- Formatting and style verification are enforced by CSharpier and dotnet format, with Husky.Net hooks.
- The `.Net Format` VS Code task in [`.vscode/tasks.json`](../.vscode/tasks.json) must be clean and warning-free at all times.

### Image Architecture

- Base images (`nx-base`, `nx-base-lsio`) are built and pushed, then used as `FROM` images for derived product Dockerfiles.
- Derived images should track base image tag changes (for example, the Ubuntu distro tag) to keep builds consistent.

### CI Pipeline (GitHub Actions)

- Pull requests run unit tests and style checks, plus a fast smoke build (NxMeta and NxMeta-LSIO, amd64 only, no push) that runs only when image files change -- not the full matrix.
- Publishing is schedule/manual only via `publish-release.yml`, which builds the base images once and then publishes the full matrix for both the `main` and `develop` branches in a single run.
- Merges to `main`/`develop` do not publish; auto-merged Dependabot and codegen PRs are picked up by the next scheduled publish. Do not reintroduce push-triggered publishing or full-matrix PR builds.
- Structured files are linted in-editor via the extensions recommended in the workspace file `NxWitness.code-workspace` (C#, Markdown, Docker, GitHub Actions, spelling) rather than a CI lint job; lint changed files before pushing, and run `actionlint` for deeper workflow checks. Editor settings, extension recommendations, and spell-check words belong in the workspace file (not `.vscode/`). See AGENTS.md.

## What to Keep in Sync

- Generated Dockerfiles and scripts must reflect CreateMatrix behavior.
- Base image Dockerfiles and derived image Dockerfiles should remain aligned since derived images build on the base images.
- Documentation in [README.md](../README.md) and release notes should align with current outputs and supported product variants.

## Expectations for Changes

- Follow the zero-warnings policy and formatting requirements in [CODESTYLE.md](../CODESTYLE.md).
- Use explicit types (no `var`), Allman braces, file-scoped namespaces, and other conventions as defined in the master style guide.
- Respect line endings and encoding rules from the repository configuration, including UTF-8 without BOM.

## GitHub Copilot Review Runbook

Use this section for provider-specific mechanics. The expected review loop *contract* (request review on every push, verify head-SHA coverage, triage findings, reply + resolve, escalate when stuck) is defined in [AGENTS.md -> PR Review Etiquette](../AGENTS.md#pr-review-etiquette). This section only describes how to make GitHub Copilot reliably execute it.

### Triggering and Polling

Auto-review on push is configured (via the branch ruleset's `copilot_code_review` rule with `review_on_push: true`) but fires inconsistently in practice - treat it as best-effort, not guaranteed. After every push, **re-request a review programmatically** via the GraphQL `requestReviews` mutation, passing the Copilot reviewer's bot node id in `botIds`. This now works reliably (it previously did not - a maintainer had to click "re-request review" in the UI; the agent can now drive the loop end-to-end without that hand-off).

> **The reviewer login differs by API - this is intentional, not a typo.** In **GraphQL** (`gh api graphql` and `gh pr view --json reviews`, which is GraphQL-backed) the `Bot.login` is `copilot-pull-request-reviewer` - **no `[bot]` suffix**. In the **REST** API (`gh api repos/.../issues|pulls/...`) the same account's `user.login` is `copilot-pull-request-reviewer[bot]` - **with** the suffix. Each query below uses the correct form for its API; match the API, not a single spelling, when adapting them.

```sh
# 1. PR node id + the Copilot reviewer's bot node id (read from any existing
#    Copilot review; the reviewer login is `copilot-pull-request-reviewer`).
PR_NODE=$(gh pr view <N> --json id --jq '.id')
BOT_ID=$(gh api graphql -f query='
{
  repository(owner: "ptr727", name: "NxWitness") {
    pullRequest(number: <N>) {
      reviews(first: 50) { nodes { author { __typename login ... on Bot { id } } } }
    }
  }
}' --jq '[.data.repository.pullRequest.reviews.nodes[]
          | select(.author.login == "copilot-pull-request-reviewer")
          | .author.id] | first')

# 2. Re-request a Copilot review on the current head.
gh api graphql -f query='
mutation($pr: ID!, $bot: ID!) {
  requestReviews(input: { pullRequestId: $pr, botIds: [$bot], union: true }) {
    pullRequest { id }
  }
}' -F pr="$PR_NODE" -F bot="$BOT_ID"
```

The bot node id is read from an existing Copilot review, so step 1 needs at least one prior review on the PR - the auto-review-on-open normally supplies the first one. If no Copilot review exists yet and auto-review didn't fire, request `Copilot` once through the GitHub PR UI to seed it, then use the mutation for every subsequent re-request.

**Do NOT post `@Copilot review` as a PR comment.** That comment triggers the Copilot *coding agent* (`copilot-swe-agent[bot]`), which makes code changes rather than posting a review.

Known non-working request paths (don't rely on them - use the `requestReviews` mutation above instead):

- `POST /requested_reviewers` with `reviewers=[Copilot]` can return 200 but no-op.
- `copilot-pull-request-reviewer` as a requested reviewer slug returns 422.

### Verify Review Covered Current Head

Before merging, confirm Copilot reviewed the current PR head SHA. Copilot may respond as either a formal review (carries an exact commit SHA) or an issue comment (no SHA - use the most recent Copilot comment for manual confirmation). Check both.

```sh
PR_HEAD=$(gh pr view <N> --json headRefOid --jq '.headRefOid')

# 1. Formal review - exact SHA match.
gh pr view <N> --json reviews --jq \
  '.reviews[] | select(.author.login=="copilot-pull-request-reviewer") | .commit.oid' \
  | grep -q "$PR_HEAD" && echo "covered via formal review"

# 2. Issue comment - show the most recent Copilot comment for manual
#    confirmation. This is the REST API, so the login carries the `[bot]` suffix.
gh api repos/ptr727/NxWitness/issues/<N>/comments --jq \
  '[.[] | select(.user.login=="copilot-pull-request-reviewer[bot]")] | last | {created_at, body: .body[:200]}'
```

Coverage is confirmed when (1) exits 0. For issue comments (path 2), body content is the only reliable signal - `created_at` is not: `git log -1 --format=%cI` is the **commit** timestamp, not the push timestamp, so amended or rebased commits can have an earlier timestamp and an older Copilot comment could satisfy a time check even though Copilot never saw the current head. Treat path (2) as confirmed only when the comment body explicitly refers to the current changes.

### Bounded Retry Workflow

If a review did not run on the current head, retry:

1. Wait briefly and check head-SHA coverage (see above).
1. Re-request the review via the `requestReviews` mutation (see "Triggering and Polling"); fall back to the GitHub PR UI only if the mutation no-ops.
1. Retry up to two more times (three total).
1. If still missing, mark review as blocked and escalate to the user/maintainer with what was attempted.

### Reply and Thread Resolution Workflow

List unresolved threads. Use `first: 100` with cursor-based pagination; if `hasNextPage` is true, re-run with `after: "<endCursor>"` to retrieve the next page:

```sh
gh api graphql -f query='
{
  repository(owner: "ptr727", name: "NxWitness") {
    pullRequest(number: <N>) {
      reviewThreads(first: 100) {
        nodes {
          id isResolved path
          comments(first: 1) { nodes { author { login } body } }
        }
        pageInfo { hasNextPage endCursor }
      }
    }
  }
}' | jq '
  .data.repository.pullRequest.reviewThreads |
  (.pageInfo | "hasNextPage=\(.hasNextPage) endCursor=\(.endCursor)"),
  (.nodes[] | select(.isResolved == false))
'
```

Reply on a thread, then resolve it:

```sh
gh api graphql -f query='
mutation($threadId: ID!, $body: String!) {
  addPullRequestReviewThreadReply(input: { pullRequestReviewThreadId: $threadId, body: $body }) {
    comment { id }
  }
}' -F threadId="PRRT_..." -F body="Fixed in <SHA>: <one-line summary>."

gh api graphql -f query='
mutation($threadId: ID!) {
  resolveReviewThread(input: { threadId: $threadId }) { thread { id isResolved } }
}' -F threadId="PRRT_..."
```

Issue-level Copilot comments (those in `issues/<N>/comments`) have no resolution action - GitHub provides no API or UI to resolve them. Reply if the finding warrants it; no resolution step is needed or possible.

Reply-body conventions:

- Accepted bug/style fix: include fixing commit SHA and a one-line summary.
- Declined style comment: cite the rule (AGENTS.md or language CODESTYLE) and the existing-tree precedent.
- Declined architecture proposal: one-sentence rationale.

After the final push, sweep-resolve stale older threads for removed code paths.
