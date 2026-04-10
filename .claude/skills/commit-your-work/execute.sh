#!/bin/bash
# ============================================================================
# Skill: commit-your-work
# Project: WitchCityRope
#
# PURPOSE:
#   Guides an agent through committing ONLY their own changes to git.
#   This is a MULTI-AGENT environment. Other agents may have uncommitted
#   changes in the working directory. You MUST NOT touch, revert, stash,
#   checkout, or in any way modify those changes.
#
# USAGE:
#   bash .claude/skills/commit-your-work/execute.sh
#
# WHAT THIS SKILL DOES:
#   1. Shows git status so the agent can see ALL modified/untracked files
#   2. Shows recent commit messages for style reference
#   3. Outputs detailed instructions for the agent to follow
#
# WHAT THE AGENT MUST DO AFTER RUNNING THIS SKILL:
#   - Identify which files are THEIRS vs another agent's
#   - Stage ONLY their files with explicit `git add <file>` calls
#   - Commit with a proper message
#   - Push to origin
#   - Verify the other agent's unstaged changes are STILL in the working directory
#
# ============================================================================

set -e

echo ""
echo "============================================"
echo "  COMMIT YOUR WORK — Multi-Agent Safe Commit"
echo "============================================"
echo ""

echo "--- Current Branch ---"
git branch --show-current
echo ""

echo "--- Git Status (all files) ---"
git status
echo ""

echo "--- Recent Commits (for message style) ---"
git log --oneline -5
echo ""

echo "--- Unstaged Diff Summary ---"
git diff --stat
echo ""

echo "--- Staged Diff Summary ---"
git diff --cached --stat
echo ""

cat <<'INSTRUCTIONS'
============================================================================
AGENT INSTRUCTIONS — READ EVERY WORD BEFORE PROCEEDING
============================================================================

You are in a MULTI-AGENT environment. Other agents may have uncommitted
changes in this working directory RIGHT NOW. Their work is just as
important as yours.

CRITICAL: DO NOT ASK THE USER WHAT TO COMMIT.
  - Commit ONLY the files YOU changed in this session. You know which
    files you touched — just commit those. Ignore everything else.
  - If you did not change a file, DO NOT TOUCH IT AT ALL. Do not stage
    it, do not ask about it, do not mention it.
  - If you AND another agent both changed the same file, commit it as-is.
  - This is not a decision that requires user input. Just do it.

ABSOLUTE RULES — VIOLATION OF ANY OF THESE IS UNACCEPTABLE:

  1. NEVER run `git stash`. NEVER. It picks up other agents' work and
     causes data loss. There is a hook that blocks this, but do not
     even think about trying.

  2. NEVER run `git checkout -- <file>` or `git restore <file>` on ANY
     file unless you created that file from scratch in this session.
     These commands DESTROY uncommitted work.

  3. NEVER run `git add .` or `git add -A`. These stage EVERYTHING,
     including other agents' work.

  4. NEVER modify, revert, or "clean up" changes in files that belong
     to another agent. If a file has both your changes AND another
     agent's changes, commit the file AS-IS. Do not remove their work.

  5. "IGNORE other agents' changes" means LEAVE THEM ALONE. Do not
     touch them. Do not stage them. Do not revert them. Do not look
     at them funny. Ignore = pretend they don't exist.

STEP-BY-STEP PROCESS:

  Step 1: IDENTIFY YOUR FILES
    Look at the git status output above. For each modified/untracked file,
    determine: "Did I create or modify this file during my work?"
    - If YES → it's yours, you will stage it
    - If NO → it's another agent's, DO NOT TOUCH IT
    - If BOTH (you and another agent changed the same file) → stage it
      as-is, do NOT remove the other agent's changes from it

  Step 2: STAGE ONLY YOUR FILES
    Use explicit file paths:
      git add path/to/your/file1.cs path/to/your/file2.tsx ...
    List every file by name. No wildcards. No directory adds.

  Step 3: VERIFY STAGING
    Run: git diff --cached --stat
    Confirm ONLY your files are staged.
    Run: git diff --stat
    Confirm the other agent's files are STILL listed as unstaged changes.

  Step 4: COMMIT
    Write a clear commit message describing YOUR changes only.
    Use a heredoc for the message:
      git commit -m "$(cat <<'EOF'
      Your commit message here.

      Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
      EOF
      )"

  Step 5: PUSH
    git push

  Step 6: VERIFY OTHER AGENT'S WORK IS INTACT
    Run: git status
    The other agent's uncommitted files MUST still appear as modified.
    If they don't, YOU BROKE SOMETHING. Tell the user immediately.

============================================================================
END OF INSTRUCTIONS
============================================================================
INSTRUCTIONS

echo ""
echo "=== SKILL_RESULT ==="
echo "{"
echo "  \"skill\": \"commit-your-work\","
echo "  \"status\": \"success\","
echo "  \"timestamp\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\","
echo "  \"message\": \"Status displayed. Follow the instructions above to commit safely.\""
echo "}"
echo "=== END_SKILL_RESULT ==="
