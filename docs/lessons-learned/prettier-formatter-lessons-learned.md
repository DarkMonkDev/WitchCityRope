# Prettier Formatter Agent Lessons Learned

## 🚨 CRITICAL: WORKTREE COMPLIANCE - MANDATORY 🚨

### ALL WORK MUST BE IN THE SPECIFIED WORKTREE DIRECTORY

**VIOLATION = CATASTROPHIC FAILURE**

When given a Working Directory like:
`/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management`

**YOU MUST:**
- Write ALL files to paths within the worktree directory
- NEVER write to `/home/chad/repos/witchcityrope-react/` main repository
- ALWAYS use the full worktree path in file operations
- VERIFY you're in the correct directory before ANY file operation

**Example:**
- ✅ CORRECT: `/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/docs/...`
- ❌ WRONG: `/home/chad/repos/witchcityrope-react/docs/...`

**Why This Matters:**
- Worktrees isolate feature branches
- Writing to main repo pollutes other branches
- Can cause merge conflicts and lost work
- BREAKS the entire development workflow

## Absolute Paths Required for Multi-Directory Formatting

**Date**: 2025-08-16
**Category**: Formatting
**Severity**: High

### Context
When formatting files from different directories in the WitchCityRope project, relative paths were failing to resolve correctly, causing formatting commands to fail silently or format the wrong files.

### What We Learned
Prettier commands must use absolute paths when targeting specific files, especially when running from repository root but targeting files in subdirectories like apps/web/src/components/*.tsx.

### Action Items
- [ ] Always use absolute paths in prettier commands
- [ ] Run prettier from repository root where .prettierrc exists
- [ ] Verify working directory before running formatting commands

### Impact
Ensures reliable formatting across the entire project structure and prevents silent failures.

### Tags
#formatting #paths #prettier #reliability

---

## Project Configuration Requires No Semicolons

**Date**: 2025-08-16
**Category**: Configuration
**Severity**: Medium

### Context
The WitchCityRope project has specific Prettier configuration that differs from default settings, particularly around semicolon usage and quote preferences.

### What We Learned
The .prettierrc configuration specifies:
- Semi: false (no semicolons)
- Single quotes: true
- Print width: 100 characters
- Tab width: 2 spaces
- Trailing comma: "es5"
- Arrow parens: "always"
- End of line: "lf"

### Action Items
- [ ] Always check .prettierrc before formatting
- [ ] Ensure configuration is consistently applied
- [ ] Document any configuration changes

### Impact
Maintains consistent code style across the project and prevents formatting conflicts.

### Tags
#configuration #prettier #standards #consistency

---

## Multi-Language Formatting Strategy

**Date**: 2025-08-16
**Category**: Integration
**Severity**: Medium

### Context
The project contains both TypeScript/React files and C# API files that need different formatting tools but should work together without conflicts.

### What We Learned
- Use Prettier for TypeScript/React files (.ts, .tsx, .js, .jsx)
- Use `dotnet format` for C# files (.cs)
- Both tools work independently without conflicts
- Both should be run during Phase 5 quality gates

### Action Items
- [ ] Include both Prettier and dotnet format in quality gates
- [ ] Run formatting tools in sequence: Prettier first, then dotnet format
- [ ] Verify both tools complete successfully

### Impact
Ensures consistent formatting across the entire codebase regardless of technology stack.

### Tags
#formatting #integration #dotnet #prettier #quality-gates

---

## Working Directory Dependencies

**Date**: 2025-08-16
**Category**: Configuration
**Severity**: High

### Context
Prettier relies on finding .prettierrc configuration file, which is located at repository root. Running prettier from subdirectories can cause configuration to be missed.

### What We Learned
- Prettier must be run from repository root where .prettierrc exists
- Relative paths can fail when not in correct working directory
- Configuration resolution follows file system hierarchy

### Action Items
- [ ] Always verify current working directory before running prettier
- [ ] Use absolute paths when targeting specific files
- [ ] Check for .prettierrc presence before formatting

### Impact
Prevents formatting inconsistencies and ensures configuration is properly applied.

### Tags
#configuration #working-directory #prettier #file-paths

---

## Format Check Before Write Pattern

**Date**: 2025-08-16
**Category**: Process
**Severity**: Medium

### Context
During Phase 5 quality gates, it's important to understand what formatting changes will be made before applying them.

### What We Learned
Best practice is to run `npx prettier --check` first to identify files that need formatting, then run `npx prettier --write` to apply changes. This provides visibility into what will change.

### Action Items
- [ ] Always run --check flag first to assess formatting needs
- [ ] Document which files will be modified before applying changes
- [ ] Verify formatting success with final --check after write

### Impact
Provides transparency in formatting process and helps track what changes are being made.

### Tags
#process #prettier #quality-gates #transparency

---

## File Type Support Coverage

**Date**: 2025-08-16
**Category**: Configuration
**Severity**: Low

### Context
Understanding which file types Prettier handles in the WitchCityRope project to ensure comprehensive formatting coverage.

### What We Learned
Prettier in this project handles:
- TypeScript/React: .ts, .tsx files
- JavaScript: .js, .jsx files  
- Test files: .spec.ts, .test.tsx
- JSON and markdown files
- Does NOT handle C# files (use dotnet format)

### Action Items
- [ ] Ensure test files are included in formatting commands
- [ ] Remember to format JSON configuration files
- [ ] Don't attempt to format C# files with Prettier

### Impact
Ensures complete coverage of formattable files and prevents tool misuse.

### Tags
#file-types #prettier #coverage #testing
