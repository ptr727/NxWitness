# AUDIT.md

How an agent audits a repository against the fleet ground truth in the hub and reports drift. This is the procedure. The ground truth it checks against is [`registry/repos.json`][repos], the [`spec/`][spec] manifests, [`repo-config/`][repo-config], and the prose authorities ([`GOVERNANCE.md`][governance], [`CODESTYLE.md`][codestyle], [`WORKFLOW.md`][workflow]). The audit is read-only: it produces a report under [`reports/`][reports], never edits the target repo.

The verdict vocabulary is [`WORKFLOW.md`][workflow]'s: **operational / not operational**, **N/A**,
**defect**, and the applicable/absent rule. Do not invent a parallel scheme.

**This file measures. It does not decide the order a finding is applied in.** Section 10 states that converging is a separate phase and how a fix ships. [`RESYNC.md`][resync] is that phase for a repository that is already stood up and has fallen behind. It sequences the remedies so the rules land before the files they govern and a deletion lands before the re-vendor that would otherwise refresh it.

```mermaid
flowchart TD
  s0m["0m: fleet membership, every owned non-fork repo has a registry entry"] --> s0["0: has the repo been stood up? if not, STANDUP.md"]
  s0 --> s1["1: scope, ground-truth branch (main)"]
  s1 --> s2["2: resolve the repo's type(s)"]
  s2 --> s3["3: applicability gate, per item or check"]
  s3 --> s4["4: per-dimension checks, letter and intent"]
  s4 --> s5["5: assert Actions implement WORKFLOW.md"]
  s5 --> s6["6: validate settings, rulesets, secrets"]
  s6 --> s7["7: verdict model"]
  s7 -->|"every applicable check passes"| operational["operational"]
  s7 -->|"a letter miss, intent satisfied"| drift["drift finding"]
  s7 -->|"letter and intent both miss"| defect["defect: not operational"]
  s7 --> s8["8: report, under reports/"]
```

## 0. When to Run and What "Done" Means

**Start here only if the repo already carries its instruction set.** This file measures a repo against the fleet ground truth, and measuring assumes the thing being measured arrived. A repo holding no carried files, or a partial set, has a baseline that never arrived rather than drift to report. So it goes to [`STANDUP.md`][standup] sections 1A and 2 first and comes back here afterwards. The `AGENTS.md` "Fleet Bootstrap" section states that routing in the repo itself, byte-locked, so an agent finds it without knowing this file exists. Running the audit against a repo with nothing to audit produces a report that is all absences. That report reads as a catastrophic result rather than as a repo that was never stood up.

This audit is not occasional. Run it whenever you **create, adopt, or materially change** a fleet repo, and on demand for any known repo:

- **A full sweep opens with a fleet membership check, not a per-repo one.** `spec/audit.py`, run with no repo names, first lists every non-fork repository the registry `owner` actually owns on GitHub and diffs it against `registry/repos.json`. A repo that exists but carries no entry is invisible to every other check in this file. That is because all of them iterate the registry and never look past it. So this is the only place that gap is caught. The check also reconciles one field: a registry `status: "archived"` must agree with GitHub's own archived flag, in either direction. A name-filtered run or `--issue` skips it, since those are scoped to repos already known to the registry. Run this as `gh auth login` for the owner's own account. That is because a fine-grained PAT scoped to "selected repositories" returns an incomplete list with no error. So the sweep would read clean while some repos were never inspected.
- **Onboarding a repo is complete only when it either passes this audit or has a hub-committed `reports/<repo>/audit.md` plus a tracking issue** enumerating every residual delta. Passing means operational on every applicable check. A repo that is partially set up but never audited is itself a **defect**, the exact state this process prevents. The create-to-conformance counterpart is [`STANDUP.md`][standup]. Because both read the same manifests, a repo stood up by that file passes this audit by construction.
- **Touching a repo** (any conformance-affecting change) ends by re-running the applicable checks and **reconciling the registry entry to reality**: `status`, `types`, `releaseTrigger`, `workflowModel`, `driftNotes`. The registry records reality, not intent. [`spec/validate.py`][validate] proves the catalog is self-consistent, not that it matches the live repo. Closing that gap is this audit's job. The deterministic subset is mechanized in [`spec/audit.py`][audit-runner]. It covers settings, rulesets, secret names, file presence, and file names. It also covers per-scope Markdown section presence, workflow interface conformance, verbatim content, hub-hosted files a repo carries, and branch facts. The runner is owner-initiated, run on demand when onboarding a repo, on suspected drift, or before fleet-wide changes. A required section missing from a carried Markdown file is a **drift finding**, not a letter. That is because a heading rename reads as missing and equivalence is judged by hand. A carried `interface` workflow (spec/fidelity-model.md) is checked by name and wiring (required jobs, the ruleset-bound check name, the artifact-name handoff, and the forbidden `artifact-ids:` fork). All of those checks are at **drift**, since the body is owned and a rename is a hint to verify. A carried `verbatim` unit is content-hashed against the hub's canonical after line-ending normalization. Such a unit is either a whole file (`.markdownlint-cli2.jsonc`) or a canonical workflow job region (the `github-release` job). A mismatch is classified **stale** (matches a past hub revision, re-vendor) or **modified** (matches none, the repo changed fixed content). Both are at **drift**, since equivalence is intent-governed and a byte diff is a hint to review. A carried `intent` unit gets one advisory beyond presence, a last-modified comparison. A hub canonical changing after the copy's own last commit marks the copy as possibly trailing, at **drift**. That is a hint rather than proof, since a copy touched without reconciling reads current and content is never judged. A content scan also looks for a version literal. Such a literal is a three-part version, a full 40-character commit SHA, or an abbreviated one of 7 to 12 characters mixing digits and letters. One found in `AGENTS.md`, `GOVERNANCE.md`, `CODESTYLE.md`, or `WORKFLOW.md`, outside the verbatim sections downstream and across the whole file in the hub, is a **drift** finding. That is because a pin's value in prose goes stale at the next bump and every other literal reads like one.

**Verify the host before running any hub tool.** The tools carry version floors. A host below one answers `--version`, looks healthy, and produces a wrong answer. So a clean audit run from a broken host is a clean-looking result rather than a result.

```shell
python3 scripts/host_gate.py --repo "<path-to-target-checkout>"   # run from a hub checkout, floors from spec/host-tools.json
```

Pass `--repo`, since the gate reads the target's own `host-tools.json` relative to it and defaults to the working directory. Omitting it does not read the target's declaration at all, so every floor that repo adds goes unapplied. The run reports nothing about the omission. A finding is a **host** misconfiguration rather than a repo one, and [`docs/host-setup.md`][host-setup] is the contract it checks.

## 1. Scope and Ground-Truth Branch

Audit one repository at a time. Read the target's **`main` branch** as ground truth: `main` is the released, authoritative state. Read `develop` only to detect divergence. A stale or diverged `develop` (behind `main`, or diverged) is reported as a **drift finding**, never audited as the truth. Do not treat a `develop`-only file as present if it is absent on `main`.

This holds for **both workflow models**. An `operational` repo commits directly to `develop`, but its ground truth is still `main`, the promoted and gated snapshot the promotion PR blesses. `develop` there is mid-flight by design (ungated direct pushes), so auditing it would measure work in progress. Conformance scaffolding that has landed on `develop` but is not yet promoted is *un-promoted work*, not a conformance defect. It counts when it reaches `main`. A registry `groundTruthBranch` naming `develop` therefore contradicts this section, for either model.

## 2. Resolve the Repo's Type(s)

Look up the repo in [`registry/repos.json`][repos]. An entry with status `archived` or `excluded` is out of scope for the rest of this procedure. So stop here rather than proceeding to section 3. `archived` means GitHub itself reports the repo archived, so no further conformance work applies. `excluded` means a maintainer decision took it out of audit scope, recorded in the entry's `exclusionReason`. Both still carry a registry entry precisely so the decision stays visible, per section 0's membership check, rather than the repo reading as an oversight.

Otherwise read its `types[]`. If the entry is `classificationPending` (a backlog repo), classify it from the tree and propose a registry update:

- `*.csproj` / `*.slnx` -> `csharp`, a `dotnet nuget push` workflow -> `nuget`, a `System.CommandLine` console -> `console`.
- a tracked `.py` file other than the carried hook helper `hub_fetch_run.py` or its former name `hub-fetch-run.py` -> `python`, a `pypa/gh-action-pypi-publish` workflow -> `pypi`.
- `Dockerfile` + a docker build/push workflow -> `docker`, an `upstream-version.json` tracker -> `upstream-wrapper`.
- `custom_components/*/manifest.json` + `hacs.json` -> `homeassistant`, a codegen workflow -> `codegen`, no `build-*` task -> `source-only`, governance-only -> `docs`.
- `hugo.yaml` / `hugo.toml` / `config/_default/hugo.yaml` -> `hugo`. A repo may carry it alongside `source-only`, since a site deploy leaf is not a `build-*` task and both declarations stay true.
- a carried `lib/fleet/sh/secrets.sh` -> `host-secrets`, and so does a shell script the repo runs that reads a credential file under `.secrets/` or `~/.secrets/`.

## 3. Applicability Gate

Reuse [`WORKFLOW.md`][workflow] section 1, extended to this audit's own checks: an item or check that governs a construct the repo does not contain is **N/A**. Record it as N/A and **exclude it from the verdict**. N/A is never a defect. A Docker check on a repo with no image is N/A. A NuGet check on a Python package is N/A too. So are the artifact-lifecycle clauses on a source-only repo.

Which carried files and sections a repo is expected to have is decided by its scope selectors. Those are its type(s) plus workflow model, release trigger, and consumer model. The scope model and the `appliesTo` selector vocabulary are defined in [`spec/scope-model.md`][scope-model].

## 4. Per-Dimension Checks (Letter and Intent)

For each applicable type in [`spec/project-types.json`][project-types] and every cross-cutting dimension, evaluate each check at its stated verdict tier:

**Every check under a project type is judged by hand. The cross-cutting dimensions are only partly mechanized, and the line between the two halves is not where a reader assumes.** [`spec/audit.py`][audit-runner] evaluates **no** check belonging to a type in `spec/project-types.json`. It reads that file for one purpose only. That purpose is to resolve the id a registry `driftNote` names (section 8) against the catalog and against the repo's declared types. An id is resolved against the runner's own checks in `spec/checks.json` too. Resolving a catalog id is not running the check it names. What the runner does mechanize is the deterministic subset in section 0. That subset lands on several `crossCutting` checks without being organized by them. It covers branch protection and the ruleset diffs, secret names, and Dependabot ecosystems. It also covers the cspell single source, section presence, file names, and `driftNotes` freshness. So read a clean run precisely. It is evidence for that subset, it is **no** evidence for any of a type's checks, and it is partial evidence across the cross-cutting dimensions. The three are easy to conflate. That is because adding a check under a type changes what an auditor must judge and changes no tool's output. So the check reports nothing until someone evaluates it, and silence from a tool that was never looking reads exactly like a pass. Cite the `file:line` each check was judged against, since that citation is the only durable record that the judgment happened.

- **letter** - the exact file, section, config, or construct is present.
- **intent** - an equivalent outcome holds even if the form differs.

A check with `intentRef`/`workflowRef` points at the prose section that owns the rationale, so read it to judge intent. The dimensions:

- **csharp** - `.editorconfig` carries the shared `[*.cs]` rule block (letter), and analyzer severities are enforced, not relaxed (intent).
- **nuget** - `nuget.publish.oidc` (intent): publish uses OIDC Trusted Publishing with no stored `NUGET_API_KEY` secret, from a job in the publishing repository's own publisher. That job is never inside a build leaf and never in a reusable workflow a different repository hosts. [`WORKFLOW.md`][workflow] section 3's `Output Seam by Destination` gives the reason for that placement, for both package registries. `nuget.publish.skipduplicate` (letter): the push carries `--skip-duplicate` and is gated on the publish decision, not on an existence check. `nuget.publish.job` (letter): that publish job declares `id-token: write` and `actions: write`, and consume-then-deletes `nuget-build-<branch>`.
- **pypi** - `pypi.publish.oidc` (intent): OIDC publish with no stored token, from a job in the publishing repository's own publisher. That job sits under the same seam the **nuget** check names. `pypi.publish.environment` (letter): that job declares `environment: pypi` and `id-token: write`, with `skip-existing: true`.
- **python** - ruff and a CI type checker present (intent), strict in a build-profile directory (pyright, mypy, or both), and canonical in `pyproject.toml` (letter). A standalone `.ruff.toml` / `pyrightconfig.json` / `mypy.ini` / `.mypy.ini`, or a `setup.cfg` `[mypy]` section, is a drift finding.
- **dotnet-publish** - `dotnet-publish.smoke.subset` (letter): the smoke runtime matrix is a strict subset of the full set. `dotnet-publish.release.asset` (letter): the per-runtime outputs aggregate to one `release-asset-*`, gated `!smoke`.
- **docker** - registry layer cache (`buildcache-<branch>`, never `type=gha`). The size-limited Docker Hub README is published via the docker-readme task. The image always re-pushes on publish.
- **hugo** - the build fails on a generator warning, the URL-parity gate asserts a length floor before comparing, and the rendered output is untracked. The generator is pinned by version and checksum and declared once, and a vendored tree records its upstream ref. The deploy asserts what the host serves (the release id and the environment). Retention is bounded by a declared count with one side recorded as owning the prune. That side is the deploy where its credential can observe the destination and the host where that credential is confined write-only. So grade which shape the repo uses rather than looking for a prune step. Deploy credentials are per-environment, which `spec/secrets.json` cannot express, so a clean **repo-setup** verdict says nothing about whether the environments are configured.
- **branch-model** - `main` and `develop` both exist and are protected. The live rulesets match the payloads `repo-config/configure.sh` composes for the repo from [`repo-config/`][repo-config], by normalized diff (below).
- **carried-scope** - the repo carries no file the hub hosts rather than carries. The set is derived, not listed: the hub's git-tracked paths minus the [`spec/files.json`][files] baseline and every hub path a `trees` entry covers. A `trees` entry covers only the hub paths below its `source` that its `include` patterns match. A file dropped from the manifest is therefore reported on the next run, with no retirement list to remember to edit. The manifest's `repoOwnedTrees` entries name hub directories no repo carries, such as `tests`, and every hub path below one is left out of the set. The remedy is the opposite of every other file finding, a **deletion**, since the repo reaches the hub's copy per [GOVERNANCE.md "Hub-Hosted Tooling"][governance-hub-hosted-tooling]. The match is on path alone, so a hit is a candidate and not a verdict. A repo's own content at a path the hub also uses matches while carrying nothing of the hub's. The first fleet run showed such a match twice, a KiCad tooling doc at `scripts/README.md` and per-repo formatting hooks at `.husky/pre-commit`. A [`spec/divergences.json`][divergences] `gaps` disposition decides which case a hit is. So only `retire` asserts a deletion, `accepted` closes a collision or a repo-owned file, and an untriaged hit is read before it is acted on.
- **verbatim-tree** - every applicable `trees[]` declaration in [`spec/files.json`][files] owns its target tree. The audit reports missing files as letter findings, stale or modified bytes as drift, and extra files under a pruned target as drift. An unreadable or truncated repository tree is undecided and produces drift rather than a clean result.
- **repo-setup** - every required secret for the repo's publish mechanisms is configured, and no forbidden secret is present (per [`spec/secrets.json`][secrets]).
- **runtime-secrets** - a repo-scoped runtime-secrets directory, when present, is named `.secrets/` (dotted, not a bare `secrets/`), and a single opaque credential file carries no extension. Every real secret file has a tracked `<name>.example` beside it cataloged in `.secrets/README.md`. Those parts are N/A for a repo with no such directory. See [GOVERNANCE.md "Repo-Scoped Secrets"][governance-repo-scoped-secrets]. Every env and secret file the repo names, tracked or not, follows the env and secret file rule of [CODESTYLE.md "Script and Utility File Names"][codestyle-file-names]. `spec/audit.py` reports a tracked name that breaks the rule's form at **drift**. It reads each file directly in a `.secrets/` directory and each `.env` file elsewhere whose name does not open on a dot. It sets a trailing `.example` aside first, and the directory's own `README.md`, `.gitignore`, and `.gitkeep` are not secret files. The rest is judged by hand. A hyphen joins words only inside a host field, which takes the DNS label form. The check passes a hyphenated word in any field, since it cannot tell which field it is. A host field is a short name, never a fully qualified one. The check cannot tell an extension from a dot-joined last field of an extensionless name, a two-label host included. A consumer limit applies only where a name is used as a Docker secret or maps to a variable. A host-side name is judged where the repo's docs and scripts spell it out, never by reading a host. An `ENV_FILE` path and a compose `env_file` are such places. Renaming a file on a host is the maintainer's step, listed in the resync pull request.
- **file-naming** - each script file takes the stem its extension sets, per [CODESTYLE.md "Script and Utility File Names"][codestyle-file-names]. A `.py` stem is lowercase snake_case. A `.sh`, `.ps1`, `.cmd`, or `.bat` stem is lowercase-hyphenated, and so is the name of an extensionless executable with a shebang. The extension is lowercase too. The text after a name's last dot is its extension, matched whatever its case. A name with a dot and none of those extensions is not judged here, whatever its mode. So an executable dotfile with no such extension, such as `.Hidden_Hook`, and a dotted name such as `Run_Me.v2` go unjudged. A launcher with an extension beside a `.py` of the same stem keeps that stem. `spec/audit.py` reports a name that breaks its pattern at **drift**. It reports a file the patterns cannot decide for judgment instead. One is a launcher whose `.py` sits in another directory. Another is an extensionless launcher beside its `.py`, which may be an installed command named `fetch-report` rather than `fetch_report`. A third is an extensionless executable with no shebang or no readable text. Pester's `.Tests.ps1` suffix is the one tool-fixed name the patterns know. Check a reported break against the externally fixed exemption before renaming it. Record any other tool-fixed name as a registry `driftNote` naming `(filenaming.scripts)` and each exempt path as a code span, such as `` `tools/Install.ps1` ``. The audit then skips each named path and no other. It reports a named path it no longer flags, or a note naming no path, as a stale exemption. The carried hook helper still under its former name `hub-fetch-run.py` is a break here. The names kept for Python-type discovery are no naming exemption.
- **linter-parity** - one config per linter (`.markdownlint-cli2.jsonc`, `cspell.json`, ruff/pyright, editorconfig/csharpier, actionlint) drives the CLI, CI, and the editor extension where the workspace recommends one. CI runs each. That count is of the root config, so a nested `.markdownlint-cli2.jsonc` is not a second one. A local hook exists and runs at minimum the diff-scoped prose gate and the eol check via `hub_fetch_run.py` (`parity.hooks`, intent). For the hub repo itself it uses the hub's own local script copies instead. The helper's former name `hub-fetch-run.py` counts too. A repo with none wired is a defect, and one mid-convergence on the language-formatting half stays operational.
- **recurring-violations** - comments concise and non-narrative, ASCII only (no em-dash, no smart quotes) apart from the letters of a recorded name. These checks also include US spelling and line endings per `.editorconfig`. These are frequent regressions, so this dimension is high priority and always runs, and each check is grep-able (see below).
- **readme-structure** - the README follows [`spec/readme-structure.md`][readme-structure] (applicable sections, in order). Mechanically checked against the declared model in [`spec/readme-sections.json`][readme-sections]: required sections present, declared sections in their relative order, and `License` last. Also mechanically checked against that model are the shields each deliverable implies and the license shield in the closing License section. The tagline and its mirrors are checked the same way. A heading the model does not name is dropped before the order comparison, so a repo-specific section is never a finding.

## 5. Assert the Actions Implement WORKFLOW.md

Run [`WORKFLOW.md`][workflow]'s methodology against the repo's **own** Actions, reading a workflow it only calls at the SHA it pins. The methodology covers the 5A static audit (structural facts per applicable D-guarantee, each cited in the form 5A sets out). It also covers the 5B trace scenarios (predicted run/skip + version + release + artifact-end-state vs expected). The contract in WORKFLOW.md section 4 is satisfied by **outcome**, not by matching the catalog snippets in [`catalog/snippets/workflows/`][workflows] byte for byte. Those are the reference implementation, not required bytes. Where a guarantee names a construct, D6.1's `release-asset-<branch>-<target>` and D9.2's ruleset-bound job `name:` among them, that name is the outcome. A divergence is a **defect** here. That is a separate judgment from the `verbatim` content hash section 0 describes. That hash classifies a mismatch as stale or modified and reports either at **drift**. That is because equivalence is intent-governed and a byte diff is a hint to review rather than a verdict.

## 6. Validate Settings, Rulesets, and Secrets

- **General settings, the registry description, labels, rulesets, the Dependabot security features, the fleet project link, and deployment environments**. Fetch the hub and check out `main`. Run `repo-config/configure.sh check "<owner>/<repo>" "<release|operational>"` from that checkout. Pass the target repository and its registry `workflowModel` explicitly. The token needs project access as well as the admin the ruleset endpoints require. That is because a token without it cannot read the project link, and the command reports that group as failing rather than as clean. The command's own error names the scope to grant. The host needs a runnable `py -3` or `python3` too. That is because a run against a hub checkout resolves the registry description through [`spec/resolve_description.py`][resolve-description] and exits before the first check when neither interpreter answers. The command checks what `configure.sh apply` writes. That covers the declared settings, the derived settings and the registry description, the declared labels, and the Dependabot security features. It also covers the shared `main` ruleset, the `develop` ruleset the model selects, and the link to the fleet project the hub declares. It reports the live `bypass_actors` list without asserting it, because bypass authority is a per-repository human decision. It also checks one group apply never writes, the deployment environments the registry's `environments` declares for the repo. It checks that each one exists and that its deployment-branch policy is the form declared. It also checks that under a `custom` policy the entries it allows are exactly the declared branches. So anything added by hand reads as drift. An environment the registry declares nothing about is reported rather than asserted, since GitHub creates some on its own. Neither this command nor the **Secrets** bullet below queries an environment's own secret and variable stores. So a clean run says nothing about whether an environment holds what a deploy needs. In those stores the API exposes a secret's name but not its value, and a variable's name and value both.

- **Secrets** - from the same hub checkout, run [`spec/audit.py`][audit-runner] as `uv run --no-project --script spec/audit.py "<RepoName>"` and read the findings it prints with a `secrets:` prefix. That argument is the registry entry name rather than the `<owner>/<repo>` form `configure.sh` takes, and an `<owner>/<repo>` argument prints `Not cataloged` and exits 2. The runner reads both the Actions and the Dependabot store on every repo. A token that cannot read either one fails that repo's whole run with an error rather than reporting names as missing. It resolves what each store must hold from the hub's own [`spec/secrets.json`][secrets] plus the registry entry's `publish[]`/`types[]`/`requiredSecrets[]`. That file's `baseline` requires its names in both stores for every fleet repo. A mechanism adds to a store only where the mechanism's own `stores` list names that store. A declared type claims nothing where the entry's profile for it reads `lint-only`. It claims no coverage token where the tree carries no tests for it. So a repo declaring a language can still owe nothing beyond the baseline. It reports three things. They are a required name missing from a store and a forbidden name present in either store. The third is a configured name outside its store's required set, which the baseline and the applicable mechanisms make up. The entry's `requiredSecrets` adds to the Actions store alone. A claim is per store, so a name only the Actions store owes is reported when the Dependabot store holds it. A forbidden name present draws the third as well as the second, since each store's claimed set is built from its required names alone. Names are read, never values. A shallow hub clone fails the whole run, so no `secrets:` line prints, per section 8.

- **Dependabot ecosystem coverage** - for each ecosystem the repo's tree implies, `.github/dependabot.yml` must declare it. That means `github-actions` when `.github/workflows/` holds at least one `.yml` or `.yaml` entry. Those workflows reference actions, and otherwise those versions go stale and a stood-up merge-bot has no action-update PRs to auto-merge. `devcontainers` must likewise be declared when a `.devcontainer` is present. The mechanical check (`spec/audit.py`) asserts each implied ecosystem's **presence**, and it runs at all only where `.github/dependabot.yml` exists and is non-empty. A missing `dependabot.yml` is a file-presence letter of its own. An empty one is caught by neither check, since the file-presence check reports only absence. So an empty file is reported by hand. A tree-implied ecosystem that file declares nowhere is a **drift finding**. Then confirm **by inspection** that each declared ecosystem **dual-targets `main` + `develop`** per the [Branching Model][governance-branching-model]. That is because the regex below cannot pair an ecosystem with its `target-branch`. Language ecosystems (`nuget`/`uv`/`npm`) are directory-scoped and audited by inspection too.

  ```bash
  #!/usr/bin/env bash
  # Save and run this as a script rather than pasting it into a shell, since it exits rather than returns.
  # It prints one line per implied ecosystem, and its exit status reports whether the reads succeeded rather than whether an ecosystem is missing.
  set -Eeuo pipefail
  repo="<owner>/<repo>"
  ground=main # The branch section 1 reads as ground truth.

  root_paths=$(gh api "repos/$repo/contents?ref=$ground" --jq '.[].path')
  # A repo carrying no .github directory would 404 on the listing below.
  # That is an absence rather than the read failure that exit code otherwise means.
  github_paths=""
  if grep -Fxq .github <<<"$root_paths"; then
      github_paths=$(gh api "repos/$repo/contents/.github?ref=$ground" --jq '.[].path')
  fi
  has() { grep -Fxq "$1" <<<"$root_paths"$'\n'"$github_paths"; }

  if ! has .github/dependabot.yml; then
      echo "dependabot.yml absent: a file-presence finding, and ecosystem coverage is not checked at all"
      exit 0
  fi
  # The raw media type answers with the file itself, so nothing here decodes base64 or reads a content key that can be absent.
  dependabot_yaml=$(gh api "repos/$repo/contents/.github/dependabot.yml?ref=$ground" -H "Accept: application/vnd.github.raw")
  # The mechanical check gates on a non-empty file, so an empty one skips here too rather than reporting every implied ecosystem missing.
  if [ -z "$dependabot_yaml" ]; then
      echo "dependabot.yml empty: the mechanical check skips it and the file-presence check reports only absence, so report this one by hand"
      exit 0
  fi
  # Anchor to the line start (optional list dash) so a commented-out '# package-ecosystem:' is not counted.
  # Accept either quote or none, as the mechanical check does.
  # The trailing class is the mechanical check's own, so a value carrying a digit is read whole rather than truncated to its leading letters.
  eco_re="^[[:space:]]*-?[[:space:]]*package-ecosystem:[[:space:]]*[\"']?[[:alnum:]_-]+"
  # A file declaring no ecosystem at all matches nothing, and grep exiting 1 would otherwise abort the run under the header above.
  # The two greps are separate statements so that only a no-match is absorbed.
  # Chained through a pipe they are not.
  # That is because a failing first stage produces no output.
  # The second stage then no-matches, and pipefail reports that exit 1 rather than the failure.
  matched=$(grep -oE "$eco_re" <<<"$dependabot_yaml") || [ "$?" -eq 1 ]
  decl=$(grep -oE '[[:alnum:]_-]+$' <<<"$matched" | sort -u) || [ "$?" -eq 1 ]

  if has .github/workflows; then
      # The contents API answers with an object rather than an array where the path is a file.
      # So the type is tested rather than assumed.
      yaml_count=$(gh api "repos/$repo/contents/.github/workflows?ref=$ground" --jq 'if type == "array" then [.[] | select(.name | test("\\.ya?ml$"))] | length else 0 end')
      if [ "$yaml_count" -gt 0 ]; then
          grep -qx github-actions <<<"$decl" && echo "github-actions: present" || echo "github-actions: MISSING (.github/workflows/ holds a workflow file)"
      fi
  fi
  if has .devcontainer; then
      grep -qx devcontainers <<<"$decl" && echo "devcontainers: present" || echo "devcontainers: MISSING (.devcontainer present)"
  fi
  # Then read dependabot.yml and confirm each present ecosystem has both a main and a develop target-branch entry.
  ```

- **Dependabot on self-hosted runners (owner setting)** - the owner-level toggle named `Dependabot on self-hosted runners` routes Dependabot's own update jobs to a self-hosted runner pool. For a user-account owner it sits at `https://github.com/settings/security_analysis`. With none registered on the owner, those jobs queue for up to 24 hours, then get cancelled, and ordinary CI is unaffected throughout. GitHub routes only a private repo through this setting. That is why the fleet's public repos updated throughout while its private ones stalled. So read the audited repo's visibility first with `gh repo view "<owner>/<repo>" --json visibility`. Then treat a public one as N/A. On a private repo, read the newest Dependabot run's job:

  ```bash
  #!/usr/bin/env bash
  # Save and run this as a script rather than pasting it into a shell, since it exits rather than returns.
  set -Eeuo pipefail
  repo="<owner>/<repo>"

  # Dependabot's own update jobs run under the dynamic event rather than from a workflow file, so they are selected by path.
  # Read the newest run rather than the history.
  # That is because a repo whose account setting has since been turned off keeps every run cancelled while it was on.
  # The dynamic event is shared with GitHub's other generated runs, Copilot's reviewer among them.
  # On an active repo those fill whole pages, so the pages are walked rather than the first alone.
  # The walk stays within the 1000 runs this endpoint returns at most.
  # The run's date is carried out with its id so the report can say when this repo's updates last ran.
  # The finding below does not turn on that date.
  runs=$(gh api --paginate "repos/$repo/actions/runs?event=dynamic&per_page=100" --jq '.workflow_runs[] | select((.path // "") | startswith("dynamic/dependabot")) | "\(.id) \(.created_at)"')
  if [ -z "$runs" ]; then
      echo "no Dependabot run in this repo's dynamic run history"
      exit 0
  fi
  # The endpoint answers newest first and paginates in that order, so the first line is the newest Dependabot run whichever page it landed on.
  run=$(head -n 1 <<<"$runs")
  echo "newest Dependabot run: $run"
  gh api "repos/$repo/actions/runs/${run%% *}/jobs" --jq '.jobs[] | "\(.conclusion) labels=\(.labels | join(",")) runner=\(.runner_name // "") steps=\(.steps | length)"'
  ```

  A job reading `labels=dependabot` with no runner name was routed to the pool, its empty step list saying it never started. By contrast, a job GitHub's own runners took reads `labels=ubuntu-latest` with a runner name and the steps it ran. Those four fields together are the finding. A newest run that was routed and then cancelled having run no step says this repo's Dependabot updates last failed that way. It says they have not succeeded since. That is because a later success would be the newest run instead. Why they are still failing is not something the API answers. That is because all of the following sit outside it. They are the toggle's own state, whether a matching runner is registered and online, and whether the queue was ever restarted. The audit reports the repo and that run's date, and leaves the choice of remedy to the maintainer. Detection stops there, like the rest of this audit. Every remedy here is an owner-level or web-UI action no target-repo pull request can carry. So this is reported rather than converged under section 10. The remedies are to turn the toggle off, along with `Automatically enable for new repositories` beside it, or to bring a matching self-hosted runner online instead. Disabling the toggle reruns nothing already queued. So an affected repo also needs its own manual `Check for Updates` click on its own Dependabot page. A repo whose toggle is already off needs only that click.

## 7. Verdict Model

Per dimension, record `operational | not-operational | N/A`, each with a letter verdict and an intent verdict:

- letter miss but intent satisfied -> **drift finding** (equivalent outcome in a non-standard form, worth fixing, not a break).
- letter and intent both miss -> **defect** (not operational).

A repo is **operational** only if every applicable check passes. A single applicable defect makes it not operational, regardless of how clean the rest looks. N/A items are excluded, never counted as failures.

An intent staleness advisory (`files.intent`) verified with nothing to adopt is recorded, not left standing. Record it as a registry `driftNote` naming `(files.intent)`. Name each verified copy as a code span joining its path and the hub sha the finding cites, such as `` `.editorconfig@<sha>` ``. The audit then clears that copy until its intent canonical changes again. It reports as stale an acknowledgement the advisory no longer raises at that sha, and a note naming no such span.

## 8. Report

Write `reports/<repo>/audit.md` from [`reports/_template.md`][template]. The report is a dimension x {letter, intent, verdict, evidence} table with `file:line` citations (WORKFLOW.md 5A style). It also has a drift section, and a list of proposed registry/spec updates (e.g. a resolved `classificationPending`). Rank findings most severe first.

**The hub authors the report, and a downstream repo does not open a pull request against the hub to write its own.** `reports/` is the hub's evidence that it audited a repo, so a report written by the repo being audited is a claim rather than evidence. The hub cannot adopt one without checking it. Checking the judgment dimensions **is** the audit, since confirming a verdict like "analyzers enforced" means reading the same files the audit reads. So a submitted report saves only the writing up and not the work. A submitted report is also stale by construction, because it is a snapshot of one hub revision arriving at a later one. Its claims then have to be reconciled against findings that did not exist when it was written.

A downstream repo uses its context where it is worth most. It **files findings about the hub as issues against `ptr727/ProjectTemplate`** and **applies fixes to its own repo** per section 10. Filing an issue is the opposite of self-certification, and it is how several hub defects have been found. Hub findings include bugs, conflicting sources, unclear or incomplete instructions, missing capabilities, and Copilot findings about any of them. Search open and closed issues first, then update the matching issue or file a new one. Any pull request it opens against the hub follows the standard branching model. It targets `develop`, never `main`.

**Findings are a point-in-time snapshot. Stamp them and re-verify before acting.** [`spec/audit.py`][audit-runner] prints a run stamp (`audit run <UTC> | hub <sha>`) and, per repo, the exact commit it read (`@ <branch>@<sha>`). Anything derived from a run (a report, and especially an **onboarding or conformance issue**) quotes that stamp. That way a reader can tell whether it still applies. A convergence issue is generated from the audit, never composed by hand. `uv run --no-project --script spec/audit.py --issue <repo>` emits a ready-to-file title and body from that repo's live findings (grouped into must-fix, converge, and could-not-verify). So the issue content cannot drift from what the audit actually found and regenerates as the repo changes.

**Verify a convergence before it is promoted with `--branch`.** `uv run --no-project --script spec/audit.py --branch <ref> <repo>` reads that ref instead of the repo's registry `groundTruthBranch`. So a repo can audit its own `develop` while the work is still in flight rather than discovering the gaps after `main` has moved. The registry is not edited, and the run is still read-only. The run stamp names the override so a finding cannot be mistaken for one against ground truth. A ref that does not resolve is a single error naming it, never a baseline's worth of file-absent letters. A ref off the grammar a registry `groundTruthBranch` is held to, `GROUND_TRUTH_BRANCH_PATTERN` in [`spec/validate.py`][validate], is refused before anything is read. The run then exits `2` with one line on stderr naming the ref.

**Re-running the audit needs a full hub clone with git history.** The verbatim stale-vs-modified classification and the intent staleness advisory walk the canonical's history (`git log` / `git show` from the hub root). A shallow hub clone cannot answer "matches a past hub revision". In one, `spec/audit.py` reports a single ERROR per repo and exits non-zero. `spec/audit.py --issue` exits 2 with the error on stderr. `spec/fidelity_honesty.py` stops before writing a report. Each error says to run `git fetch --unshallow origin` in that checkout. A downstream agent verifying one finding without the full history can instead compare against the current hub canonical on `main`. That canonical is the whole file for a file-level unit, or the named `## heading` block for a verbatim section. The comparison decides current-match but not stale-vs-modified. An agent picking up such an issue **re-runs the audit first and acts on the live result, not the pasted findings**. A repo moves between filing and pickup, so a stale block leads an agent to "fix" what is already fixed. Examples are re-requesting secrets that exist and attempting a no-op forward-sync. State the findings as evidence for *why* the issue was filed, never as the current state.

**Reconcile `driftNotes` in the same pass.** A registry `driftNote` records a *current* deviation from the baseline. Once the deviation is resolved the note is deleted, not left describing finished work, since hand-maintained prose drifts silently otherwise. `spec/audit.py` flags two shapes of note, neither of them gated on the rest of the audit being clean. A note asserting outstanding work in prose ("pending", "not yet", "missing", "behind", ...) is contradicted outright by a clean audit. Where findings are open it is raised as a question of which one it means. It is raised because gating the check on a clean audit meant one standing finding a repo could not clear exempted its whole note list. It is also raised because the repo carrying open findings is where a stale note is most likely. A note naming the check that would retire it, as an id in parentheses, is the mechanically checkable shape. Such a note is matched with the parentheses (`(hugo.generator.pinned)`, the bare id is not detected). It is surfaced on **every** run. The audit resolves the id against the catalog and confirms the repo declares its type. It then hands the check itself to the auditor, since section 4 above is judged by hand. **So a note naming a check id is retired by a person, not by a run.** For an id only `spec/checks.json` defines, the finding says the audit evaluates it, and the note goes once that check reports nothing. Write it that way anyway. The id says exactly what would close the note. The surfaced finding puts that decision in front of whoever runs the audit rather than leaving the note to sit until someone rereads it. A note naming a suppression id is not surfaced this way, since it suppresses a finding rather than naming a check to judge. The check it suppresses reports it once stale. A note naming `(python.directories.declared)` on a repo without the `python` type suppresses the Python discovery advisory. It is stale once no `.py` file remains. A note naming `(filenaming.scripts)` exempts each path it names as a code span from the file-naming dimension. A named path the check no longer flags is stale, and so is a note naming no path. A note naming `(files.intent)` acknowledges a verified intent advisory, per section 7. The pending-marker check above still reads each of them.

## 9. Escalate

Surface spec questions rather than resolving them silently, for example the Python config-placement canonicalization, or a new construct no type covers. A repeated letter miss that many repos share is a signal the spec (not each repo) needs adjusting, so raise it.

```mermaid
flowchart LR
  finding["a finding from 7: Verdict Model"] -->|"the same miss, many repos"| s9["9: Escalate, fix the spec instead"]
  finding -->|"one repo"| s10["10: Converge, branch + fix + PR"]
  s10 --> review["review loop to green"]
  review --> merge["maintainer merges"]
  merge --> reaudit["re-audit, the hub commits the report"]
  s9 --> reaudit
```

## 10. Converge: Apply the Fixes

Sections 0-9 (the audit and its report) are **read-only** and never touch the target. **Converging** is the separate follow-on phase: the drift the report found is **resolved by applying fixes to the target repo**, not left as a report. The convergence loop:

- **Apply via a pull request on the target repo.** Branch from the target's `develop` (or `main` for a `main`-only repo), make the fix, and open a PR. Never push a fix directly to a protected branch, and never hand-edit a target outside a PR.
- **Drive the PR's review to green** - the same loop the hub runs (see [GOVERNANCE.md "PR Review Etiquette"][governance-pr-review-etiquette]). The [Copilot review runbook][copilot-runbook] in `.github/copilot-instructions.md` covers that one reviewer's mechanics. Request review at the defined moments. Address and resolve every thread from every reviewer the repo has configured, and confirm the review covers the head SHA.
- **Merge only with explicit maintainer approval.** The agent drives to green and stops. The maintainer merges.
- **One focused PR per drift class**, cross-referencing the audit finding. A sprawling all-drifts PR draws many review rounds and never feels done.
- **A `hub-only:` finding converges by deleting the file, not by updating it.** That prefix is how `spec/audit.py` reports the **carried-scope** dimension section 4 names. It is the one class where the fix removes content. So it is easy to convert into a re-vendor by reflex and end up refreshing a copy that should not exist. Delete the repo's copy and reach the hub's per [GOVERNANCE.md "Hub-Hosted Tooling"][governance-hub-hosted-tooling]. Confirm the disposition is `retire` before deleting anything. An untriaged hit may be the repo's own content at a shared path, and deleting that destroys work the hub never owned.
- **Any deletion sweeps the inbound references to the path, and the sweep is part of the deletion rather than follow-up.** This governs every removal and not only a `hub-only:` one. Grep the path tree-wide and read every hit. Then read the files whose job is to say what the repo holds because a path search cannot find a description that names no path. Point a link at the hub's copy when that is the equivalent. Rewrite a runnable command to the invocation that works. Remove a mention with no equivalent and remove its reference definition in the same edit, per [GOVERNANCE.md "Documentation Style Conventions"][governance-documentation-style].
- **Fix systemic drift in the hub, not per repo.** When many repos share a drift, fix the spec/rule (or add a machine check) here and let a re-audit re-flag it. This is instead of hand-patching each repo for the shared cause.

The convergence model: the hub audits and the agent **applies** the fixes via target PRs, and the maintainer gates every merge. It supersedes any "the hub only reports; downstream operators apply by hand" framing.

<!-- Workflow -->

<!-- Repo -->
[audit-runner]: https://github.com/ptr727/ProjectTemplate/blob/main/spec/audit.py
[codestyle]: ./CODESTYLE.md
[codestyle-file-names]: ./CODESTYLE.md#script-and-utility-file-names
[copilot-runbook]: ./.github/copilot-instructions.md
[divergences]: https://github.com/ptr727/ProjectTemplate/blob/main/spec/divergences.json
[files]: https://github.com/ptr727/ProjectTemplate/blob/main/spec/files.json
[governance]: ./GOVERNANCE.md
[governance-branching-model]: ./GOVERNANCE.md#branching-model
[governance-documentation-style]: ./GOVERNANCE.md#documentation-style-conventions
[governance-hub-hosted-tooling]: ./GOVERNANCE.md#hub-hosted-tooling
[governance-pr-review-etiquette]: ./GOVERNANCE.md#pr-review-etiquette
[governance-repo-scoped-secrets]: ./GOVERNANCE.md#repo-scoped-secrets
[host-setup]: https://github.com/ptr727/ProjectTemplate/blob/main/docs/host-setup.md
[project-types]: https://github.com/ptr727/ProjectTemplate/blob/main/spec/project-types.json
[readme-sections]: https://github.com/ptr727/ProjectTemplate/blob/main/spec/readme-sections.json
[readme-structure]: https://github.com/ptr727/ProjectTemplate/blob/main/spec/readme-structure.md
[repo-config]: https://github.com/ptr727/ProjectTemplate/tree/main/repo-config
[reports]: https://github.com/ptr727/ProjectTemplate/tree/main/reports
[repos]: https://github.com/ptr727/ProjectTemplate/blob/main/registry/repos.json
[resolve-description]: https://github.com/ptr727/ProjectTemplate/blob/main/spec/resolve_description.py
[resync]: https://github.com/ptr727/ProjectTemplate/blob/main/RESYNC.md
[scope-model]: https://github.com/ptr727/ProjectTemplate/blob/main/spec/scope-model.md
[secrets]: https://github.com/ptr727/ProjectTemplate/blob/main/spec/secrets.json
[spec]: https://github.com/ptr727/ProjectTemplate/tree/main/spec
[standup]: https://github.com/ptr727/ProjectTemplate/blob/main/STANDUP.md
[template]: https://github.com/ptr727/ProjectTemplate/blob/main/reports/_template.md
[validate]: https://github.com/ptr727/ProjectTemplate/blob/main/spec/validate.py
[workflow]: ./WORKFLOW.md
[workflows]: https://github.com/ptr727/ProjectTemplate/tree/main/catalog/snippets/workflows
