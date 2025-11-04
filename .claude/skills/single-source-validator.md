---
name: single-source-validator
description: ENFORCEMENT tool that detects when Skills automation is duplicated in agent definitions, lessons learned, or process docs. Prevents "single source of truth nightmare" by finding bash commands, step-by-step procedures, or process descriptions that replicate Skills. BLOCKING AUTHORITY - workflow cannot complete with violations.
---

# Single Source of Truth Validator Skill

**Purpose**: ENFORCE single source of truth for Skills - prevent duplication nightmare.

**When to Use**:
- After creating any new Skill
- Before Phase 5 finalization (MANDATORY)
- When updating agent definitions
- When updating lessons learned
- During periodic audits (monthly)

## 🚨 CRITICAL: ENFORCEMENT MECHANISM

**This skill has BLOCKING AUTHORITY.**

**If violations found:**
- ❌ Phase 5 validation FAILS
- ❌ Workflow CANNOT complete
- ❌ Changes MUST NOT be committed

**Why this matters:**
- Skills = ONLY source of automation
- All other docs REFERENCE skills, never duplicate
- Agents ignore guides when procedures are scattered
- User feedback: "SUPER CRITICAL" - duplication causes nightmare

---

## Single Source of Truth Architecture

```
SKILLS (/.claude/skills/*.md)
    Automation ONLY
    Executable bash scripts
    Zero duplication
         ↑
         |
    Referenced by
         |
         ↓
AGENT DEFINITIONS (/.claude/agents/*.md)
    Role + Tools + Skill References
    "Use skill X for Y"
    NEVER duplicate procedures

LESSONS LEARNED (/docs/lessons-learned/*.md)
    Problem → Solution → Example
    "See: /.claude/skills/X.md"
    NEVER duplicate procedures

PROCESS DOCS (/docs/standards-processes/*.md)
    Workflow + References
    "Automation: /.claude/skills/X.md"
    NEVER duplicate procedures
```

---

## What This Validates

This skill checks for **8 types** of single-source violations:

1. **Bash Command Duplication** - Exact bash commands from skills appearing in agent definitions, lessons learned, or process docs
2. **Step-by-Step Procedures** - Numbered or bulleted procedure steps duplicating skill automation
3. **Hardcoded Skill Paths** - Direct file path references (/.claude/skills/X.md) instead of skill name references
4. **Detailed Agent Skill Sections** - Agent definitions with When:/What:/Location: patterns (should be reference-only)
5. **Procedural Keyword Duplication** - Skill-specific procedures duplicated in other documents
6. **Missing Proper References** - Documents should say "Use X skill" or "See SKILLS-REGISTRY.md"
7. **Bash Scripts in Non-Skill Docs** - Bash code blocks in guides without skill references (NEW)
8. **Procedure Sections Without Skill References** - "How to", "Steps to", "Procedure" sections not referencing skills (NEW)

**Detection Levels:**
- ❌ **VIOLATIONS** (CRITICAL) - Exact duplications, blocks Phase 5
- ⚠️ **WARNINGS** - Potential issues, manual review recommended

---

## Validation Script

```bash
#!/bin/bash
# Single Source of Truth Validator
# ENFORCEMENT: Detects Skills duplication in other documents

set -e  # Exit on error

SKILL_NAME="${1:-}"
SEVERITY="${2:-CRITICAL}"  # CRITICAL, WARNING, INFO

if [ -z "$SKILL_NAME" ]; then
    echo "Usage: bash single-source-validator.md SKILL_NAME [SEVERITY]"
    echo "Example: bash single-source-validator.md container-restart"
    exit 1
fi

echo "🔍 Single Source of Truth Validator"
echo "===================================="
echo ""
echo "Skill: $SKILL_NAME"
echo "Severity: $SEVERITY"
echo ""

# Configuration
SKILL_FILE="/.claude/skills/${SKILL_NAME}.md"
VIOLATIONS=0
WARNINGS=0

# Check skill exists
if [ ! -f "$SKILL_FILE" ]; then
    echo "❌ ERROR: Skill not found: $SKILL_FILE"
    exit 1
fi

echo "✅ Skill found: $SKILL_FILE"
echo ""

# Step 1: Extract key indicators from skill
echo "1️⃣  Extracting key indicators from skill..."
echo ""

# Extract bash commands (lines starting with docker, npm, bash, etc.)
BASH_COMMANDS=$(grep -E "^(docker|npm|bash|./|curl|psql|ssh)" "$SKILL_FILE" | head -20 || true)
COMMAND_COUNT=$(echo "$BASH_COMMANDS" | wc -l)

echo "   Found $COMMAND_COUNT bash commands in skill"
echo ""

# Step 2: Search agent definitions
echo "2️⃣  Checking agent definitions..."
echo ""

AGENT_DIR="/.claude/agents"
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
        if grep -F "/.claude/skills/${SKILL_NAME}.md" "$AGENT_FILE" > /dev/null 2>&1; then
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

LESSONS_DIR="/docs/lessons-learned"
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
        if grep -F "/.claude/skills/${SKILL_NAME}.md" "$LESSON_FILE" > /dev/null 2>&1; then
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
    "/docs/standards-processes"
    "/docs/functional-areas"
    "/docs/guides-setup"
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
        if grep -F "/.claude/skills/${SKILL_NAME}.md" "$PROCESS_FILE" > /dev/null 2>&1; then
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

# Step 7: Check for procedural duplication patterns
echo "7️⃣  Checking for procedural duplication patterns..."
echo ""

# Extract skill procedure keywords
SKILL_KEYWORDS=$(grep -E "^(## |### )" "$SKILL_FILE" | sed 's/^#* //' | tr '[:upper:]' '[:lower:]' || true)

# Search for matching procedure sections
ALL_FILES=$(find .claude/agents docs/lessons-learned docs/standards-processes docs/functional-areas -name "*.md" 2>/dev/null || true)

while IFS= read -r KEYWORD; do
    if [ -z "$KEYWORD" ]; then continue; fi

    # Only check significant keywords (not common words)
    if [[ "$KEYWORD" =~ (the|and|for|with|how|what) ]]; then
        continue
    fi

    MATCHES=$(grep -il "$KEYWORD" $ALL_FILES 2>/dev/null | grep -v "$SKILL_FILE" || true)

    if [ -n "$MATCHES" ]; then
        echo "   ⚠️  WARNING: Keyword '$KEYWORD' found in other files"
        echo "      Files: $(echo $MATCHES | tr '\n' ' ')"
        echo "      Review manually for procedure duplication"
        echo ""
        ((WARNINGS++))
    fi
done <<< "$SKILL_KEYWORDS"

echo ""

# Step 7.5: Check for bash scripts in non-skill documentation
echo "7️⃣.5 Checking for bash scripts in non-skill documentation..."
echo ""

# Find non-skill markdown files with bash code blocks
NON_SKILL_DOCS=$(find docs/ -name "*.md" 2>/dev/null || true)

for DOC_FILE in $NON_SKILL_DOCS; do
    DOC_NAME=$(basename "$DOC_FILE" .md)

    # Check if file contains bash code blocks
    BASH_BLOCKS=$(grep -c '```bash' "$DOC_FILE" 2>/dev/null || true)

    if [ "$BASH_BLOCKS" -gt 0 ]; then
        # Check if file references any skills
        HAS_SKILL_REF=$(grep -E "(Use .* skill|See .* skill|skill automates|SKILLS-REGISTRY)" "$DOC_FILE" 2>/dev/null || true)

        if [ -z "$HAS_SKILL_REF" ]; then
            echo "   ⚠️  WARNING: Doc '$DOC_NAME' has $BASH_BLOCKS bash blocks but no skill references"
            echo "      File: $DOC_FILE"
            echo "      Review: Should bash procedures be in a skill?"
            echo ""
            ((WARNINGS++))
        fi
    fi
done

echo ""

# Step 7.6: Check for procedure sections without skill references
echo "7️⃣.6 Checking for procedure sections without skill references..."
echo ""

for DOC_FILE in $NON_SKILL_DOCS; do
    DOC_NAME=$(basename "$DOC_FILE" .md)

    # Check for procedure section headers
    PROC_HEADERS=$(grep -E "^## (How to|Steps to|Procedure|Installation|Setup|Configuration)" "$DOC_FILE" 2>/dev/null || true)

    if [ -n "$PROC_HEADERS" ]; then
        # Check if document references skills in those sections
        HAS_NEARBY_REF=$(grep -A 10 -E "^## (How to|Steps to|Procedure|Installation|Setup|Configuration)" "$DOC_FILE" | grep -E "(Use .* skill|See SKILLS-REGISTRY)" 2>/dev/null || true)

        if [ -z "$HAS_NEARBY_REF" ]; then
            echo "   ⚠️  WARNING: Doc '$DOC_NAME' has procedure sections without skill references"
            echo "      File: $DOC_FILE"
            echo "      Sections: $PROC_HEADERS"
            echo "      Review: Should procedures reference existing skills?"
            echo ""
            ((WARNINGS++))
        fi
    fi
done

echo ""

# Step 8: Summary
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
    echo "3. Replace with reference: See: /.claude/skills/${SKILL_NAME}.md"
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
```

---

## Proper Reference Format

### ❌ WRONG - Duplication

**Agent definition** (test-executor.md):
```markdown
## Before E2E Tests

Run these steps:
1. Stop containers: `docker-compose down`
2. Start with dev overlay: `docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d`
3. Wait 15 seconds
4. Check health: `curl http://localhost:5173`
```

**Why wrong**: Duplicates bash commands from container-restart skill

### ✅ CORRECT - Reference

**Agent definition** (test-executor.md):
```markdown
## Before E2E Tests

**MANDATORY**: Use container-restart skill to ensure environment is healthy.

**See**: `/.claude/skills/container-restart.md`

The skill automatically:
- Stops containers correctly
- Starts with dev overlay
- Checks compilation errors
- Verifies health endpoints
```

**Why correct**: References skill, provides context, no duplication

---

## Lessons Learned Format

### ❌ WRONG - Duplication

**Lesson** (test-executor-lessons-learned.md):
```markdown
## Problem: E2E Tests Fail

**Solution**:
1. Run `docker-compose down`
2. Run `docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d`
3. Check logs for compilation errors
4. Verify health endpoints
```

**Why wrong**: Step-by-step procedure duplicates skill

### ✅ CORRECT - Reference

**Lesson** (test-executor-lessons-learned.md):
```markdown
## Problem: E2E Tests Fail with "Element Not Found"

**Root Cause**: Container shows "Up" but code has compilation errors inside.

**Solution**: Use `container-restart` skill before E2E tests.

The skill automatically checks for compilation errors and verifies health.

**See**: `/.claude/skills/container-restart.md`

**Example**:
```bash
# In test-executor workflow
bash .claude/skills/container-restart.md
# Then run E2E tests
npm run test:e2e
```
```

**Why correct**: Explains problem/solution, references skill, shows usage example

---

## Process Documentation Format

### ❌ WRONG - Duplication

**Process doc** (docker-operations-guide.md):
```markdown
## Container Restart Procedure

Steps:
1. Stop: `docker-compose -f docker-compose.yml -f docker-compose.dev.yml down`
2. Start: `docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d`
3. Verify: Check container logs for errors
4. Health: Test all endpoints
```

**Why wrong**: Detailed procedure duplicates skill automation

### ✅ CORRECT - Reference

**Process doc** (docker-operations-guide.md):
```markdown
## Container Restart Procedure

**Automation**: `/.claude/skills/container-restart.md`

The skill provides automated container restart with:
- Correct docker-compose commands (dev overlay)
- Compilation error checking (critical for E2E tests)
- Health endpoint verification
- Database seed data validation

**When to use**: Before E2E tests, after code changes, when containers unhealthy

**Manual procedure**: See skill file for troubleshooting steps if automation fails
```

**Why correct**: References skill as automation, provides context, no duplication

---

## Integration with Phase 5 Validator

**Update to phase-5-validator.md**:

```bash
# Step: Check for single source of truth violations
echo "Checking single source of truth compliance..."

# Get list of all skills
SKILLS=$(ls /.claude/skills/*.md 2>/dev/null | grep -v "README.md" || true)

for SKILL_FILE in $SKILLS; do
    SKILL_NAME=$(basename "$SKILL_FILE" .md)

    echo "Validating skill: $SKILL_NAME"

    # Run validator
    bash /.claude/skills/single-source-validator.md "$SKILL_NAME" CRITICAL

    if [ $? -ne 0 ]; then
        echo "❌ VIOLATION: Duplication detected for skill: $SKILL_NAME"
        echo "Phase 5 validation BLOCKED"
        exit 1
    fi
done

echo "✅ All skills maintain single source of truth"
```

---

## Validation Workflow

### For New Skills

```bash
# 1. Create new skill
vim /.claude/skills/my-new-skill.md

# 2. Validate immediately
bash /.claude/skills/single-source-validator.md my-new-skill

# 3. Fix any violations before committing
```

### For Existing Skills

```bash
# Run validator on specific skill
bash /.claude/skills/single-source-validator.md container-restart

# Or validate all skills
for skill in /.claude/skills/*.md; do
    name=$(basename "$skill" .md)
    if [ "$name" != "README" ]; then
        bash /.claude/skills/single-source-validator.md "$name"
    fi
done
```

### Before Phase 5 Completion

```bash
# MANDATORY: Validate all skills
# Integrated into phase-5-validator.md
# Blocks workflow if violations found
```

---

## Common Violations & Fixes

### Violation 1: Agent Definition Has Bash Commands

**Violation**:
```
❌ VIOLATION: Agent 'test-executor' duplicates bash command
   File: /.claude/agents/test-executor.md
   Command: docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

**Fix**:
```bash
# 1. Open agent file
vim /.claude/agents/test-executor.md

# 2. Remove bash command section

# 3. Replace with reference
# Before E2E Tests
# Use container-restart skill: /.claude/skills/container-restart.md

# 4. Validate
bash /.claude/skills/single-source-validator.md container-restart
```

### Violation 2: Lessons Learned Has Procedure Steps

**Violation**:
```
❌ VIOLATION: Lesson 'test-executor-lessons-learned' duplicates bash command
   File: /docs/lessons-learned/test-executor-lessons-learned.md
   Command: docker logs witchcity-web --tail 50 | grep -i error
```

**Fix**:
```bash
# 1. Open lesson file
vim /docs/lessons-learned/test-executor-lessons-learned.md

# 2. Remove step-by-step procedure

# 3. Replace with Problem→Solution→Example format
## Problem: Containers start but tests fail
**Root Cause**: Compilation errors inside container
**Solution**: Use container-restart skill (checks compilation)
**See**: /.claude/skills/container-restart.md

# 4. Validate
bash /.claude/skills/single-source-validator.md container-restart
```

### Violation 3: Process Doc Duplicates Automation

**Violation**:
```
❌ VIOLATION: Process doc 'staging-deployment-guide' duplicates bash command
   File: /docs/functional-areas/deployment/staging-deployment-guide.md
   Command: docker build -f apps/api/Dockerfile -t registry.digitalocean.com/...
```

**Fix**:
```bash
# 1. Open process doc
vim /docs/functional-areas/deployment/staging-deployment-guide.md

# 2. Remove bash script section

# 3. Replace with reference to automation
**Automation**: /.claude/skills/staging-deploy.md
The skill handles: build, registry push, deployment, health checks

**Manual procedure**: This guide covers context and troubleshooting

# 4. Validate
bash /.claude/skills/single-source-validator.md staging-deploy
```

---

## Severity Levels

### CRITICAL (Default)
- **Exact bash command matches**: Always blocked
- **Step-by-step procedures**: Always blocked
- **Warnings treated as failures**: Yes
- **Use for**: Phase 5 validation, skill creation

### WARNING
- **Exact bash command matches**: Blocked
- **Step-by-step procedures**: Blocked
- **Warnings treated as failures**: No (manual review)
- **Use for**: Periodic audits, exploratory checks

### INFO
- **Exact bash command matches**: Reported only
- **Step-by-step procedures**: Reported only
- **Warnings treated as failures**: No
- **Use for**: Documentation reviews, planning

---

## Agent Instructions

### All Agents (Mandatory)

**Before modifying agent definitions, lessons learned, or process docs:**

```bash
# Check if skill exists for this procedure
ls /.claude/skills/ | grep -i "relevant-keyword"

# If skill exists, REFERENCE it, don't duplicate
echo "See: /.claude/skills/[skill-name].md" >> file.md

# If no skill exists and procedure is complex, CREATE SKILL FIRST
vim /.claude/skills/new-procedure.md

# Then reference from other docs
```

### Orchestrator (Phase 5)

**MANDATORY validation before finalization:**

```bash
# Run validator on ALL skills
echo "Validating single source of truth..."

for skill in /.claude/skills/*.md; do
    name=$(basename "$skill" .md)
    if [ "$name" != "README" ] && [ "$name" != "single-source-validator" ]; then
        bash /.claude/skills/single-source-validator.md "$name" CRITICAL
        if [ $? -ne 0 ]; then
            echo "❌ Phase 5 validation BLOCKED due to violations in: $name"
            exit 1
        fi
    fi
done

echo "✅ Single source of truth maintained across all skills"
```

### Librarian (File Organization)

**When creating/moving documentation:**

```bash
# Before creating new procedure doc
# 1. Check if skill exists
bash /.claude/skills/single-source-validator.md [relevant-skill] INFO

# 2. If skill exists, reference it
# 3. If no skill, consider if automation needed
# 4. If automation needed, create skill FIRST
```

---

## Metrics & Monitoring

### Success Metrics

- **Zero violations** in Phase 5 validation (target: 100%)
- **Skills referenced** vs **procedures duplicated** ratio (target: >95%)
- **Agent compliance** - agents using skills vs. duplicating (target: 100%)

### Monitoring Commands

```bash
# Count total skills
SKILL_COUNT=$(ls /.claude/skills/*.md | wc -l)

# Count references to skills
REF_COUNT=$(grep -r "\.claude/skills/" /.claude/agents /docs/lessons-learned /docs/standards-processes | wc -l)

# Count violations (should be 0)
VIOLATION_COUNT=0
for skill in /.claude/skills/*.md; do
    name=$(basename "$skill" .md)
    if [ "$name" != "README" ] && [ "$name" != "single-source-validator" ]; then
        bash /.claude/skills/single-source-validator.md "$name" CRITICAL > /dev/null 2>&1
        if [ $? -ne 0 ]; then
            ((VIOLATION_COUNT++))
        fi
    fi
done

echo "Skills: $SKILL_COUNT"
echo "References: $REF_COUNT"
echo "Violations: $VIOLATION_COUNT"
```

---

## Troubleshooting

### Issue: Validator reports false positive

**Cause**: Skill and doc have similar bash command for different purpose

**Solution**:
1. Review context around command in both files
2. If different purpose, add comment in doc: `# Different from skill: [explanation]`
3. Run validator with WARNING severity for manual review

### Issue: Too many warnings, hard to review

**Cause**: Keyword-based detection is overly broad

**Solution**:
1. Focus on VIOLATIONS first (exact bash commands)
2. Review warnings manually during periodic audits
3. Refine keyword extraction logic if needed

### Issue: Validator missed duplication

**Cause**: Procedure rewritten in different style (paraphrased)

**Solution**:
1. Manual code review remains important
2. Validator catches exact duplicates, not paraphrases
3. Educate agents on spirit of single source of truth

---

## Version History

- **2025-11-04**: Created as ENFORCEMENT mechanism for single source of truth
- Addresses user feedback: "SUPER CRITICAL" - agents ignore guides when procedures scattered
- Provides BLOCKING AUTHORITY - workflow cannot complete with violations
- Integrated with phase-5-validator for mandatory validation

---

**Remember**: This skill has blocking authority. Zero tolerance for duplication. Skills are the ONLY source of automation.
