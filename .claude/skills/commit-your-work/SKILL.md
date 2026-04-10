---
name: commit-your-work
description: Multi-agent safe git commit — shows status, then guides you through staging only your files, committing, and pushing
---

## MANDATORY BEHAVIOR — DO NOT DEVIATE

1. Run `bash .claude/skills/commit-your-work/execute.sh`
2. Commit ONLY the files YOU changed in this session. You already know which files you touched. DO NOT ASK THE USER.
3. Ignore all other modified files completely — do not stage them, do not mention them, do not ask about them.
4. If you AND another agent both changed the same file, commit it as-is.
5. DO NOT ask the user any questions during this process. No "should I commit X?", no "do you want all files?", no confirmation prompts. JUST DO IT.
6. Follow the step-by-step instructions printed by the script exactly.
7. Push to origin after committing.
8. Verify other agents' unstaged files are still intact after the commit.
