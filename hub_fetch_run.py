"""Fetch a ptr727/ProjectTemplate script fresh from `main` and run it through uv.

Never pinned or vendored: a pin nothing keeps current goes stale by construction.
CI (this repo's own, and the hub's) is the backstop for a change that lands broken on `main`.
A fetch failure fails the caller loudly rather than silently skipping the gate it guards.
The script runs as `uv run --no-project --script`.
The packages its PEP 723 block declares therefore resolve, beside the data files it reads, which are fetched from the same ref.
Usage: hub_fetch_run.py <hub-relative-path> [script-args...]
Example: hub_fetch_run.py .github/actions/prose-gate/prose_lint.py . --diff HEAD
"""

import os
import re
import shutil
import subprocess
import sys
import tempfile
import urllib.error
import urllib.request
from pathlib import Path

HUB_REPO = "ptr727/ProjectTemplate"
HUB_RAW_BASE = f"https://raw.githubusercontent.com/{HUB_REPO}"
HUB_MAIN_SHA_URL = f"https://api.github.com/repos/{HUB_REPO}/commits/main"
HUB_GIT_URL = f"https://github.com/{HUB_REPO}"
PROVENANCE_VAR = "PROSE_GATE_PROVENANCE"
PROSE_GATE_PATH = ".github/actions/prose-gate/prose_lint.py"
# The files a script reads or imports from the hub tree, fetched before it runs.
COMPANIONS = {
    PROSE_GATE_PATH: (".github/actions/prose-gate/prose-gate.json",),
    "scripts/docker_lint.py": (),
}
PROBE_TIMEOUT = 30
LOOKUP_TIMEOUT = 5
EMPTY_TREE = "4b825dc642cb6eb9a060e54bf8d69288fbee4904"


def lookup_via_git() -> tuple[str | None, str]:
    """`main`'s commit from `git ls-remote`, which spends no API quota, else None and the cause.

    Stderr is discarded rather than captured, so a remote helper git leaves behind on a timeout
    holds no pipe this process waits on.
    """
    try:
        probe = subprocess.run(
            ["git", "ls-remote", HUB_GIT_URL, "refs/heads/main"],
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            check=False,
            encoding="utf-8",
            errors="replace",
            env={**os.environ, "GIT_TERMINAL_PROMPT": "0"},
            timeout=LOOKUP_TIMEOUT,
        )
    except subprocess.TimeoutExpired:
        return None, "timed out"
    except OSError as exc:
        return None, type(exc).__name__
    if probe.returncode != 0:
        return None, f"exit {probe.returncode}"
    if m := re.fullmatch(r"([0-9a-f]{40})\trefs/heads/main\n?", probe.stdout):
        return m.group(1), ""
    return None, "unexpected output"


def resolve_main_commit() -> str | None:
    """Return the full commit hash `main` points at now, from the API or else `git ls-remote`.

    Every file a run fetches comes from that one commit, so a script and its data always pair.
    Where neither resolves, one line names both causes and the gate runs against `main` instead.
    That run fetches twice and refuses to run, if the two reads differ, a push having landed mid-run.
    A hook with no network cannot fetch the gate at all, and the CI gate is the enforcement.
    """
    token = (os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN") or "").strip()
    request = urllib.request.Request(
        HUB_MAIN_SHA_URL, headers={"Accept": "application/vnd.github.sha"}
    )
    if token:
        request.add_unredirected_header("Authorization", f"Bearer {token}")
    try:
        with urllib.request.urlopen(request, timeout=LOOKUP_TIMEOUT) as response:
            sha = response.read().decode(errors="replace").strip()
    except urllib.error.HTTPError as exc:
        reason = f"HTTP {exc.code}"
    except Exception as exc:  # noqa: BLE001
        reason = type(exc).__name__
    else:
        if re.fullmatch(r"[0-9a-f]{40}", sha):
            return sha
        reason = "unexpected response body"
    git_sha, git_reason = lookup_via_git()
    if git_sha:
        return git_sha
    print(
        f"hub_fetch_run: could not resolve the main commit (API {reason}, git ls-remote "
        f"{git_reason}), using main.",
        file=sys.stderr,
    )
    return None


def head_is_unborn() -> bool:
    """Whether HEAD is confirmed unborn (a real branch, no commits yet), never guessed.

    `git rev-parse --verify -q HEAD` exits 1 with empty stderr for a confirmed unborn HEAD,
    verified directly: a fresh `git init` with no commits gives exactly that signature.
    Any other shape is a probe failure to propagate, never a reason to guess at the scope.
    Those shapes include:
    - a non-git directory (exit 128, a `fatal:` message even with `-q`)
    - a missing or broken git executable (raised as OSError)
    - a hung probe (a timeout)
    - a permission error
    - any other failure
    """
    try:
        probe = subprocess.run(
            ["git", "rev-parse", "--verify", "-q", "HEAD"],
            capture_output=True,
            check=False,
            timeout=PROBE_TIMEOUT,
        )
    except (OSError, subprocess.TimeoutExpired) as exc:
        print(f"hub_fetch_run: could not run git to probe HEAD: {exc}", file=sys.stderr)
        sys.exit(1)
    if probe.returncode == 1 and not probe.stderr:
        return True
    if probe.returncode == 0:
        return False
    print(
        f"hub_fetch_run: git rev-parse --verify -q HEAD failed unexpectedly "
        f"(exit {probe.returncode}): {probe.stderr.decode(errors='replace').strip()}",
        file=sys.stderr,
    )
    sys.exit(1)


def resolve_unborn_head(argv: list[str]) -> list[str]:
    """Replace a `--diff HEAD` pair with git's empty-tree hash when HEAD does not exist yet.

    A brand-new repository's first commit has no HEAD to diff against.
    The fetched gate scripts refuse to widen to a whole-tree scan rather than report the backlog as new.
    The empty tree diffs cleanly against everything staged, which is the correct scope for a first commit.
    Any other `--diff` value passes through unchanged.
    """
    out = list(argv)
    for i, token in enumerate(out[:-1]):
        if token == "--diff" and out[i + 1] == "HEAD" and head_is_unborn():
            out[i + 1] = EMPTY_TREE
    return out


class FetchError(Exception):
    def __init__(self, url: str, cause: Exception) -> None:
        super().__init__(url)
        self.url = url
        self.cause = cause


def fetch_all(ref: str, paths: tuple[str, ...]) -> dict[str, bytes]:
    """Fetch each hub path at `ref` in order, raising FetchError naming the first that fails."""
    fetched: dict[str, bytes] = {}
    for path in paths:
        url = f"{HUB_RAW_BASE}/{ref}/{path}"
        try:
            with urllib.request.urlopen(url, timeout=30) as response:
                fetched[path] = response.read()
        except (urllib.error.URLError, OSError) as exc:
            raise FetchError(url, exc) from exc
    return fetched


USAGE = "usage: hub_fetch_run.py <hub-relative-path> [script-args...]"


def main(argv: list[str]) -> int:
    if argv[:1] in (["-h"], ["--help"]):
        print(USAGE)
        return 0
    if not argv or argv[0].startswith("-"):
        problem = f"unknown option {argv[0]!r}, " if argv else ""
        print(f"hub_fetch_run: {problem}{USAGE}", file=sys.stderr)
        return 2
    hub_path, script_args = argv[0], resolve_unborn_head(argv[1:])
    is_prose_gate = hub_path == PROSE_GATE_PATH
    commit = resolve_main_commit() if hub_path in COMPANIONS else None
    ref = commit or "main"
    if hub_path in COMPANIONS and not is_prose_gate:
        print(f"hub_fetch_run: running {hub_path} from {HUB_REPO}@{ref}.", file=sys.stderr)
    uv = shutil.which("uv")
    if uv is None:
        print("hub_fetch_run: uv is not on PATH, so the gate did not run.", file=sys.stderr)
        return 1
    env = dict(os.environ)
    if is_prose_gate and not env.get(PROVENANCE_VAR, "").strip():
        if commit:
            env[PROVENANCE_VAR] = f"{HUB_REPO}@{commit} (hub_fetch_run.py main)"
        else:
            env[PROVENANCE_VAR] = f"{HUB_REPO}@main (hub_fetch_run.py, commit unresolved)"
    with tempfile.TemporaryDirectory() as scratch:
        paths = (*COMPANIONS.get(hub_path, ()), hub_path)
        try:
            fetched = fetch_all(ref, paths)
            if commit is None and len(paths) > 1 and fetch_all(ref, paths) != fetched:
                print(
                    "hub_fetch_run: main moved while the files were fetched, "
                    "so a mixed pair was refused, re-run to fetch a consistent pair.",
                    file=sys.stderr,
                )
                print("hub_fetch_run: the gate did not run.", file=sys.stderr)
                return 1
        except FetchError as exc:
            print(f"hub_fetch_run: could not fetch {exc.url}: {exc.cause}", file=sys.stderr)
            print("hub_fetch_run: the gate did not run.", file=sys.stderr)
            return 1
        # A file keeps its hub-relative path, so a sys.path entry derived from it stays in here.
        for path, content in fetched.items():
            target = Path(scratch) / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(content)
        script = Path(scratch) / hub_path
        cmd = [uv, "run", "--no-project", "--script", str(script), *script_args]
        return subprocess.run(cmd, env=env, check=False).returncode


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
