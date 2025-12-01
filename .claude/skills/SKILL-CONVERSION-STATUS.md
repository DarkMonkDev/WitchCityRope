# Skill Conversion Status - Option A-Plus Pattern

**Date**: 2025-11-11
**Task**: Convert markdown-only skills to Option A-Plus pattern (executable scripts + documentation)
**Status**: 1 of 9 Complete

## Overview

Converting 9 skills from markdown-only documentation to the Option A-Plus pattern used by successfully converted skills (container-restart, staging-deploy, database-reset-staging, test-catalog-updater, phase-1-validator).

### Option A-Plus Pattern Components

1. **execute.sh** - Executable bash script with:
   - Shebang (`#!/bin/bash`) and header comment
   - `set -e` for error handling
   - Pre-flight information section (purpose, when to use, when NOT to use, what script does)
   - Confirmation prompt (skippable with `SKIP_CONFIRMATION=true`)
   - Numbered prerequisite checks with emojis (1️⃣, 2️⃣, 3️⃣)
   - Main script logic (extracted from SKILL.md bash code blocks)
   - Comprehensive error messages pointing to SKILL.md
   - Summary report at end
   - **Must be executable**: `chmod +x execute.sh`

2. **SKILL.md** - Updated documentation with:
   - "## How to Use This Skill" section (replaces "Quick Validation" or "Automated Script")
   - Executable script usage examples
   - Parameter descriptions
   - What the script validates/does
   - **NO embedded ```bash code blocks** (maintains single source of truth)
   - References to execute.sh for automation

## Completed Conversions

### ✅ phase-2-validator (COMPLETED)
- **Status**: ✅ Done
- **Files Created**:
  - `/home/chad/repos/witchcityrope/.claude/skills/phase-2-validator/execute.sh` (chmod +x)
- **Files Modified**:
  - `/home/chad/repos/witchcityrope/.claude/skills/phase-2-validator/SKILL.md`
- **Pattern**: Full Option A-Plus implementation
- **Validation**: Script validates Design Phase completion (functional-spec, database-design, ui-ux-design)

## Pending Conversions

### 🟡 phase-3-validator
- **Priority**: HIGH (Implementation Phase validator)
- **Source**: `/home/chad/repos/witchcityrope/.claude/skills/phase-3-validator/SKILL.md` (lines 63-336)
- **Bash Script Location**: Lines 66-336 contain full validation script
- **Key Features**:
  - Validates code compilation (API + Web)
  - Checks implementation completeness
  - Code quality validation (TypeScript, C#, ESLint)
  - Testing infrastructure check
  - Documentation validation
- **Parameters**: `<feature-name>` `[work-type]` `[required-percentage]`
- **Max Score**: 50 points
- **Quality Gates**: Feature 85%, Bug 75%, Hotfix 70%, Docs 80%, Refactor 90%

### 🟡 phase-4-validator
- **Priority**: HIGH (Testing Phase validator - 100% test pass rate required)
- **Source**: `/home/chad/repos/witchcityrope/.claude/skills/phase-4-validator/SKILL.md` (lines 61-371)
- **Bash Script Location**: Lines 63-371 contain full validation script
- **Key Features**:
  - Environment health check (Docker containers, database, API, web)
  - Test execution validation (unit, integration, E2E - MUST be 100% passing)
  - Test coverage analysis
  - Test quality assessment
  - Documentation check
- **Parameters**: No parameters (uses current environment)
- **Max Score**: 100 points
- **Critical**: ALL work types require 100% test pass rate (zero tolerance)

### 🟡 phase-5-validator
- **Priority**: HIGH (Finalization Phase validator with BLOCKING authority)
- **Source**: `/home/chad/repos/witchcityrope/.claude/skills/phase-5-validator/SKILL.md` (lines 79-562)
- **Bash Script Location**: Lines 82-562 contain full validation script
- **Key Features**:
  - **BLOCKING CRITICAL**: Single source of truth validation (runs FIRST)
  - **BLOCKING CRITICAL**: Lessons learned size validation (runs SECOND)
  - Git & version control validation
  - Documentation validation
  - Lessons learned validation
  - Deployment readiness check
  - Cleanup validation
- **Parameters**: No parameters
- **Max Score**: 100 points (after BLOCKING checks pass)
- **Special**: Has BLOCKING AUTHORITY - violations prevent workflow completion

### 🟡 quality-gate-calculator
- **Priority**: MEDIUM (Supporting skill for phase validators)
- **Source**: `/home/chad/repos/witchcityrope/.claude/skills/quality-gate-calculator/SKILL.md` (lines 77-242)
- **Bash Script Location**: Lines 79-242 contain calculation script
- **Key Features**:
  - Calculates quality gate thresholds by work type and phase
  - Exports quality gate to `/tmp/quality-gate.env`
  - Provides rationale for thresholds
  - Shows pass/fail examples
- **Parameters**: `<work-type>` `<phase>`
- **Output**: Environment file with REQUIRED_PERCENTAGE, TARGET_SCORE, MAX_SCORE

### 🟡 single-source-validator
- **Priority**: MEDIUM (ENFORCEMENT tool with BLOCKING authority)
- **Source**: `/home/chad/repos/witchcityrope/.claude/skills/single-source-validator/SKILL.md` (lines 86-443)
- **Bash Script Location**: Lines 88-443 contain validator script
- **Key Features**:
  - Detects bash command duplication in agent definitions/lessons/docs
  - Checks for step-by-step procedure duplication
  - Validates proper skill references
  - Has BLOCKING AUTHORITY in Phase 5
- **Parameters**: `<skill-name>` `[severity]` (CRITICAL/WARNING/INFO)
- **Special**: Used by phase-5-validator to enforce single source of truth

### 🟡 handoff-document-generator
- **Priority**: MEDIUM (Document generation for phase transitions)
- **Source**: `/home/chad/repos/witchcityrope/.claude/skills/handoff-document-generator/SKILL.md` (lines 149-352)
- **Bash Script Location**: Lines 151-352 contain generation script
- **Key Features**:
  - Generates standardized handoff documents
  - Creates template with TODO sections
  - Auto-fills known information
  - Lists recent files
- **Parameters**: `<source-agent>` `<target-agent>` `<feature-name>` `[work-type]`
- **Output**: Handoff markdown file in new-work/handoffs/ directory

### 🟡 lessons-learned-validator
- **Priority**: MEDIUM (File size validation with BLOCKING authority)
- **Source**: `/home/chad/repos/witchcityrope/.claude/skills/lessons-learned-validator/SKILL.md` (lines 339-714)
- **Bash Script Location**: Lines 341-714 contain validation script
- **Key Features**:
  - Validates lessons learned file structure and format
  - **CRITICAL**: Checks file size (max 2,000 lines, warning at 1,800)
  - Validates Problem→Solution→Example pattern
  - Content quality checks
  - Has BLOCKING AUTHORITY in Phase 5 if files exceed 2,000 lines
- **Parameters**: `<lessons-learned-file.md>`
- **Max Score**: 100 points (after BLOCKING size check passes)

### 🟡 master-index-updater
- **Priority**: LOW (Index maintenance automation)
- **Source**: `/home/chad/repos/witchcityrope/.claude/skills/master-index-updater/SKILL.md` (lines 99-241)
- **Bash Script Location**: Lines 101-241 contain updater script
- **Key Features**:
  - Adds new features to Functional Area Master Index
  - Updates existing feature entries
  - Marks features as deprecated
  - Creates backup before modification
- **Parameters**: `<action>` `<feature-name>` `<domain>` `<feature-path>`
- **Actions**: add | update | deprecate

## Conversion Instructions

For each remaining skill, follow this EXACT process:

### Step 1: Create execute.sh

```bash
# Navigate to skill directory
cd /home/chad/repos/witchcityrope/.claude/skills/[skill-name]/

# Create execute.sh file
# Copy the bash script from SKILL.md lines indicated above
# Add pre-flight information section (see phase-1-validator or phase-2-validator as examples)
# Add prerequisite checks before main logic
# Ensure all paths are correct
# Add comprehensive error messages
# Make executable
chmod +x execute.sh
```

### Step 2: Update SKILL.md

Replace the "## Quick Validation" or "## Automated Validation Script" or "## Automated [X] Script" section with:

```markdown
## How to Use This Skill

**Executable Script**: `execute.sh`

```bash
# Basic usage
bash .claude/skills/[skill-name]/execute.sh <parameters>

# Examples:
[Provide 2-3 concrete examples]
```

**Parameters**:
- [List each parameter with description]

**Script validates/does**:
- [List what the script checks or performs]

**Exit codes**:
- 0: [Success condition]
- 1: [Failure condition]
```

Then REMOVE the embedded ```bash code block (the automation is now in execute.sh).

### Step 3: Test the Conversion

```bash
# Test the executable script runs
bash .claude/skills/[skill-name]/execute.sh

# Should show pre-flight information if no parameters

# Test with actual parameters (if applicable)
bash .claude/skills/[skill-name]/execute.sh [test-parameters]

# Verify SKILL.md no longer has embedded bash scripts
grep -c '```bash' .claude/skills/[skill-name]/SKILL.md
# Should be 0 or only have example usage blocks (not full scripts)
```

## Reference Files

Use these as templates for the pattern:

**Best Examples**:
- `/home/chad/repos/witchcityrope/.claude/skills/phase-1-validator/execute.sh` (lines 1-290)
- `/home/chad/repos/witchcityrope/.claude/skills/phase-1-validator/SKILL.md` (lines 12-44)
- `/home/chad/repos/witchcityrope/.claude/skills/restart-dev-containers/execute.sh` (lines 1-243)
- `/home/chad/repos/witchcityrope/.claude/skills/phase-2-validator/execute.sh` (just created)

**Structure Pattern**:
```bash
#!/bin/bash
# [Skill Name] Script
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

if [ "$#" -lt [required-params] ]; then
    echo "📋 [Skill Name]"
    echo "==============="
    echo ""
    echo "📋 Purpose: [What this does]"
    echo ""
    echo "✅ Use when:"
    echo "   • [Scenario 1]"
    echo "   • [Scenario 2]"
    echo ""
    echo "❌ DO NOT use if:"
    echo "   • [Anti-scenario 1]"
    echo "   • [Anti-scenario 2]"
    echo ""
    echo "⚙️  What this script does:"
    echo "   1. [Step 1]"
    echo "   2. [Step 2]"
    echo ""
    echo "📝 Usage:"
    echo "   bash execute.sh <param1> [param2]"
    echo ""
    exit 1
fi

# ============================================
# PARAMETER EXTRACTION
# ============================================

PARAM1="$1"
PARAM2="${2:-default}"

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "🔍 [Skill Name]"
echo "==============="
echo ""

echo "1️⃣  Checking [prerequisite 1]..."
[validation logic]
echo "   ✅ [Success message]"

echo ""
echo "2️⃣  Checking [prerequisite 2]..."
[validation logic]
echo "   ✅ [Success message]"

echo ""
echo "✅ All prerequisites passed"
echo ""

# ============================================
# MAIN SCRIPT - [ACTION]
# ============================================

[Main logic from original bash script]

echo "✅ [Completion Message]"
echo "======================"
echo ""
echo "[Summary information]"
exit 0
```

## Completion Checklist

- [ ] phase-3-validator: execute.sh created + SKILL.md updated + chmod +x
- [ ] phase-4-validator: execute.sh created + SKILL.md updated + chmod +x
- [ ] phase-5-validator: execute.sh created + SKILL.md updated + chmod +x
- [ ] quality-gate-calculator: execute.sh created + SKILL.md updated + chmod +x
- [ ] single-source-validator: execute.sh created + SKILL.md updated + chmod +x
- [ ] handoff-document-generator: execute.sh created + SKILL.md updated + chmod +x
- [ ] lessons-learned-validator: execute.sh created + SKILL.md updated + chmod +x
- [ ] master-index-updater: execute.sh created + SKILL.md updated + chmod +x
- [ ] All execute.sh files tested and working
- [ ] All SKILL.md files no longer have embedded bash scripts
- [ ] File registry updated with all changes
- [ ] PROGRESS.md updated
- [ ] Git commit with all changes

## Notes

- **Priority**: HIGH items are phase validators - most critical for workflow
- **BLOCKING AUTHORITY**: phase-5-validator and lessons-learned-validator have blocking authority (can halt workflow)
- **Single Source of Truth**: After conversion, SKILL.md should ONLY reference execute.sh, never duplicate the bash code
- **File Size**: Large skills with 300+ lines of bash may need multiple prerequisite checks and well-organized main logic sections
- **Error Handling**: All scripts must have comprehensive error messages that point to SKILL.md for troubleshooting

## Current Status Summary

**Converted**: 1/9 (11%)
**Remaining**: 8/9 (89%)
**Executable**: 6/14 total skills now have execute.sh files

**Next**: Convert phase-3-validator (HIGH priority Implementation Phase validator)
