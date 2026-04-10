#!/usr/bin/env python3
"""
Hook: Block `git stash` in Bash tool invocations.

WHY:
    In multi-agent workflows, `git stash` silently picks up OTHER agents'
    uncommitted work, leading to data loss when the stash is later applied
    elsewhere or dropped. This hook forces the agent to commit their own
    files explicitly (via the `commit-your-work` skill) instead.

HOW:
    Reads the Bash tool's command from stdin as a JSON blob:
        {"tool_input": {"command": "..."}}

    Tokenizes the command using shlex (POSIX mode), which respects shell
    quoting. Scans the resulting tokens for a `git` followed by `stash` at
    COMMAND POSITION — meaning either the first token, or the first token
    after a shell separator (;, &&, ||, |, &, etc.).

WHY NOT A RAW REGEX:
    An earlier version of this hook used `grep -qE 'git\\s+stash\\b'` which
    matched the literal string anywhere in the command, including inside
    heredoc bodies and quoted strings. That caused false positives — a
    commit message heredoc containing the text "git stash" (e.g. a commit
    that documents this very hook) would get blocked. See the 2026-04-10
    commit ac8a23e5 for the incident.

    shlex.split handles shell quoting correctly: a heredoc body passed via
    `git commit -m "$(cat <<'EOF' ... git stash ... EOF)"` ends up as a
    single quoted token, not as individual `git` and `stash` tokens, so
    the scan correctly ignores it.

TRUE POSITIVES (blocked):
    git stash
    git stash push -m "work in progress"
    cd /tmp && git stash pop
    git stash; echo done

TRUE NEGATIVES (allowed):
    git commit -m "mentions git stash in the message body"
    echo "don't use git stash"
    git status
    git commit -m "$(cat <<'EOF' ... git stash ... EOF)"

KNOWN FALSE POSITIVE (acceptable):
    echo git stash   → blocked (git is not at command position, but our
                       simple command-position check sees `echo` as a
                       non-separator and still flags it). This is rare
                       enough in practice that we accept the trade-off.

REGISTRATION:
    Registered in .claude/settings.json under hooks.PreToolUse with
    matcher "Bash". Runs before every Bash tool invocation.
"""

import json
import shlex
import sys


# Tokens that indicate a command boundary. If `git` appears immediately
# after one of these, it's at command position (and `git stash` after it
# is a real invocation, not an argument to another command).
#
# Note: `&&`, `||`, `|`, `;`, `&` are the common shell separators. shlex.split
# in POSIX mode with default settings typically emits these as standalone
# tokens when they appear with surrounding whitespace, which is the normal
# case in agent-generated commands.
COMMAND_SEPARATORS = {
    ";", "&", "&&", "|", "||", "(", ")", "{", "}",
    "then", "else", "elif", "do", "done", "fi",
}

BLOCK_RESPONSE = json.dumps({
    "decision": "block",
    "reason": (
        "BLOCKED: NEVER use git stash. Always commit files instead. "
        "Stash picks up other agents uncommitted work and causes data loss. "
        "Use the commit-your-work skill to safely commit only your own changes."
    ),
})


def is_at_command_position(tokens: list[str], i: int) -> bool:
    """
    Return True if tokens[i] is at the start of a new command.

    A token is at command position if:
      - It's the first token (i == 0), OR
      - The previous token is a known shell separator.
    """
    if i == 0:
        return True
    return tokens[i - 1] in COMMAND_SEPARATORS


def main() -> int:
    # Read the hook payload from stdin. If parsing fails, allow the command
    # (fail open) — a broken hook should not block the agent entirely.
    try:
        data = json.load(sys.stdin)
    except (json.JSONDecodeError, ValueError):
        return 0

    cmd = data.get("tool_input", {}).get("command", "")
    if not cmd:
        return 0

    # Tokenize with shlex. We use shlex.shlex (not shlex.split) with
    # punctuation_chars=True so shell operators like `;`, `|`, `&` become
    # standalone tokens. Without this, `git stash; echo done` would produce
    # tokens ['git', 'stash;', 'echo', 'done'] — with the trailing semicolon
    # glued to 'stash' — and our consecutive-token scan would miss it.
    #
    # POSIX mode respects quoting: single/double-quoted strings become single
    # tokens, so `git stash` inside a quoted string (commit message, echo
    # argument, heredoc body) stays inside its enclosing token and is never
    # seen as standalone `git` + `stash` tokens.
    try:
        # punctuation_chars must be set via constructor kwarg (read-only attr).
        # Default punctuation set is ();<>|& — which covers the common shell
        # separators. whitespace_split keeps unquoted word content intact.
        lex = shlex.shlex(cmd, posix=True, punctuation_chars=True)
        lex.whitespace_split = True
        tokens = list(lex)
    except ValueError:
        # Unbalanced quotes or similar malformed input. Fall back to a
        # conservative raw-string check — prefer a false positive over
        # silently missing a real stash invocation.
        if "git stash" in cmd:
            print(BLOCK_RESPONSE)
        return 0

    # Scan for consecutive `git` + `stash` tokens at command position.
    for i in range(len(tokens) - 1):
        if tokens[i] == "git" and tokens[i + 1] == "stash":
            if is_at_command_position(tokens, i):
                print(BLOCK_RESPONSE)
                return 0

    return 0


if __name__ == "__main__":
    sys.exit(main())
