---
name: lessons-learned-validator
description: Complete lessons learned standards, validation, and multi-file management. Single source of truth for all lessons learned operations including format, size limits, split procedures, and quality standards.
---

# Lessons Learned Validator Skill

**Purpose**: Single source of truth for ALL lessons learned standards, validation, and management.

**When to Use**:
- Before committing lesson updates
- During Phase 5 finalization (MANDATORY)
- When adding new lessons
- When files approach size limits
- When splitting files

**This skill contains**:
- ✅ Complete standards and format requirements
- ✅ File size limits and multi-file management
- ✅ Validation logic and quality checks
- ✅ Split procedures and fix instructions
- ✅ Reading/writing protocols

---

## 📋 Lessons Learned Standards

### File Naming Convention

Lessons learned files follow strict naming to ensure consistency and discoverability:

**Pattern**: `[role]-lessons-learned.md`
**Location**: `/docs/lessons-learned/[role]-lessons-learned.md`

**Multi-part files**:
- Part 1: `[role]-lessons-learned.md` (original)
- Part 2: `[role]-lessons-learned-2.md`
- Part 3: `[role]-lessons-learned-3.md`
- Continue sequential numbering as needed

### Supported Roles

The following roles are recognized for lessons learned documentation:

- `backend-developer-lessons-learned.md` - Backend development, API design, server-side logic
- `react-developer-lessons-learned.md` - React development, UI components, client-side functionality
- `test-developer-lessons-learned.md` - Test creation and test suite design
- `test-executor-lessons-learned.md` - Test execution, environment setup, troubleshooting
- `database-designer-lessons-learned.md` - Database design, migrations, data management
- `devops-lessons-learned.md` - Deployment, infrastructure, operational concerns
- `ui-designer-lessons-learned.md` - UI/UX design, wireframes, design systems
- `business-requirements-lessons-learned.md` - Requirements gathering and analysis
- `functional-spec-lessons-learned.md` - Technical specifications and design
- `code-reviewer-lessons-learned.md` - Code review patterns and quality checks
- `git-manager-lessons-learned.md` - Version control and git operations
- `librarian-lessons-learned.md` - Documentation organization and maintenance
- `orchestrator-lessons-learned.md` - Workflow coordination and orchestration
- `technology-researcher-lessons-learned.md` - Technology evaluation and research
- `lint-validator-lessons-learned.md` - Code quality validation
- `prettier-formatter-lessons-learned.md` - Code formatting standards

### Standard Entry Format (MANDATORY)

**Format**: Problem → Solution → Example (PREVENTION pattern)

Each lessons learned entry MUST follow this structure:

```markdown
## Problem: [Brief Description]

**Problem**: Detailed description of what went wrong.

**Root Cause**: Why it happened.

**Solution**: Specific, actionable steps to prevent recurrence.

**Example**:
```bash
# ❌ Wrong approach that caused the problem
command --wrong-flag

# ✅ Correct approach that prevents the problem
command --correct-flag
```
```

### Entry Requirements

1. **Date Format**: Use ISO format (YYYY-MM-DD) for consistency
2. **Context**: Provide enough background for future readers to understand
3. **Actionable**: Each lesson MUST include specific, actionable takeaways
4. **Concrete**: Include code examples, commands, file paths, error messages
5. **Prevention-focused**: Use language like "avoid", "don't", "never", "instead of"
6. **Cross-referenced**: Link to related documentation

### NOT a Lessons Learned

**Don't create lessons for**:
- "How to" instructions → That's a Skill (automation)
- General documentation → That's a guide in /docs/
- Step-by-step procedures → That's a Skill or process doc

**DO create lessons for**:
- What went wrong and why
- Mistakes to avoid
- Better approaches discovered
- Architecture violations that caused problems
- Debugging patterns that worked

### Common Tags

Use these standardized tags to categorize lessons:

- `#critical` - Critical issues that caused significant problems
- `#process` - Process improvements and workflow changes
- `#tooling` - Tool selection and configuration lessons
- `#debugging` - Debugging techniques and troubleshooting
- `#performance` - Performance-related insights
- `#security` - Security considerations and best practices
- `#integration` - Third-party service integration lessons
- `#testing` - Testing strategy and implementation insights
- `#deployment` - Deployment and infrastructure lessons
- `#communication` - Team communication and coordination

---

## 📏 File Size Limits and Multi-File Management

### Size Limits (MANDATORY)

**Maximum file size**: 2,000 lines per file
**Warning threshold**: 1,800 lines (90% of maximum)
**Check before writing**: Always use `wc -l filename` before adding lessons

**Why 2,000 lines?**
- Conservative limit for Claude's 25,000 token read limit
- Ensures files remain readable and maintainable
- Prevents file read errors that block workflows

### Multi-File Structure

When lessons learned files exceed 1,800 lines (warning) or 2,000 lines (maximum), they MUST be split:

**File naming**:
- Part 1: `[role]-lessons-learned.md` (original file)
- Part 2: `[role]-lessons-learned-2.md`
- Part 3: `[role]-lessons-learned-3.md`
- Part N: `[role]-lessons-learned-N.md`

**Each part MUST**:
- Have a multi-file header (see format below)
- Reference all other parts
- Specify which part to write to
- Stay under 2,000 lines

### Part 1 Header Format (REQUIRED)

**Every multi-file lessons learned MUST have this header in Part 1**:

```markdown
## 📚 MULTI-FILE LESSONS LEARNED
**Files**: 3 total
**Part 1**: [role]-lessons-learned.md (THIS FILE)
**Part 2**: [role]-lessons-learned-2.md (MUST READ)
**Part 3**: [role]-lessons-learned-3.md (MUST READ)
**Read ALL**: Parts 1, 2, AND 3 are MANDATORY
**Write to**: Part 3 ONLY
**Maximum file size**: 2,000 lines per file
**IF READ FAILS**: STOP and use lessons-learned-validator skill to fix immediately
```

### Part 2+ Header Format (REQUIRED)

```markdown
## 📚 MULTI-FILE LESSONS LEARNED
**Files**: 3 total
**Part 2**: [role]-lessons-learned-2.md (THIS FILE)
**Part 1**: [role]-lessons-learned.md (MUST READ FIRST)
**Part 3**: [role]-lessons-learned-3.md (MUST ALSO READ)
**Read ALL**: Parts 1, 2, AND 3 are MANDATORY
**Write to**: Part 3 ONLY
**Maximum file size**: 2,000 lines per file
**IF READ FAILS**: STOP and use lessons-learned-validator skill to fix immediately
```

### Reading Protocol (MANDATORY)

**BEFORE doing ANY work, agents MUST**:

1. **Read Part 1** to get file count from header
2. **Read ALL parts** in sequence (Part 1, 2, 3, ..., N)
3. **IF ANY FILE FAILS TO READ**: STOP IMMEDIATELY
   - DO NOT proceed with any work
   - Use lessons-learned-validator skill to check file
   - Fix the problem (split if too large, create if missing)
   - Update Part 1 header
   - Re-read ALL files to verify
   - ONLY THEN proceed with original task

### Writing Protocol (MANDATORY)

**ALWAYS write to the LAST file in the series**:

1. **Check Part 1 header** to identify last file
2. **Check line count** of last file: `wc -l [last-file].md`
3. **If last file < 1,800 lines**: Write to last file (safe)
4. **If last file 1,800-2,000 lines**: Warning - plan split soon
5. **If last file > 2,000 lines**: MUST split before writing

### Split Procedure (When File Exceeds 2,000 Lines)

**Step-by-step split process**:

1. **Check current state**:
   ```bash
   wc -l docs/lessons-learned/[role]-lessons-learned-N.md
   ```

2. **If file > 2,000 lines**, create next part:
   ```bash
   # If Part 2 is full, create Part 3
   touch docs/lessons-learned/[role]-lessons-learned-3.md
   ```

3. **Add header to new part** (see Part 2+ format above)

4. **Move recent lessons to new part**:
   - Move newest 200-400 lines from full part → new part
   - Keep old part under 2,000 lines
   - Preserve lesson structure (don't split mid-lesson)

5. **Update Part 1 header**:
   - Change `**Files**: 2 total` → `**Files**: 3 total`
   - Add reference to Part 3
   - Change `**Write to**: Part 2 ONLY` → `**Write to**: Part 3 ONLY`

6. **Verify all parts readable**:
   ```bash
   wc -l docs/lessons-learned/[role]-lessons-learned*.md
   # All files should be under 2,000 lines
   ```

7. **Test reading all parts** before proceeding

### Hard Block Enforcement (CRITICAL)

**STARTUP VALIDATION GATE - MANDATORY FOR ALL AGENTS**:

```bash
# Set flag
LESSONS_LEARNED_READABLE=false

# Attempt to read ALL lessons learned files for your role
for FILE in docs/lessons-learned/[your-role]-lessons-learned*.md; do
    if ! cat "$FILE" > /dev/null 2>&1; then
        echo "❌ CRITICAL: Cannot read $FILE"
        echo "STOP: Use lessons-learned-validator skill to fix"
        exit 1
    fi
done

# Only when ALL files read successfully
LESSONS_LEARNED_READABLE=true

# ONLY proceed with work if flag is true
if [ "$LESSONS_LEARNED_READABLE" = "true" ]; then
    # Proceed with task
else
    echo "❌ BLOCKED: Cannot proceed until lessons files are readable"
    exit 1
fi
```

### Fix Procedure When File Too Large

**If validator reports file exceeds 2,000 lines**:

1. **Identify the oversized file**:
   ```bash
   find docs/lessons-learned -name "*lessons-learned*.md" -exec wc -l {} \; | sort -rn
   ```

2. **Check if it's the last file in series**:
   - Read Part 1 header to see file count
   - Last file = Part N

3. **Create next part**:
   ```bash
   # If Part 2 is oversized (file count is 2)
   # Create Part 3
   touch docs/lessons-learned/[role]-lessons-learned-3.md
   ```

4. **Add header to new part** with correct file count

5. **Move content**:
   - Calculate lines to move: `LINES - 1800` (leave buffer)
   - Move that many lines from bottom of oversized file to new file
   - Keep lesson entries intact (don't split mid-lesson)

6. **Update Part 1 header** with new file count

7. **Run validator again** to confirm fix

---

## ✅ Validation Checklist

### Structure (20 points)
- [ ] File exists in /docs/lessons-learned/ (5 points)
- [ ] Filename follows pattern: [role]-lessons-learned.md (3 points)
- [ ] Multi-file header present if part of series (4 points)
- [ ] Table of contents present (4 points)
- [ ] Navigation links between parts (4 points)

### Format Compliance (30 points)
- [ ] Each lesson has ## heading (5 points)
- [ ] Problem section present (7 points)
- [ ] Solution section present (7 points)
- [ ] Example section present (7 points)
- [ ] Prevention-focused language (4 points)

### Content Quality (30 points)
- [ ] Problem is specific (7 points)
- [ ] Solution is actionable (8 points)
- [ ] Example is concrete (7 points)
- [ ] Lesson is maintainable (4 points)
- [ ] Cross-references included (4 points)

### Maintenance (20 points)
- [ ] Recent updates documented (5 points)
- [ ] Outdated lessons removed (5 points)
- [ ] Duplicate lessons consolidated (5 points)
- [ ] File size monitored (5 points)

---

## 🤖 Automated Validator Script

```bash
#!/bin/bash
# Lessons Learned Validator
# Single source of truth for lessons learned validation

LESSONS_FILE="$1"
SCORE=0
MAX_SCORE=100

if [ -z "$LESSONS_FILE" ]; then
    echo "Usage: $0 <lessons-learned-file.md>"
    exit 1
fi

if [ ! -f "$LESSONS_FILE" ]; then
    echo "❌ Error: File not found: $LESSONS_FILE"
    exit 1
fi

echo "Lessons Learned Validation"
echo "==========================="
echo "File: $LESSONS_FILE"
echo ""

# Structure validation
echo "Structure Validation"
echo "--------------------"

# File location
if [[ "$LESSONS_FILE" == *"/docs/lessons-learned/"* ]]; then
    echo "✅ File in correct location"
    ((SCORE+=5))
else
    echo "❌ File not in /docs/lessons-learned/"
fi

# Filename pattern
BASENAME=$(basename "$LESSONS_FILE")
if [[ "$BASENAME" =~ ^[a-z-]+-lessons-learned\.md$ ]] || [[ "$BASENAME" =~ ^[a-z-]+-lessons-learned-[0-9]+\.md$ ]]; then
    echo "✅ Filename follows pattern"
    ((SCORE+=3))
else
    echo "❌ Filename doesn't follow pattern: [role]-lessons-learned.md"
fi

# File size check (LINE COUNT - CRITICAL)
LINE_COUNT=$(wc -l < "$LESSONS_FILE")
MAX_LINES=2000
WARNING_THRESHOLD=1800  # 90% of max

echo "File size: $LINE_COUNT lines (max: $MAX_LINES)"

if [ "$LINE_COUNT" -gt "$MAX_LINES" ]; then
    echo "❌ CRITICAL: File exceeds maximum line count"
    echo "   Current: $LINE_COUNT lines"
    echo "   Maximum: $MAX_LINES lines"
    echo "   MANDATORY: Split file immediately"
    echo "   See split procedure in this skill"
    # No points awarded - this is a blocking violation
elif [ "$LINE_COUNT" -gt "$WARNING_THRESHOLD" ]; then
    echo "⚠️  WARNING: Approaching line limit"
    echo "   Current: $LINE_COUNT lines ($((LINE_COUNT * 100 / MAX_LINES))% of limit)"
    echo "   Warning threshold: $WARNING_THRESHOLD lines"
    echo "   Action: Plan file split soon"
    ((SCORE+=2))
else
    echo "✅ Line count within limits ($LINE_COUNT / $MAX_LINES lines)"
    ((SCORE+=4))
fi

# Multi-file structure validation
if [ "$LINE_COUNT" -gt 1000 ] || [[ "$BASENAME" =~ -[0-9]+\.md$ ]]; then
    if grep -q "## 📚 MULTI-FILE LESSONS LEARNED" "$LESSONS_FILE" || grep -q "Part [0-9]" "$LESSONS_FILE"; then
        echo "✅ Multi-file structure documented"
    else
        echo "⚠️  Large file or multi-part without proper header"
        echo "   Add multi-file structure header"
    fi
fi

# Table of contents
if grep -q "## Table of Contents" "$LESSONS_FILE" || grep -q "## Contents" "$LESSONS_FILE"; then
    echo "✅ Table of contents present"
    ((SCORE+=4))
else
    echo "⚠️  No table of contents"
    ((SCORE+=2))
fi

# Navigation links (for multi-part files)
if [[ "$BASENAME" =~ -[0-9]+\.md$ ]]; then
    if grep -q "← Previous:" "$LESSONS_FILE" && grep -q "→ Next:" "$LESSONS_FILE"; then
        echo "✅ Navigation links present"
        ((SCORE+=4))
    else
        echo "⚠️  Multi-part file missing navigation links"
        ((SCORE+=2))
    fi
else
    ((SCORE+=4))  # Not applicable for single files
fi

echo ""

# Format compliance
echo "Format Compliance"
echo "-----------------"

# Count lessons (## headings excluding ToC, Conclusion, etc.)
LESSON_COUNT=$(grep -c "^## [^#]" "$LESSONS_FILE" | grep -v "Table of Contents\|Conclusion\|Multi-File" || echo 0)
echo "Found $LESSON_COUNT lessons"

if [ "$LESSON_COUNT" -eq 0 ]; then
    echo "❌ No lessons found"
    exit 1
fi

# Check for Problem/Solution/Example pattern
PROBLEM_COUNT=$(grep -c "### Problem\|**Problem:**" "$LESSONS_FILE")
SOLUTION_COUNT=$(grep -c "### Solution\|**Solution:**" "$LESSONS_FILE")
EXAMPLE_COUNT=$(grep -c "### Example\|**Example:**" "$LESSONS_FILE")

if [ "$PROBLEM_COUNT" -ge 3 ]; then
    echo "✅ Problem sections present ($PROBLEM_COUNT)"
    ((SCORE+=7))
elif [ "$PROBLEM_COUNT" -gt 0 ]; then
    echo "⚠️  Limited Problem sections ($PROBLEM_COUNT)"
    ((SCORE+=3))
else
    echo "❌ No Problem sections found"
fi

if [ "$SOLUTION_COUNT" -ge 3 ]; then
    echo "✅ Solution sections present ($SOLUTION_COUNT)"
    ((SCORE+=7))
elif [ "$SOLUTION_COUNT" -gt 0 ]; then
    echo "⚠️  Limited Solution sections ($SOLUTION_COUNT)"
    ((SCORE+=3))
else
    echo "❌ No Solution sections found"
fi

if [ "$EXAMPLE_COUNT" -ge 3 ]; then
    echo "✅ Example sections present ($EXAMPLE_COUNT)"
    ((SCORE+=7))
elif [ "$EXAMPLE_COUNT" -gt 0 ]; then
    echo "⚠️  Limited Example sections ($EXAMPLE_COUNT)"
    ((SCORE+=3))
else
    echo "❌ No Example sections found"
fi

# Prevention-focused language
PREVENTION_KEYWORDS=$(grep -ic "prevent\|avoid\|don't\|never\|must not\|instead of" "$LESSONS_FILE")
if [ "$PREVENTION_KEYWORDS" -ge 5 ]; then
    echo "✅ Prevention-focused language ($PREVENTION_KEYWORDS keywords)"
    ((SCORE+=4))
else
    echo "⚠️  Limited prevention focus"
    ((SCORE+=2))
fi

echo ""

# Content quality
echo "Content Quality"
echo "---------------"

# Problem specificity (check for code examples, error messages, file paths)
SPECIFIC_INDICATORS=$(grep -c "Error:\|file:\|line:\|\`\`\`\|TypeError\|Exception" "$LESSONS_FILE")
if [ "$SPECIFIC_INDICATORS" -ge 5 ]; then
    echo "✅ Problems are specific ($SPECIFIC_INDICATORS specific indicators)"
    ((SCORE+=7))
else
    echo "⚠️  Problems could be more specific"
    ((SCORE+=4))
fi

# Solution actionability (check for concrete actions)
ACTION_KEYWORDS=$(grep -ic "run\|execute\|add\|remove\|change\|update\|create\|delete\|fix" "$LESSONS_FILE")
if [ "$ACTION_KEYWORDS" -ge 10 ]; then
    echo "✅ Solutions are actionable ($ACTION_KEYWORDS action verbs)"
    ((SCORE+=8))
else
    echo "⚠️  Solutions could be more actionable"
    ((SCORE+=5))
fi

# Example concreteness (code blocks, commands, file paths)
CODE_BLOCKS=$(grep -c "^\`\`\`" "$LESSONS_FILE")
COMMAND_EXAMPLES=$(grep -c "^\$ \|^# \|bash" "$LESSONS_FILE")
EXAMPLE_SCORE=$((CODE_BLOCKS + COMMAND_EXAMPLES))

if [ "$EXAMPLE_SCORE" -ge 5 ]; then
    echo "✅ Examples are concrete ($CODE_BLOCKS code blocks, $COMMAND_EXAMPLES commands)"
    ((SCORE+=7))
else
    echo "⚠️  Examples could be more concrete"
    ((SCORE+=4))
fi

# Maintainability (date references, version numbers)
DATE_REFERENCES=$(grep -c "2025\|2024\|Date:" "$LESSONS_FILE")
if [ "$DATE_REFERENCES" -gt 0 ]; then
    echo "✅ Lessons have timestamps"
    ((SCORE+=4))
else
    echo "⚠️  No timestamps found"
    ((SCORE+=2))
fi

# Cross-references
CROSS_REFS=$(grep -c "See:\|Related:\|docs/\|Also:" "$LESSONS_FILE")
if [ "$CROSS_REFS" -ge 3 ]; then
    echo "✅ Cross-references present ($CROSS_REFS)"
    ((SCORE+=4))
else
    echo "⚠️  Limited cross-references"
    ((SCORE+=2))
fi

echo ""

# Maintenance
echo "Maintenance Validation"
echo "----------------------"

# Recent updates
LAST_MODIFIED=$(find "$LESSONS_FILE" -mtime -30 2>/dev/null)
if [ -n "$LAST_MODIFIED" ]; then
    echo "✅ Recently updated (within 30 days)"
    ((SCORE+=5))
else
    echo "⚠️  Not updated recently (>30 days)"
    ((SCORE+=2))
fi

# Outdated markers
OUTDATED_COUNT=$(grep -ic "outdated\|deprecated\|no longer\|obsolete" "$LESSONS_FILE")
if [ "$OUTDATED_COUNT" -eq 0 ]; then
    echo "✅ No outdated lessons marked"
    ((SCORE+=5))
else
    echo "⚠️  $OUTDATED_COUNT lessons marked as outdated"
    echo "   Remove or update these lessons"
    ((SCORE+=2))
fi

# Duplicate detection (simple check for repeated ## headings)
DUPLICATE_HEADINGS=$(grep "^## " "$LESSONS_FILE" | sort | uniq -d | wc -l)
if [ "$DUPLICATE_HEADINGS" -eq 0 ]; then
    echo "✅ No duplicate lesson titles"
    ((SCORE+=5))
else
    echo "⚠️  $DUPLICATE_HEADINGS duplicate lesson titles"
    echo "   Consolidate duplicate lessons"
    ((SCORE+=2))
fi

# File size monitoring (LINE COUNT)
# Already calculated: LINE_COUNT, MAX_LINES=2000, WARNING_THRESHOLD=1800
PERCENTAGE_USED=$((LINE_COUNT * 100 / MAX_LINES))

if [ "$LINE_COUNT" -gt "$MAX_LINES" ]; then
    echo "❌ File exceeds maximum ($LINE_COUNT / $MAX_LINES lines - ${PERCENTAGE_USED}%)"
    echo "   CRITICAL: Must split immediately"
    # No points - blocking violation
elif [ "$LINE_COUNT" -gt "$WARNING_THRESHOLD" ]; then
    echo "⚠️  File approaching limit ($LINE_COUNT / $MAX_LINES lines - ${PERCENTAGE_USED}%)"
    echo "   Action: Plan split soon (over 90%)"
    ((SCORE+=3))
else
    echo "✅ File size healthy ($LINE_COUNT / $MAX_LINES lines - ${PERCENTAGE_USED}%)"
    ((SCORE+=5))
fi

# Multi-file structure compliance check
if [[ "$BASENAME" =~ -[0-9]+\.md$ ]]; then
    echo ""
    echo "Multi-File Part Validation"
    echo "---------------------------"

    # Check for MUST READ header
    if grep -q "MUST ALSO READ\|MUST READ" "$LESSONS_FILE"; then
        echo "✅ Part has MUST READ header"
    else
        echo "⚠️  Part missing MUST READ header"
    fi

    # Check for Part references
    if grep -q "Part 1" "$LESSONS_FILE"; then
        echo "✅ References other parts"
    else
        echo "⚠️  No references to other parts"
    fi

    # Check header synchronization
    if grep -q "IF READ FAILS" "$LESSONS_FILE" && grep -q "MANDATORY" "$LESSONS_FILE"; then
        echo "✅ Critical headers present"
    else
        echo "⚠️  Headers may be incomplete"
    fi
fi

echo ""

# BLOCKING VALIDATION: Check for critical violations BEFORE scoring
echo "🚨 Critical Violations Check"
echo "============================="

BLOCKING_VIOLATIONS=0

# Check if file exceeds maximum line count
if [ "$LINE_COUNT" -gt "$MAX_LINES" ]; then
    echo "❌ BLOCKING: File exceeds maximum line count ($LINE_COUNT > $MAX_LINES)"
    ((BLOCKING_VIOLATIONS++))
fi

# Check if outdated lessons are present
if [ "$OUTDATED_COUNT" -gt 5 ]; then
    echo "❌ BLOCKING: Too many outdated lessons ($OUTDATED_COUNT)"
    ((BLOCKING_VIOLATIONS++))
fi

if [ "$BLOCKING_VIOLATIONS" -gt 0 ]; then
    echo ""
    echo "❌ ❌ ❌ VALIDATION BLOCKED ❌ ❌ ❌"
    echo ""
    echo "Critical violations found: $BLOCKING_VIOLATIONS"
    echo ""
    echo "Actions required:"
    if [ "$LINE_COUNT" -gt "$MAX_LINES" ]; then
        echo "1. Split file immediately (exceeds $MAX_LINES line limit)"
        echo "   See 'Split Procedure' section in this skill for instructions"
        echo ""
        echo "   Quick steps:"
        echo "   - Create Part $(($(ls -1 docs/lessons-learned/${BASENAME%-*}-lessons-learned*.md 2>/dev/null | wc -l) + 1)): ${LESSONS_FILE%.md}-$(($(ls -1 docs/lessons-learned/${BASENAME%-*}-lessons-learned*.md 2>/dev/null | wc -l) + 1)).md"
        echo "   - Add multi-file header to new part"
        echo "   - Move recent $((LINE_COUNT - 1800)) lines to new part"
        echo "   - Update Part 1 header with new file count"
        echo "   - Verify all parts under 2,000 lines"
    fi
    if [ "$OUTDATED_COUNT" -gt 5 ]; then
        echo "2. Remove or update outdated lessons"
    fi
    echo ""
    echo "Cannot proceed with workflow until resolved."
    exit 1
fi

echo "✅ No blocking violations"
echo ""

# Calculate percentage
PERCENTAGE=$((SCORE * 100 / MAX_SCORE))

echo "================================"
echo "Final Score: $SCORE / $MAX_SCORE ($PERCENTAGE%)"
echo ""

if [ "$PERCENTAGE" -ge 80 ]; then
    echo "✅ PASS - Lessons learned file meets quality standards"
    echo ""
    if [ "$LINE_COUNT" -gt "$WARNING_THRESHOLD" ]; then
        echo "⚠️  Note: File approaching size limit ($LINE_COUNT / $MAX_LINES lines)"
        echo "   Consider planning file split soon"
    fi
    exit 0
else
    echo "❌ FAIL - Lessons learned file needs improvement"
    echo "   Score: $PERCENTAGE% (need 80%+)"
    echo "   Missing: $((MAX_SCORE - SCORE)) points"
    exit 1
fi
```

---

## 📖 Usage Examples

### From Agent (Self-Validation)

Before committing lessons, validate format and size:

```bash
# Validate your lessons file
bash .claude/skills/lessons-learned-validator.md \
  docs/lessons-learned/[your-role]-lessons-learned.md
```

### Manual Validation

```bash
# Validate specific file
bash .claude/skills/lessons-learned-validator.md \
  docs/lessons-learned/react-developer-lessons-learned.md
```

### Batch Validation

```bash
# Validate all lessons learned files
for file in docs/lessons-learned/*-lessons-learned*.md; do
    echo "Validating: $file"
    bash .claude/skills/lessons-learned-validator.md "$file"
    echo ""
done
```

### Check File Size Before Writing

```bash
# Check if you need to split
LAST_FILE=$(ls -1 docs/lessons-learned/[role]-lessons-learned*.md | tail -1)
LINE_COUNT=$(wc -l < "$LAST_FILE")

if [ "$LINE_COUNT" -gt 1800 ]; then
    echo "⚠️  File approaching limit - plan split soon"
elif [ "$LINE_COUNT" -gt 2000 ]; then
    echo "❌ File exceeds limit - MUST split before writing"
fi
```

---

## 🚨 Common Issues and Fixes

### Issue: "How To" Instead of "Problem/Solution"

**Wrong**:
```markdown
## How to Configure Docker

Run `docker-compose up -d`
```

**Right**:
```markdown
## Problem: Docker Containers Fail to Start

**Problem**: Running `docker-compose up` fails with port conflicts.

**Solution**: Use development compose file overlay:
- Run: `docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d`
- Or use container-restart skill

**Example**:
```bash
# ❌ Wrong - Uses wrong ports
docker-compose up

# ✅ Right - Uses dev ports correctly
bash .claude/skills/container-restart.md
```
```

### Issue: Generic Problems

**Wrong**:
```markdown
**Problem**: Tests fail.
```

**Right**:
```markdown
**Problem**: E2E tests fail with "Element not found" error even though element exists.

Root cause: Docker container has compilation error but still shows "running" status.

Error message: `TimeoutError: Waiting for selector "#login-button" timed out`
```

### Issue: Non-Actionable Solutions

**Wrong**:
```markdown
**Solution**: Be careful with state management.
```

**Right**:
```markdown
**Solution**: Always use Zustand for global state, React Query for server state.

Steps:
1. Create store: `apps/web/src/stores/authStore.ts`
2. Use hook: `const { user } = useAuthStore()`
3. Never store server data in Zustand - use React Query
```

### Issue: Vague Examples

**Wrong**:
```markdown
**Example**: We fixed this in the user component.
```

**Right**:
```markdown
**Example**: File: `apps/web/src/features/auth/components/LoginForm.tsx:45`

```typescript
// ❌ Wrong - Direct state mutation
setUser(existingUser)

// ✅ Right - Create new object
setUser({ ...existingUser, isAuthenticated: true })
```
```

### Issue: File Exceeds Size Limit

**Problem**: Validator reports "File exceeds 2,000 lines"

**Fix**: Use split procedure in this skill

**Quick fix**:
```bash
# 1. Check current size
wc -l docs/lessons-learned/[role]-lessons-learned-N.md

# 2. Create next part
touch docs/lessons-learned/[role]-lessons-learned-$((N+1)).md

# 3. Add header to new part (see header format in this skill)

# 4. Move recent 200-400 lines to new part

# 5. Update Part 1 header with new file count

# 6. Verify all parts under 2,000 lines
wc -l docs/lessons-learned/[role]-lessons-learned*.md
```

---

## 📊 Output Format

The validator produces structured output for programmatic use:

```json
{
  "validation": {
    "file": "docs/lessons-learned/react-developer-lessons-learned.md",
    "score": 87,
    "maxScore": 100,
    "percentage": 87,
    "status": "pass",
    "structure": {
      "score": 18,
      "maxScore": 20,
      "issues": ["Large file, consider splitting"]
    },
    "format": {
      "score": 26,
      "maxScore": 30,
      "lessons": 15,
      "problemSections": 15,
      "solutionSections": 15,
      "exampleSections": 14
    },
    "content": {
      "score": 25,
      "maxScore": 30,
      "codeBlocks": 18,
      "crossReferences": 7
    },
    "maintenance": {
      "score": 18,
      "maxScore": 20,
      "lastUpdated": "2025-11-04",
      "outdatedLessons": 0,
      "duplicates": 0,
      "fileSize": "1,456 lines (73% of max)"
    },
    "recommendations": [
      "Add more cross-references to architecture docs",
      "One lesson missing Example section"
    ]
  }
}
```

---

## 🎯 Integration with Agent Workflows

### All Agents (Startup)

**MANDATORY startup check**:
1. Check if your role has lessons files
2. Read Part 1 header to get file count
3. Read ALL parts in sequence
4. If ANY fail to read: STOP and fix
5. Only proceed when all files readable

### When Adding Lessons

1. Identify last file in series (check Part 1 header)
2. Check line count: `wc -l [last-file].md`
3. If < 1,800 lines: Write to last file
4. If > 1,800 lines: Plan split soon
5. If > 2,000 lines: Must split before writing

### Before Committing

Run validator on all your lessons files:
```bash
bash .claude/skills/lessons-learned-validator.md \
  docs/lessons-learned/[your-role]-lessons-learned*.md
```

### During Phase 5 Finalization

Validator runs automatically on ALL lessons files. Oversized files block finalization.

---

## 📁 File Organization

### Active Lessons Learned
- **Location**: `docs/lessons-learned/`
- **Purpose**: Current, relevant lessons that actively inform development decisions
- **Maintenance**: Regularly reviewed and updated

### Archived Lessons
- **Location**: `docs/archive/obsolete-lessons/`
- **Purpose**: Historical lessons preserved for reference but no longer applicable
- **Criteria for Archiving**:
  - Technology has been completely replaced
  - Process is no longer used
  - Lesson is superseded by newer approaches
  - Context is no longer relevant to current system

### Migration Process

When archiving lessons:

1. Move file to `docs/archive/obsolete-lessons/`
2. Add header indicating archive date and reason
3. Update any cross-references in active documentation
4. Add entry to archive index if it exists

---

## 📅 Review and Maintenance Schedule

### Regular Review
- **Monthly**: Review recent entries for actionability and relevance
- **Quarterly**: Assess overall structure and identify patterns
- **Annually**: Archive obsolete lessons and reorganize as needed

### Quality Standards
- Entries must be specific and actionable
- Context must be sufficient for future understanding
- Each lesson should include measurable impact when possible
- Cross-references to related documentation should be included

---

## 🔗 Integration with Development Process

### When to Document Lessons
- After resolving significant technical challenges
- Following post-mortem meetings
- When discovering better approaches to existing problems
- After completing major features or refactoring efforts
- During code reviews when patterns are identified

### Linking to Other Documentation
- Reference specific files in `/docs/functional-areas/` when applicable
- Link to relevant ADRs (Architecture Decision Records)
- Cross-reference with troubleshooting guides
- Connect to testing documentation and standards

---

## Progressive Disclosure

**Initial Context**: Show pass/fail and score only
**On Request**: Show detailed breakdown by category
**On Failure**: Show specific issues with examples of fixes
**On Pass**: Show summary with minor recommendations

---

**Remember**: This skill is the SINGLE SOURCE OF TRUTH for all lessons learned operations. Everything you need to know about lessons learned format, validation, size management, and fix procedures is in this file. Do not look elsewhere for this information.
