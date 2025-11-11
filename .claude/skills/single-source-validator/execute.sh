#!/bin/bash
# Single Source of Truth Validator
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# ENFORCEMENT tool that detects when Skills automation is duplicated in agent definitions,
# lessons learned, or process docs. Prevents "single source of truth nightmare" by finding
# bash commands, step-by-step procedures, or process descriptions that replicate Skills.
# BLOCKING AUTHORITY - workflow cannot complete with violations.

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

if [ "$#" -eq 0 ] || [ "$1" == "--help" ]; then
    echo "🔍 Single Source of Truth Validator"
    echo "===================================="
    echo ""
    echo "📋 Purpose: ENFORCE single source of truth for Skills - prevent duplication nightmare"
    echo ""
    echo "🚨 BLOCKING AUTHORITY: This validator can BLOCK workflow completion for:"
    echo "   • Exact bash command duplication (Skills → Agent definitions/Lessons/Docs)"
    echo "   • Step-by-step procedures duplicating skill automation"
    echo "   • Hardcoded skill file paths instead of skill name references"
    echo "   • Detailed Skills sections in agent definitions"
    echo ""
    echo "✅ Use when:"
    echo "   • After creating any new Skill"
    echo "   • Before Phase 5 finalization (MANDATORY)"
    echo "   • When updating agent definitions"
    echo "   • When updating lessons learned"
    echo "   • During periodic audits (monthly)"
    echo ""
    echo "❌ DO NOT use if:"
    echo "   • Skill doesn't exist yet (create it first)"
    echo "   • Just checking documentation quality (use other validators)"
    echo ""
    echo "⚙️  What this script does:"
    echo "   1. Extracts bash commands from skill file"
    echo "   2. Searches agent definitions for duplication"
    echo "   3. Searches lessons learned for duplication"
    echo "   4. Searches process docs for duplication"
    echo "   5. Checks for hardcoded skill paths"
    echo "   6. Validates agent Skills sections format"
    echo "   7. Reports violations and warnings"
    echo "   8. Blocks workflow if violations found"
    echo ""
    echo "📝 Usage:"
    echo "   bash execute.sh <skill-name> [severity]"
    echo ""
    echo "   skill-name: Name of skill to validate (e.g., container-restart)"
    echo "   severity: CRITICAL|WARNING|INFO (default: CRITICAL)"
    echo ""
    echo "   Examples:"
    echo "   bash execute.sh container-restart"
    echo "   bash execute.sh staging-deploy CRITICAL"
    echo "   bash execute.sh phase-1-validator WARNING"
    echo ""
    exit 1
fi

# ============================================
# PARAMETER EXTRACTION
# ============================================

SKILL_NAME="$1"
SEVERITY="${2:-CRITICAL}"  # CRITICAL, WARNING, INFO

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "🔍 Single Source of Truth Validator"
echo "===================================="
echo ""

echo "1️⃣  Checking skill exists..."
SKILL_FILE=".claude/skills/${SKILL_NAME}/SKILL.md"
if [ ! -f "$SKILL_FILE" ]; then
    echo "   ❌ FAIL: Skill not found: $SKILL_FILE"
    exit 1
fi
echo "   ✅ Skill found: $SKILL_FILE"

echo ""
echo "2️⃣  Checking severity level..."
if ! echo "$SEVERITY" | grep -qE "^(CRITICAL|WARNING|INFO)$"; then
    echo "   ⚠️  Unknown severity: $SEVERITY (using CRITICAL)"
    SEVERITY="CRITICAL"
fi
echo "   ✅ Severity: $SEVERITY"

echo ""
echo "✅ All prerequisites passed"
echo ""

# ============================================
# MAIN SCRIPT - VALIDATION
# ============================================

echo "Skill: $SKILL_NAME"
echo "Severity: $SEVERITY"
echo ""

# Configuration
VIOLATIONS=0
WARNINGS=0

# Step 1: Extract key indicators from skill
echo "1️⃣  Extracting key indicators from skill..."
echo ""

# Extract bash commands (lines starting with docker, npm, bash, etc.)
# Look for execute.sh or embedded bash scripts
if [ -f ".claude/skills/${SKILL_NAME}/execute.sh" ]; then
    BASH_COMMANDS=$(grep -E "^(docker|npm|bash|./|curl|psql|ssh|git|cd |ls |wc |find |grep )" ".claude/skills/${SKILL_NAME}/execute.sh" | head -20 || true)
else
    BASH_COMMANDS=$(grep -E "^(docker|npm|bash|./|curl|psql|ssh|git|cd |ls |wc |find |grep )" "$SKILL_FILE" | head -20 || true)
fi

COMMAND_COUNT=$(echo "$BASH_COMMANDS" | grep -v "^$" | wc -l)
echo "   Found $COMMAND_COUNT bash commands in skill"
echo ""

# Step 2: Search agent definitions
echo "2️⃣  Checking agent definitions..."
echo ""

AGENT_DIR=".claude/agents"
if [ -d "$AGENT_DIR" ]; then
    for AGENT_FILE in "$AGENT_DIR"/*.md; do
        AGENT_NAME=$(basename "$AGENT_FILE" .md)

        # Check for bash command duplication
        while IFS= read -r COMMAND; do
            if [ -z "$COMMAND" ]; then continue; fi

            # Ignore comments and empty lines
            if [[ "$COMMAND" =~ ^# ]] || [[ "$COMMAND" =~ ^$ ]]; then
                continue
            fi

            # Search for exact command match
            if grep -F "$COMMAND" "$AGENT_FILE" > /dev/null 2>&1; then
                echo "   ❌ VIOLATION: Agent '$AGENT_NAME' duplicates bash command"
                echo "      File: $AGENT_FILE"
                echo "      Command: $COMMAND"
                echo ""
                ((VIOLATIONS++))
            fi
        done <<< "$BASH_COMMANDS"

        # Check for step-by-step instructions
        if grep -E "^(Step |1\.|2\.|3\.|4\.|5\.)" "$AGENT_FILE" | grep -i "$(echo $SKILL_NAME | tr '-' ' ')" > /dev/null 2>&1; then
            echo "   ⚠️  WARNING: Agent '$AGENT_NAME' may have step-by-step instructions"
            echo "      File: $AGENT_FILE"
            echo "      Review manually for duplication"
            echo ""
            ((WARNINGS++))
        fi

        # Check for proper reference format
        if grep -F ".claude/skills/${SKILL_NAME}" "$AGENT_FILE" > /dev/null 2>&1; then
            echo "   ✅ Agent '$AGENT_NAME' properly references skill"
        fi
    done
else
    echo "   ⚠️  Agent directory not found: $AGENT_DIR"
fi

echo ""

# Step 3: Search lessons learned
echo "3️⃣  Checking lessons learned..."
echo ""

LESSONS_DIR="docs/lessons-learned"
if [ -d "$LESSONS_DIR" ]; then
    LESSONS_FILES=$(find "$LESSONS_DIR" -name "*.md" 2>/dev/null || true)

    for LESSON_FILE in $LESSONS_FILES; do
        LESSON_NAME=$(basename "$LESSON_FILE" .md)

        # Check for bash command duplication
        while IFS= read -r COMMAND; do
            if [ -z "$COMMAND" ]; then continue; fi
            if [[ "$COMMAND" =~ ^# ]] || [[ "$COMMAND" =~ ^$ ]]; then
                continue
            fi

            if grep -F "$COMMAND" "$LESSON_FILE" > /dev/null 2>&1; then
                echo "   ❌ VIOLATION: Lesson '$LESSON_NAME' duplicates bash command"
                echo "      File: $LESSON_FILE"
                echo "      Command: $COMMAND"
                echo ""
                ((VIOLATIONS++))
            fi
        done <<< "$BASH_COMMANDS"

        # Check for proper reference format
        if grep -F ".claude/skills/${SKILL_NAME}" "$LESSON_FILE" > /dev/null 2>&1; then
            echo "   ✅ Lesson '$LESSON_NAME' properly references skill"
        fi
    done
else
    echo "   ⚠️  Lessons directory not found: $LESSONS_DIR"
fi

echo ""

# Step 4: Search process docs
echo "4️⃣  Checking process documentation..."
echo ""

PROCESS_DIRS=(
    "docs/standards-processes"
    "docs/functional-areas"
    "docs/guides-setup"
)

for PROCESS_DIR in "${PROCESS_DIRS[@]}"; do
    if [ ! -d "$PROCESS_DIR" ]; then continue; fi

    PROCESS_FILES=$(find "$PROCESS_DIR" -name "*.md" 2>/dev/null || true)

    for PROCESS_FILE in $PROCESS_FILES; do
        PROCESS_NAME=$(basename "$PROCESS_FILE" .md)

        # Check for bash command duplication
        while IFS= read -r COMMAND; do
            if [ -z "$COMMAND" ]; then continue; fi
            if [[ "$COMMAND" =~ ^# ]] || [[ "$COMMAND" =~ ^$ ]]; then
                continue
            fi

            if grep -F "$COMMAND" "$PROCESS_FILE" > /dev/null 2>&1; then
                echo "   ❌ VIOLATION: Process doc '$PROCESS_NAME' duplicates bash command"
                echo "      File: $PROCESS_FILE"
                echo "      Command: $COMMAND"
                echo ""
                ((VIOLATIONS++))
            fi
        done <<< "$BASH_COMMANDS"

        # Check for proper reference format
        if grep -F ".claude/skills/${SKILL_NAME}" "$PROCESS_FILE" > /dev/null 2>&1; then
            echo "   ✅ Process doc '$PROCESS_NAME' properly references skill"
        fi
    done
done

echo ""

# Step 5: Check for hardcoded skill paths (CRITICAL)
echo "5️⃣  Checking for hardcoded skill paths..."
echo ""

# Search for hardcoded references to THIS skill file
HARDCODED_REFS=$(grep -rn "\.claude/skills/${SKILL_NAME}\.md" docs/lessons-learned/ docs/standards-processes/ docs/functional-areas/ 2>/dev/null || true)

if [ -n "$HARDCODED_REFS" ]; then
    echo "   ❌ VIOLATION: Hardcoded skill paths found"
    echo "      Files should reference SKILLS-REGISTRY.md, not individual skill files"
    echo ""
    echo "$HARDCODED_REFS" | while IFS= read -r LINE; do
        FILE=$(echo "$LINE" | cut -d: -f1)
        LINENUM=$(echo "$LINE" | cut -d: -f2)
        echo "      File: $FILE (line $LINENUM)"
    done
    echo ""
    echo "   Fix: Replace with 'See SKILLS-REGISTRY.md' or 'Use ${SKILL_NAME} skill'"
    echo ""
    ((VIOLATIONS++))
fi

echo ""

# Step 6: Check agent definitions for detailed Skills sections
echo "6️⃣  Checking agent definitions for detailed Skills sections..."
echo ""

if [ -d ".claude/agents" ]; then
    DETAILED_SECTIONS=$(find .claude/agents -name "*.md" -type f -exec grep -l "## Available Skills" {} \; 2>/dev/null || true)

    for AGENT_FILE in $DETAILED_SECTIONS; do
        AGENT_NAME=$(basename "$AGENT_FILE" .md)

        # Check if agent has detailed format (When:/What:/Location: pattern)
        if grep -A 30 "## Available Skills" "$AGENT_FILE" | grep -qE "(When:|What:|Location:|Critical:)"; then
            echo "   ❌ VIOLATION: Agent '$AGENT_NAME' has detailed Skills section"
            echo "      File: $AGENT_FILE"
            echo "      Should be reference-only format (just skill names + link to registry)"
            echo ""
            ((VIOLATIONS++))
        fi
    done
fi

echo ""

# Step 7: Summary
echo "📊 Validation Summary"
echo "===================="
echo ""
echo "Skill: $SKILL_NAME"
echo "Violations: $VIOLATIONS"
echo "Warnings: $WARNINGS"
echo ""

if [ $VIOLATIONS -gt 0 ]; then
    echo "❌ VALIDATION FAILED"
    echo ""
    echo "🚨 CRITICAL: Single source of truth violations detected!"
    echo ""
    echo "Actions required:"
    echo "1. Review each violation listed above"
    echo "2. Remove duplicated bash commands from agent definitions/lessons/process docs"
    echo "3. Replace with reference: See: .claude/skills/${SKILL_NAME}/SKILL.md"
    echo "4. Run validator again to confirm fixes"
    echo ""
    echo "Phase 5 validation CANNOT complete until violations resolved."
    echo ""
    exit 1
fi

if [ $WARNINGS -gt 0 ]; then
    echo "⚠️  VALIDATION PASSED WITH WARNINGS"
    echo ""
    echo "Manual review recommended:"
    echo "1. Check warning items for potential duplication"
    echo "2. Ensure proper skill references in place"
    echo "3. Update any outdated documentation"
    echo ""

    if [ "$SEVERITY" = "CRITICAL" ]; then
        echo "Severity set to CRITICAL - treating warnings as failures"
        exit 1
    fi

    exit 0
fi

echo "✅ VALIDATION PASSED"
echo ""
echo "Single source of truth maintained:"
echo "• No bash command duplication"
echo "• No procedural duplication"
echo "• Proper skill references in place"
echo ""

exit 0
