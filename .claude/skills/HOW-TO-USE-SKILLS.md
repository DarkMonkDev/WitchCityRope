# How to Use Skills - Complete Guide for All Agents

**Purpose**: Single source of truth for skills usage patterns and principles
**Audience**: ALL agents (planning, development, testing, quality, utility)
**Status**: Mandatory reading for all agents
**Last Updated**: 2025-11-04

---

## 🎯 THE KEY PRINCIPLE

> **"If you're documenting HOW to do something step-by-step, it should be a SKILL. If you're explaining WHAT to do or WHY it matters, reference the skill, don't duplicate it."**

This principle prevents the "single source of truth nightmare" where procedures exist in multiple places and get out of sync.

---

## What Are Skills?

**Skills = Executable automation + Complete context + Self-contained procedures**

Skills are located in `/.claude/skills/` and contain:
- ✅ Bash automation scripts (executable procedures)
- ✅ Configuration values (file size limits, ports, thresholds)
- ✅ Context and rationale (WHY this procedure exists)
- ✅ Troubleshooting guides (WHAT to do when things fail)
- ✅ Integration instructions (HOW other systems use this)

**Skills are COMPLETE** - Everything needed to execute a procedure is in ONE file.

---

## What Skills Are NOT

Skills are NOT:
- ❌ Just bash scripts (they include context and troubleshooting)
- ❌ Just documentation (they include executable automation)
- ❌ Lessons learned (skills = procedures, lessons = experience)
- ❌ Requirements docs (skills = implementation, requirements = what to build)
- ❌ Agent definitions (skills = tools agents use, agents = roles that use tools)

---

## Decision Trees: When to Create a Skill

### Create a SKILL When:

```
Is this procedure:
├─ Repetitive? YES →
├─ Automatable? YES →
├─ Has clear pass/fail? YES →
├─ Needed by multiple agents? YES →
└─ CREATE SKILL
```

**Examples of skills**:
- container-restart (restart Docker with health checks)
- phase-validators (validate quality gates)
- staging-deploy (deploy to staging environment)

### Create DOCUMENTATION When:

```
Is this content:
├─ Explanatory (WHY not HOW)? YES →
├─ Context or rationale? YES →
├─ Historical or narrative? YES →
├─ Policy definition? YES →
└─ CREATE DOC (reference skills for procedures)
```

**Examples of documentation**:
- Lessons learned (problems encountered, solutions used)
- Business requirements (WHAT to build, not HOW)
- Architectural decisions (WHY we chose this approach)

---

## How to Reference Skills (DO THIS)

### ✅ CORRECT Reference Patterns

**Pattern 1: Direct skill invocation**
```markdown
Use restart-dev-containers skill before running E2E tests.
```

**Pattern 2: Skill reference in context**
```markdown
Before testing, ensure Docker environment is healthy using restart-dev-containers skill.
```

**Pattern 3: Registry reference for discovery**
```markdown
See SKILLS-REGISTRY.md for available validation skills.
```

**Pattern 4: In startup procedures**
```markdown
**Skills Usage**: See HOW-TO-USE-SKILLS.md for complete guide
```

### ❌ WRONG Reference Patterns

**NEVER hardcode file paths:**
```markdown
❌ WRONG: See: /.claude/skills/container-restart.md
```

**NEVER duplicate procedure steps:**
```markdown
❌ WRONG: To restart containers:
1. Stop containers: docker-compose down
2. Start with dev overlay: docker-compose -f ... up -d
```

**NEVER split information:**
```markdown
❌ WRONG: File size limit is 2000 lines (see lessons-learned-validator for details)
✅ CORRECT: File size limits defined in lessons-learned-validator skill
```

---

## How to Invoke Skills

### Method 1: Using Skill Tool (Recommended)

If your agent has the `Skill` tool in its tools list:

```markdown
Use the Skill tool to invoke container-restart
```

This is the cleanest and most intuitive method.

### Method 2: Using Bash Tool (Alternative)

If your agent has the `Bash` tool:

```bash
# Extract and run the bash script from the skill
bash /.claude/skills/container-restart.md
```

Skills are markdown files with embedded bash scripts that can be executed directly.

---

## How to Avoid Duplication

### Rule 1: Skills Are Self-Contained

**Everything needed to execute a procedure goes in ONE skill file:**
- ✅ Variables and configuration
- ✅ Bash automation script
- ✅ Validation logic
- ✅ Error handling
- ✅ Troubleshooting steps

**Never split these across multiple files.**

### Rule 2: Documentation References Skills

**Documentation should:**
- ✅ Explain WHEN to use a skill
- ✅ Explain WHY the skill exists
- ✅ Reference the skill by name
- ❌ NEVER duplicate the procedure steps
- ❌ NEVER duplicate bash commands
- ❌ NEVER duplicate configuration values

### Rule 3: One Skill = One Responsibility

**Each skill should:**
- ✅ Do ONE thing well
- ✅ Have a clear purpose
- ✅ Be independently executable
- ❌ NOT combine unrelated procedures

### Rule 4: Enforcement

**Automated detection:**
- **single-source-validator** skill checks for duplication
- **phase-5-validator** BLOCKS finalization if violations found
- **Zero tolerance** for procedure duplication

**If you create duplication:**
- ❌ Phase 5 validation will FAIL
- ❌ You CANNOT commit/deploy
- ✅ Fix by removing duplication and referencing skill

---

## Skills Discovery - Three-Tier System

### Tier 1: SKILLS-REGISTRY.md (START HERE)

**Location**: `/.claude/skills/SKILLS-REGISTRY.md`

**What it contains**:
- Complete list of all skills
- When to use each skill
- Who should use each skill
- Common usage patterns

**Use this to**:
- Discover what skills exist
- Find the right skill for your task
- Understand skill categories

### Tier 2: Agent Definition Skills Sections

**Location**: `/.claude/agents/*/[agent-name].md`

**What it contains**:
- List of skills THIS agent should use
- Reference to SKILLS-REGISTRY.md for details
- NO duplication of skill procedures

**Use this to**:
- See which skills are relevant to your role
- Quick reference during work

### Tier 3: Individual Skill Files

**Location**: `/.claude/skills/[skill-name].md`

**What it contains**:
- Complete procedure
- Bash automation
- Troubleshooting
- Everything needed to execute

**Use this to**:
- Actually execute the procedure
- Understand implementation details

---

## When YOU Should Use Skills

### By Phase of Work

**Phase 1: Requirements**
- business-requirements → phase-1-validator, handoff-document-generator

**Phase 2: Design**
- functional-spec, database-designer, ui-designer → phase-2-validator, handoff-document-generator

**Phase 3: Implementation**
- react-developer, backend-developer → phase-3-validator, container-restart, handoff-document-generator

**Phase 4: Testing**
- test-developer → phase-4-validator
- test-executor → container-restart (MANDATORY), test-catalog-updater, phase-4-validator

**Phase 5: Finalization**
- librarian → phase-5-validator, master-index-updater, single-source-validator
- git-manager → phase-5-validator, staging-deploy

**All Phases**
- ALL agents → lessons-learned-validator (when updating lessons)

---

## Common Mistakes to Avoid

### Mistake 1: Duplicating Procedures in Documentation

**❌ WRONG**:
```markdown
## How to Restart Containers

1. Stop all containers:
   ```bash
   docker-compose down
   ```

2. Start with dev overlay:
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
   ```

3. Check health endpoints...
```

**✅ CORRECT**:
```markdown
## How to Restart Containers

Use restart-dev-containers skill for correct restart procedure with health checks and compilation validation.
```

### Mistake 2: Creating Skill When Documentation Suffices

**Don't create a skill for**:
- One-time procedures
- Simple explanations
- Historical context
- Rationale discussions

**Only create skills for**:
- Repetitive procedures
- Automatable tasks
- Multi-step processes needed by multiple agents

### Mistake 3: Hardcoding File Paths

**❌ WRONG**: `See: /.claude/skills/container-restart.md`
**✅ CORRECT**: `Use restart-dev-containers skill`

File paths break when files move. Skill names are stable.

### Mistake 4: Partial References

**❌ WRONG**: "Run docker-compose (see restart-dev-containers skill for flags)"
**✅ CORRECT**: "Use restart-dev-containers skill"

Don't partially duplicate and then reference. Just reference the skill completely.

---

## Why This Matters

### The Problem Without Single Source

**Before skills system**:
- Procedures documented in 10+ places
- Changes require updating multiple files
- Documentation gets out of sync
- Agents get confused which version to follow
- Human says: "I'm pissed we already have violations"

**Example of the nightmare**:
- docker-compose command existed in 171 different places
- Each variation slightly different
- Some used wrong flags
- Some skipped health checks
- Maintenance nightmare

### The Solution With Skills

**After skills system**:
- Procedure exists in ONE place (the skill)
- Change once, everywhere stays current
- Documentation just references the skill
- Agents always follow correct procedure
- Zero maintenance burden

**Example of success**:
- restart-dev-containers skill = single source
- All 171 references updated to point to skill
- Change skill once = changes everywhere
- Zero duplication

---

## Success Metrics

**Target state**:
- ✅ Zero CRITICAL violations (bash command duplications)
- ✅ Zero HIGH violations (procedural descriptions)
- ⚠️ < 50 MEDIUM warnings (configuration references)
- ℹ️ LOW warnings acceptable (generic business language)

**Enforcement**:
- Phase 5 validation BLOCKS on CRITICAL/HIGH violations
- Warnings reported but don't block
- Monthly audits catch drift

---

## Integration with Your Workflow

### At Session Start
1. Read this guide (HOW-TO-USE-SKILLS.md)
2. Check SKILLS-REGISTRY.md for available skills
3. Note which skills are relevant to your role

### During Work
1. **Before documenting a procedure**: Check if skill exists
2. **If skill exists**: Reference it, don't duplicate
3. **If no skill**: Ask "Should this be a skill?" (see decision tree)
4. **If creating skill**: Update SKILLS-REGISTRY.md

### At Phase Transitions
1. Use handoff-document-generator skill
2. Use appropriate phase validator
3. Check for duplication with single-source-validator

### At Session End
1. Use lessons-learned-validator if you updated lessons
2. Phase-5-validator will check for skill duplications
3. Fix any violations before committing

---

## Quick Reference Card

**Skills are**:
- ✅ Executable automation
- ✅ Self-contained procedures
- ✅ Single source of truth

**Skills are NOT**:
- ❌ Just bash scripts
- ❌ Lessons learned
- ❌ Requirements docs

**Reference skills by**:
- ✅ Skill name: "Use restart-dev-containers skill"
- ❌ File path: "See /.claude/skills/..."

**Invoke skills with**:
- ✅ Skill tool: `Skill(command: "container-restart")`
- ✅ Bash tool: `bash /.claude/skills/container-restart.md`

**Discovery**:
1. SKILLS-REGISTRY.md (what exists)
2. Agent definition (what I should use)
3. Skill file (how to use it)

**Enforcement**:
- single-source-validator detects duplications
- phase-5-validator BLOCKS violations
- Zero tolerance policy

---

## Questions and Support

**Can't find the right skill?**
→ Check SKILLS-REGISTRY.md for complete catalog

**Should I create a new skill?**
→ Use decision tree in this guide

**Found a duplication?**
→ Remove it and reference the skill instead

**Skill doesn't work?**
→ Check skill's troubleshooting section

**Need to change a procedure?**
→ Change the skill file (one place), not documentation

---

**Remember**: Skills exist so you DON'T have to remember complex procedures. When in doubt, check SKILLS-REGISTRY.md and reference the skill!
