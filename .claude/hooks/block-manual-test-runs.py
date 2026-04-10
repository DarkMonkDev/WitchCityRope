#!/usr/bin/env python3
"""
Hook: Block direct test-runner invocations in Bash tool commands.

WHY:
    WitchCityRope has skills and a test-executor agent that handle all test
    execution. Running test commands directly via Bash skips:
      - Isolated test container startup (uses dev containers instead — bad)
      - Database seeding and reset
      - Health checks before/after test runs
      - Result reporting back through the skill layer
      - Proper environment variable wiring

    On 2026-04-10 during the vetting status cleanup, an agent ran
    `npx vitest run` directly to check the unit test suite baseline. It
    produced 196 failures that mostly reflected environment misconfiguration
    (missing QueryClientProvider, etc.), not real test failures. The user
    had explicitly said "ALWAYS USE THE SKILLS" and the agent still did it.
    This hook is the enforcement that memory instructions couldn't provide.

BLOCKED COMMANDS (at command position only):
    vitest                 # direct vitest invocation
    npx vitest ...
    jest
    npx jest ...
    playwright test ...    # note: "playwright install" is still allowed
    npx playwright test ...
    dotnet test ...
    npm test / npm t / npm run test / npm run test:unit / etc.
    yarn test / yarn test:unit / etc.
    pnpm test / pnpm test:unit / etc.

ALLOWED (not blocked):
    playwright install             # browser install, not a test run
    dotnet build / dotnet run      # not a test command
    npm install / npm run build    # not a test script
    echo "run npm test"            # quoted, not at command position
    commit messages mentioning any of the above via heredoc

HOW TO RUN TESTS INSTEAD:
    - `test-environment` skill (unit / integration / E2E in isolated containers)
    - `restart-test-containers` skill (lifecycle only)
    - Delegate to `test-executor` agent via Task tool (CLAUDE.md convention:
      "test/testing/debug/fix → Delegate to test-executor agent")

DESIGN:
    Uses shlex tokenization (same approach as block-git-stash.py) so quoted
    strings, heredoc bodies, and command-message arguments are not false-
    positived. Only blocks test runners at COMMAND POSITION — i.e., the
    first token, or the first token after a shell separator (;, &&, ||, |).

REGISTRATION:
    Registered in .claude/settings.json under hooks.PreToolUse with
    matcher "Bash". Runs alongside block-git-stash.py on every Bash
    tool invocation.
"""

import json
import shlex
import sys


# Tokens that indicate a command boundary. If a blocked runner appears
# immediately after one of these, it's at command position.
COMMAND_SEPARATORS = {
    ";", "&", "&&", "|", "||", "(", ")", "{", "}",
    "then", "else", "elif", "do", "done", "fi",
}

# Package runners that may be followed by a test-runner name at the next
# token position, e.g. `npx vitest`, `npx jest`, `npx playwright test`.
WRAPPER_COMMANDS = {"npx", "pnpm", "bunx"}

# Test runners that are ALWAYS a test invocation when at command position
# or after a wrapper like `npx`. No arg inspection needed.
DIRECT_TEST_RUNNERS = {
    "vitest",
    "jest",
    "mocha",
    "karma",
}

BLOCK_REASON = (
    "BLOCKED: Direct test-runner invocations are not allowed. "
    "Use the 'test-environment' skill (for unit/integration/E2E tests in "
    "isolated containers) or delegate to the 'test-executor' agent via the "
    "Task tool. Running `npx vitest`, `dotnet test`, `npm test`, "
    "`playwright test`, etc. directly skips container isolation, database "
    "seeding, and health checks — producing unreliable results. "
    "See CLAUDE.md: 'test/testing/debug/fix → Delegate to test-executor agent'."
)

BLOCK_RESPONSE = json.dumps({"decision": "block", "reason": BLOCK_REASON})


def is_at_command_position(tokens: list[str], i: int) -> bool:
    """Token at index i starts a new command iff i==0 or prev token is a separator."""
    if i == 0:
        return True
    return tokens[i - 1] in COMMAND_SEPARATORS


def check_tokens(tokens: list[str]) -> bool:
    """
    Scan tokens for a blocked test-runner invocation at command position.
    Returns True if the command should be blocked.
    """
    n = len(tokens)
    for i in range(n):
        tok = tokens[i]
        if not is_at_command_position(tokens, i):
            continue

        # Case 1: `npx vitest ...`, `npx jest ...`, `npx playwright test ...`,
        # `pnpm vitest ...`, `bunx vitest ...`, etc.
        if tok in WRAPPER_COMMANDS and i + 1 < n:
            next_tok = tokens[i + 1]
            if next_tok in DIRECT_TEST_RUNNERS:
                return True
            # `npx playwright test ...` blocked, `npx playwright install` allowed
            if next_tok == "playwright" and i + 2 < n and tokens[i + 2] == "test":
                return True

        # Case 2: direct runner at command position: `vitest`, `jest`, etc.
        if tok in DIRECT_TEST_RUNNERS:
            return True

        # Case 3: `playwright test ...` (without npx prefix — global install)
        if tok == "playwright" and i + 1 < n and tokens[i + 1] == "test":
            return True

        # Case 4: `dotnet test ...`
        if tok == "dotnet" and i + 1 < n and tokens[i + 1] == "test":
            return True

        # Case 5: package manager test scripts.
        #   `npm test`, `npm t`, `npm run test`, `npm run test:unit`, etc.
        #   `yarn test`, `yarn test:unit`, etc.
        #   `pnpm test`, `pnpm test:unit`, etc.
        if tok in {"npm", "yarn", "pnpm"} and i + 1 < n:
            next_tok = tokens[i + 1]
            if tok == "npm":
                # `npm test` or `npm t`
                if next_tok in {"test", "t"}:
                    return True
                # `npm run test` or `npm run test:*`
                if next_tok == "run" and i + 2 < n:
                    script = tokens[i + 2]
                    if script == "test" or script.startswith("test:"):
                        return True
            else:  # yarn or pnpm
                # `yarn test`, `yarn test:unit`, `pnpm test`, etc.
                if next_tok == "test" or next_tok.startswith("test:"):
                    return True
                # `yarn run test`, `pnpm run test` (less common but valid)
                if next_tok == "run" and i + 2 < n:
                    script = tokens[i + 2]
                    if script == "test" or script.startswith("test:"):
                        return True

    return False


def main() -> int:
    # Read the hook payload from stdin. Fail open on parse errors — a broken
    # hook should not block all Bash tool use entirely.
    try:
        data = json.load(sys.stdin)
    except (json.JSONDecodeError, ValueError):
        return 0

    cmd = data.get("tool_input", {}).get("command", "")
    if not cmd:
        return 0

    # Tokenize with shlex, treating shell operators (;, |, &, etc.) as
    # standalone tokens. POSIX mode respects quoting so test-runner names
    # inside quoted strings, heredoc bodies, or commit messages are not
    # mistaken for command invocations.
    try:
        lex = shlex.shlex(cmd, posix=True, punctuation_chars=True)
        lex.whitespace_split = True
        tokens = list(lex)
    except ValueError:
        # Unbalanced quotes or malformed input. Fall back to a conservative
        # substring scan — prefer a false positive over silently missing a
        # real manual test invocation.
        manual_markers = [
            "npx vitest",
            "npx jest",
            "npx playwright test",
            "dotnet test",
            "npm test",
            "npm t ",
            "npm run test",
            "yarn test",
            "pnpm test",
            "playwright test",
        ]
        if any(marker in cmd for marker in manual_markers):
            print(BLOCK_RESPONSE)
        return 0

    if check_tokens(tokens):
        print(BLOCK_RESPONSE)
    return 0


if __name__ == "__main__":
    sys.exit(main())
