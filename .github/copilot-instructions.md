# Copilot Instructions

Repository conventions for GitHub Copilot (and any other AI agent reading this file).

The **canonical guide is [AGENTS.md](../AGENTS.md)** at the repo root - read it first. It covers project layout, branch flow, PR review etiquette, the release pipeline, workflow YAML conventions, and what NOT to touch.

This file is intentionally narrow: commit/PR-title conventions (so VS Code's AI commit-message and PR-title generators get them without an extra fetch), plus a GitHub Copilot Review Runbook that documents the provider-specific mechanics behind the review-loop contract defined in AGENTS.md.

For language-specific style rules, see:

- .NET - [`CODESTYLE.md`](../CODESTYLE.md) at the repo root.

Do not duplicate language-specific rules here.

## Commit Messages and Pull Request Titles

Feature -> develop PRs squash-merge - the PR title becomes the single commit on develop. Develop -> main PRs merge-commit - main's history shows one merge commit per release with develop's tip as the second parent. Titles are descriptive and have no versioning effect - versioning is handled by [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) reading [version.json](../version.json) and git history, not by parsing commit messages.

`develop` leads `main` by a minor. After a `develop -> main` release lands and main's publish completes, bump the minor in [version.json](../version.json) on `develop` via an isolated `bump-version-X.Y` PR, so develop's prereleases sort above main's last stable. A `develop -> main` promotion that carries only maintenance (not a release) holds main's version instead - `git checkout main -- version.json` on the promotion branch. See [AGENTS.md "Versioning"](../AGENTS.md#versioning).

Branch protection enforces the merge method on both bases (develop allows only squash, main allows only merge). When running `gh pr merge` against either base, pick the matching flag (`--squash` for develop, `--merge` for main); a mismatch fails with "Merge method ... is not allowed on this repository". The merge-bot workflow (`.github/workflows/merge-bot-pull-request.yml`) does this dispatch automatically for Dependabot and codegen PRs via a `case` on `base.ref` - keep that pattern when adding new auto-merge jobs.

### Format

- Imperative subject summarizing the change, <= 72 characters, no trailing period. ("Add 24-hour PM2.5 average sensor", not "Added X" or "Adds X".)
- Optional body, blank-line separated, explaining *why* the change is being made when that's non-obvious. The diff shows *what*.

### Rules

- Don't write `update stuff`, `wip`, or other vague titles. (Dependabot's default `Bump X from Y to Z` titles are fine - keep them.)
- Don't add `Co-Authored-By:` lines unless the user explicitly asks.
- Don't put release-bump magnitude in the title - no "minor", "patch", "release v0.2.0", etc. NBGV computes the next release version from `version.json` + git history. Dependency versions in dependency-bump titles are fine and expected.
- Use US English spelling and match the existing heading style of the file you're editing: title case with lowercase short bind words (a, an, the, and, but, or, of, in, on, at, to, by, for, from); hyphenated compounds capitalize both parts unless the second is a short preposition (*Built-in*, *EPA-Corrected*, *24-Hour*).

### Examples

```text
Add structured logging extensions to library
Pin softprops/action-gh-release to commit SHA
Drop net8.0 multi-targeting from console project
Bump xunit.v3 from 3.2.2 to 3.3.0
Clarify LSIO volume configuration in README
```

## GitHub Copilot Review Runbook

> **Carry this runbook verbatim into every derived repo.** This whole file is a drop-in: copy it as-is and change only the `<owner>` / `<repo>` / `<N>` placeholders in the snippets below (and drop the language-specific style pointers that don't apply). It pairs with the [AGENTS.md "PR Review Etiquette"](../AGENTS.md#pr-review-etiquette) contract, which is also a mandatory verbatim carry - see [ProjectTemplate AGENTS.md "Files and Sections Derived Repos Must Carry Verbatim"](https://github.com/ptr727/ProjectTemplate/blob/main/AGENTS.md#files-and-sections-derived-repos-must-carry-verbatim). A derived repo without this runbook in-repo has no pointer to the reliable Copilot mechanics and falls back to known-broken paths (the no-op `POST /requested_reviewers`, the wrong bot-login filter).

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

## When in Doubt

Read [AGENTS.md](../AGENTS.md) for the full picture (release flow, files you must not touch, branching, workflow YAML). For language-specific rules, [`CODESTYLE.md`](../CODESTYLE.md) is authoritative. Don't restate any of these files' rules in commit bodies or PR descriptions - keep those focused on the change itself.

**In a derived repo:** if you find a discrepancy that should be fixed in the template itself (this file or AGENTS.md is out of date, a rule is missing, something bit this repo and would bite the next), open an issue upstream in [`ptr727/ProjectTemplate`](https://github.com/ptr727/ProjectTemplate) rather than only fixing it locally - see [ProjectTemplate AGENTS.md "Staying in Sync and Reporting Drift Upstream"](https://github.com/ptr727/ProjectTemplate/blob/main/AGENTS.md#staying-in-sync-and-reporting-drift-upstream).
