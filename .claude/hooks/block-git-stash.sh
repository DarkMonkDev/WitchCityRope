#!/bin/bash
# Hook: Block `git stash` in Bash tool invocations.
#
# WHY:
#   In multi-agent workflows, `git stash` silently picks up OTHER agents'
#   uncommitted work, leading to data loss when the stash is later applied
#   elsewhere or dropped. This hook forces the agent to commit their own
#   files explicitly (via the `commit-your-work` skill) instead of using
#   stash as a quick-and-dirty workaround.
#
# HISTORY:
#   Copied from inventory-purchasing-workflow repo's block-direct-commands.sh
#   on 2026-04-10. Only the git stash blocking logic was copied — the dotnet
#   and docker command blocks were intentionally excluded because
#   witchcityrope uses different skill names than the inventory repo.
#
# REGISTRATION:
#   Registered in .claude/settings.json under hooks.PreToolUse with
#   matcher "Bash". Runs before every Bash tool invocation.

CMD=$(jq -r '.tool_input.command // empty')

# Skip if no command (shouldn't happen for Bash tool)
[ -z "$CMD" ] && exit 0

# Block git stash — NEVER use stash, always commit instead.
# Stash silently picks up other agents' uncommitted work and causes data loss.
if echo "$CMD" | grep -qE 'git\s+stash\b'; then
  echo '{"decision":"block","reason":"BLOCKED: NEVER use git stash. Always commit files instead. Stash picks up other agents uncommitted work and causes data loss. Use the commit-your-work skill to safely commit only your own changes."}'
  exit 0
fi

# Allow everything else
exit 0
