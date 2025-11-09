# Skills Registry - Central Discovery Mechanism

**Purpose**: Single source of truth for ALL available Skills and when to use them
**Audience**: Agents, Orchestrator, Human Developers
**Status**: 17 Skills Active (as of 2025-11-08)
**Structure**: Each skill is now in `skill-name/SKILL.md` format (Claude Code compatible)

---

## 🔍 Quick Skill Lookup

**Need to know what Skills exist?** Start here.
**Need to know when to use a Skill?** Check "When to Use" column.
**Need to know if YOU should use a Skill?** Check "Primary Users" column.

**📖 For complete skills usage guide**: See `/.claude/skills/HOW-TO-USE-SKILLS.md`
- Decision trees (skill vs documentation)
- How to invoke skills (Skill tool vs Bash)
- Reference patterns (correct/wrong examples)
- Common mistakes to avoid
- Integration with workflow

---

## All Skills by Category

### 📊 Phase Validators (5 Skills)

Automated quality gate validation at each workflow phase.

| Skill | When to Use | Primary Users | Quality Threshold |
|-------|-------------|---------------|-------------------|
| **phase-1-validator** | After business requirements complete, before design starts | business-requirements, orchestrator | 95% (Features), 80% (Docs) |
| **phase-2-validator** | After design complete (functional spec + database + UI), before implementation | functional-spec, database-designer, ui-designer, orchestrator | 90% (Features), 80% (Bugs) |
| **phase-3-validator** | After first vertical slice implementation, before full rollout | react-developer, backend-developer, orchestrator | 85% (Features), 70% (Bugs) |
| **phase-4-validator** | After all tests written and run, before finalization | test-developer, test-executor, orchestrator | **100% (ALL work types)** |
| **phase-5-validator** | Before commit/PR creation, deployment readiness check | git-manager, librarian, orchestrator | 80% (Features), 70% (Hotfixes) |

**Integration**: Orchestrator invokes automatically at phase transitions. BLOCKS workflow if thresholds not met.

---

### 🔄 Workflow Automation (5 Skills)

Automate documentation, tracking, and handoff tasks.

| Skill | When to Use | Primary Users | Output |
|-------|-------------|---------------|--------|
| **handoff-document-generator** | At EVERY phase transition (Requirements→Design, Design→Implementation, etc.) | ALL agents, orchestrator | Handoff document in `/docs/functional-areas/[feature]/handoffs/` |
| **lessons-learned-validator** | Before committing lesson updates, during Phase 5 finalization (MANDATORY - size check) | ALL agents (when updating lessons), librarian | Validation report + score + size alerts (1700 line max) |
| **test-catalog-updater** | After EVERY test execution (unit, integration, E2E) | test-executor | Updated TEST_CATALOG.md |
| **quality-gate-calculator** | At workflow START to determine thresholds for work type | orchestrator | Quality thresholds exported to /tmp/quality-gate.env |
| **master-index-updater** | When feature added/updated/deprecated in functional areas | librarian | Updated functional-area-master-index.md |

**Integration**: Agents self-invoke during their work. Orchestrator enforces handoff generation.

---

### 🐳 Infrastructure Automation (2 Skills)

Automate development environment and deployment tasks.

| Skill | When to Use | Primary Users | Purpose |
|-------|-------------|---------------|---------|
| **container-restart** | BEFORE E2E tests (MANDATORY), after code changes, when containers unhealthy | test-executor, react-developer, backend-developer, orchestrator | Restart Docker dev containers correctly with compilation checks |
| **staging-deploy** | After Phase 5 validation passes, when deploying features for testing | git-manager, orchestrator | Deploy to DigitalOcean staging environment |

**Integration**: test-executor MUST use container-restart before E2E tests. Orchestrator may auto-deploy after Phase 5.

---

### ✨ Code Quality Automation (3 Skills)

Automate code quality checks, formatting, and fixes.

| Skill | When to Use | Primary Users | Performance |
|-------|-------------|---------------|-------------|
| **code-quality-check** | Before commit (via pre-commit hook), before PR creation, after feature complete | react-developer, backend-developer, lint-validator, pre-commit hook | ~5s full, ~2s changed files |
| **code-format** | Before commit, after development session, resolving merge conflicts | react-developer, backend-developer, prettier-formatter, pre-commit hook | ~3s full, <1s changed files |
| **code-quality-fix** | After failing quality check, before committing, batch cleanup | react-developer, backend-developer, code-reviewer | ~5s full, ~2s changed files |

**Integration**:
- Pre-commit hook runs code-quality-check automatically (BLOCKS on errors)
- Phase-3-validator may invoke for quality gates
- Developers use code-quality-fix for auto-fixing issues

**Options**:
- `--quick`: Skip TypeScript & security (faster)
- `--changed-only`: Check only modified files
- `--check`: Check formatting without changes
- `--fix`: Auto-fix issues
- `--dry-run`: Preview changes without applying

---

### 🚨 Enforcement (1 Skill)

Detect and prevent violations of architecture rules.

| Skill | When to Use | Primary Users | Authority |
|-------|-------------|---------------|-----------|
| **single-source-validator** | After creating new Skill, during Phase 5 finalization (MANDATORY), monthly audits | ALL agents, orchestrator, librarian | **BLOCKING** - Detects duplication of Skills in agent definitions/lessons/process docs |

**Integration**: Phase-5-validator invokes for ALL skills. BLOCKS finalization if duplicates found.

---

## 🎯 When Should I Use a Skill?

### By Agent Role

#### business-requirements Agent
- ✅ **phase-1-validator** - Validate requirements before handoff
- ✅ **handoff-document-generator** - Create Requirements→Design handoff
- ✅ **lessons-learned-validator** - Validate lesson updates

#### functional-spec Agent
- ✅ **phase-2-validator** - Validate functional spec before handoff
- ✅ **handoff-document-generator** - Create Design→Implementation handoff
- ✅ **lessons-learned-validator** - Validate lesson updates

#### database-designer Agent
- ✅ **phase-2-validator** - Validate database design
- ✅ **lessons-learned-validator** - Validate lesson updates

#### ui-designer Agent
- ✅ **phase-2-validator** - Validate UI/UX design
- ✅ **lessons-learned-validator** - Validate lesson updates

#### react-developer Agent
- ✅ **phase-3-validator** - Validate implementation
- ✅ **container-restart** - Restart containers after code changes
- ✅ **handoff-document-generator** - Create Implementation→Testing handoff
- ✅ **lessons-learned-validator** - Validate lesson updates

#### backend-developer Agent
- ✅ **phase-3-validator** - Validate implementation
- ✅ **container-restart** - Restart containers after code changes
- ✅ **handoff-document-generator** - Create Implementation→Testing handoff
- ✅ **lessons-learned-validator** - Validate lesson updates

#### test-developer Agent
- ✅ **phase-4-validator** - Validate test suite completeness
- ✅ **lessons-learned-validator** - Validate lesson updates

#### test-executor Agent
- ✅ **container-restart** - **MANDATORY** before E2E tests
- ✅ **test-catalog-updater** - After EVERY test execution
- ✅ **phase-4-validator** - Validate all tests passing
- ✅ **lessons-learned-validator** - Validate lesson updates

#### librarian Agent
- ✅ **master-index-updater** - Update navigation when features change
- ✅ **phase-5-validator** - Validate finalization complete
- ✅ **lessons-learned-validator** - Validate lesson format/quality
- ✅ **single-source-validator** - Check for duplication during audits

#### git-manager Agent
- ✅ **phase-5-validator** - Validate before commits
- ✅ **staging-deploy** - Deploy to staging after Phase 5
- ✅ **lessons-learned-validator** - Validate lesson updates

#### code-reviewer Agent
- ✅ **phase-3-validator** - Validate implementation quality
- ✅ **phase-5-validator** - Validate finalization
- ✅ **single-source-validator** - Check for duplication in reviews

#### Orchestrator (Main Agent)
- ✅ **quality-gate-calculator** - Set thresholds at workflow start
- ✅ **phase-1-validator** - Enforce Requirements phase gate
- ✅ **phase-2-validator** - Enforce Design phase gate
- ✅ **phase-3-validator** - Enforce Implementation phase gate
- ✅ **phase-4-validator** - Enforce Testing phase gate (100% pass)
- ✅ **phase-5-validator** - Enforce Finalization phase gate
- ✅ **handoff-document-generator** - Enforce handoffs at transitions
- ✅ **single-source-validator** - Enforce single source of truth
- ✅ **container-restart** - Ensure environment healthy before tests
- ✅ **staging-deploy** - Deploy after successful Phase 5

---

## 📋 Common Usage Patterns

### Pattern 1: Starting a New Feature

```
Orchestrator:
1. Use quality-gate-calculator to set thresholds (Feature type)
2. Delegate to business-requirements agent

business-requirements:
3. Create business requirements document
4. Use phase-1-validator to check quality (target: 95%)
5. Use handoff-document-generator (Requirements→Design)

Orchestrator:
6. Review handoff, delegate to functional-spec/database-designer/ui-designer
```

### Pattern 2: Before Running E2E Tests

```
test-executor:
1. Use container-restart skill (MANDATORY)
   - Restarts containers with dev overlay
   - Checks compilation errors
   - Verifies health endpoints
2. Run E2E tests (npm run test:e2e)
3. Use test-catalog-updater to record metrics
```

### Pattern 3: Deploying to Staging

```
Orchestrator:
1. Ensure Phase 5 validation passed
2. Use staging-deploy skill
   - Builds production images
   - Pushes to DigitalOcean registry
   - Deploys to server
   - Runs health checks
3. Manual testing on https://staging.notfai.com
```

### Pattern 4: Phase Transition Handoff

```
Source Agent (e.g., react-developer):
1. Complete implementation work
2. Use phase-3-validator to check quality
3. Use handoff-document-generator (Implementation→Testing)
   - Completed work summary
   - Critical decisions
   - Context for test-developer

Orchestrator:
4. Verify handoff document exists
5. Delegate to target agent (test-developer)

Target Agent (test-developer):
6. READ handoff document first
7. Begin test creation with full context
```

### Pattern 5: Preventing Duplication

```
Agent (any):
1. Before documenting a procedure in lessons/agent def/process doc
2. Check if Skill exists: ls /.claude/skills/ | grep [keyword]
3. If Skill exists, REFERENCE it, don't duplicate
4. If no Skill exists and procedure is complex, CREATE SKILL FIRST

Orchestrator:
5. During Phase 5, use single-source-validator for ALL skills
6. If duplicates found, BLOCK finalization
7. Agent fixes violations before completing workflow
```

---

## 🔧 How to Use Skills

### From Claude Code (Auto-Invoked)

Skills are automatically invoked based on context:

```
Claude: "I'll use the phase-1-validator skill to check if requirements are complete"
Claude: "Before running E2E tests, I'll use the container-restart skill"
Claude: "I'll use the handoff-document-generator skill to create the handoff"
```

**No explicit command needed** - Claude Code decides when to invoke.

### Manual Invocation (Bash)

For testing or direct execution:

```bash
# Phase validation
bash .claude/skills/phase-1-validator.md /docs/functional-areas/.../business-requirements.md

# Container restart
bash .claude/skills/container-restart.md

# Staging deployment
bash .claude/skills/staging-deploy.md

# Generate handoff
bash .claude/skills/handoff-document-generator.md business-requirements functional-spec user-management

# Update test catalog
bash .claude/skills/test-catalog-updater.md unit 45 0 45 12.3 85

# Validate single source of truth
bash .claude/skills/single-source-validator.md container-restart CRITICAL

# Calculate quality gates
bash .claude/skills/quality-gate-calculator.md Feature

# Update master index
bash .claude/skills/master-index-updater.md add "User Management" user-management
```

---

## 🚫 What Skills Are NOT

### Skills ≠ Lessons Learned

| Skills | Lessons Learned |
|--------|-----------------|
| **Automation** - How to validate, how to check, how to update | **Prevention** - What went wrong, why debugging was hard, how to avoid |
| **Current tasks** - Execute now, report status | **Historical context** - Happened before, prevented future issues |
| **Executable scripts** - Bash commands, validation logic | **Narrative explanations** - Problem → Solution → Example format |
| **Pass/fail outputs** - Quality scores, validation reports | **Learning documentation** - Why things matter, tradeoffs made |
| **Tool-focused** - Automate repetitive validation | **Human-focused** - Share knowledge, build understanding |

**Both are essential** - Skills automate, Lessons prevent. Don't replace one with the other.

---

## 🎯 KEY PRINCIPLE: Skills vs. Documentation

> **"If you're documenting HOW to do something step-by-step, it should be a SKILL. If you're explaining WHAT to do or WHY it matters, reference the skill, don't duplicate it."**

### Decision Tree: Skill vs. Documentation

**When to Create a SKILL:**
```
Is this procedure:
├─ Repetitive? YES →
├─ Automatable? YES →
├─ Has clear pass/fail? YES →
├─ Needed by multiple agents? YES →
└─ CREATE SKILL
```

**When to Create DOCUMENTATION:**
```
Is this content:
├─ Explanatory (WHY not HOW)? YES →
├─ Context or rationale? YES →
├─ Historical or narrative? YES →
├─ Policy definition? YES →
└─ CREATE DOC (reference skills for procedures)
```

---

## 📖 How to Reference Skills (DO THIS)

### ✅ CORRECT Reference Patterns

**Pattern 1: Direct skill invocation**
```markdown
Use container-restart skill before running E2E tests.
```

**Pattern 2: Skill reference in context**
```markdown
Before testing, ensure Docker environment is healthy using container-restart skill.
```

**Pattern 3: Registry reference for discovery**
```markdown
See SKILLS-REGISTRY.md for available validation skills.
```

**Pattern 4: Procedure delegation**
```markdown
For container restart procedure, use container-restart skill.
```

**Pattern 5: Lessons learned format**
```markdown
## Problem: E2E Tests Fail with "Element Not Found"

**Root Cause**: Container shows "Up" but code has compilation errors inside.

**Solution**: Use container-restart skill before E2E tests.

The skill automatically checks for compilation errors and verifies health.
```

### ❌ WRONG Reference Patterns

**NEVER hardcode file paths:**
```markdown
❌ WRONG: See: /.claude/skills/container-restart.md
❌ WRONG: Run /.claude/skills/container-restart.md
```

**NEVER duplicate procedure steps:**
```markdown
❌ WRONG: To restart containers:
1. Stop containers: docker-compose down
2. Start with dev overlay: docker-compose -f ... up -d
[... duplicates skill content]
```

**NEVER describe procedure without referencing skill:**
```markdown
❌ WRONG: The correct container restart process is important.
Make sure to stop containers first, then start with the dev overlay...
[... describes what skill does without referencing it]
```

---

## 🚫 How to Avoid Duplication

### Rule 1: Skills Are Self-Contained
**Everything needed to execute a procedure goes in ONE skill file:**
- ✅ Variables and configuration
- ✅ Bash automation script
- ✅ Validation logic
- ✅ Error handling
- ✅ Troubleshooting steps

**Other documents INVOKE the skill, never duplicate it.**

### Rule 2: Use Reference Pattern
**When writing documentation about a procedure:**
1. ✅ **DO**: Say "Use [skill-name] skill"
2. ✅ **DO**: Explain WHEN to use the skill
3. ✅ **DO**: Explain WHY the skill is needed
4. ❌ **DON'T**: Copy procedure steps from skill
5. ❌ **DON'T**: Create manual procedure as alternative

### Rule 3: Check Before Writing
**Before documenting a procedure:**
1. Search: `ls /.claude/skills/ | grep [keyword]`
2. If skill exists: REFERENCE it, don't duplicate
3. If no skill exists: CREATE SKILL FIRST, then reference it

### Rule 4: Enforcement
**Automated detection:**
- **single-source-validator** skill checks for duplication
- **phase-5-validator** BLOCKS finalization if violations found
- **Zero tolerance** for procedure duplication

**See**: `/.claude/skills/single-source-validator.md` for enforcement details

---

## 🔗 Integration Points

### With Phase Validators

Phase validators call other skills:

- **phase-3-validator** may invoke **container-restart** before checking compilation
- **phase-4-validator** requires **test-catalog-updater** metrics
- **phase-5-validator** MUST invoke **single-source-validator** for all skills

### With Orchestrator

Orchestrator coordinates skill invocation:

- Sets workflow type → **quality-gate-calculator** → Get thresholds
- Phase transition → **handoff-document-generator** → Create handoff
- Before tests → **container-restart** → Ensure environment healthy
- After Phase 5 → **staging-deploy** → Deploy to staging

### With Lessons Learned

Skills and lessons complement each other:

- Lesson documents problem → Solution references Skill
- Skill automates solution → Prevents problem recurrence
- Example: "Container restart lesson" → References **container-restart** skill

---

## 📊 Success Metrics

### Discovery Effectiveness

- **Agents finding relevant skills**: Track how often agents use appropriate skills
- **Skill invocation rate**: Skills used vs. manual procedures performed
- **Target**: >90% of applicable tasks use skills instead of manual procedures

### Quality Improvement

- **Phase pass rate**: First-time pass rate for phase validators
- **Rework cycles**: Reduction in phase failures requiring rework
- **Target**: >80% first-time pass rate across all phases

### Enforcement Success

- **Duplication violations**: Single-source-validator findings
- **Target**: Zero violations in Phase 5 validation (100% compliance)

---

## 🆕 Adding New Skills

### When to Create a New Skill

✅ **DO create a skill when:**
- Procedure is repetitive and automatable
- Multiple agents need the same validation
- Process has clear pass/fail criteria
- Automation prevents common mistakes

❌ **DON'T create a skill when:**
- Procedure is one-time or rarely used
- Context/explanation is more important than automation
- No clear validation criteria exist
- Better suited for lessons learned or process doc

### How to Create a New Skill

1. **Create skill file**: `/.claude/skills/my-new-skill.md`
2. **Add YAML frontmatter**:
   ```yaml
   ---
   name: my-new-skill
   description: What it automates and when to use
   ---
   ```
3. **Follow structure**:
   - Purpose statement
   - When to use
   - Automation script (bash if applicable)
   - Integration points
   - Output format (JSON for automation)
4. **Register in this file**: Add to appropriate category
5. **Validate**: Use **single-source-validator** to ensure no duplication
6. **Update agent definitions**: Add to relevant agents' skill lists

---

## 🔄 Three-Tier Discovery System

This registry is **Tier 1** of the discovery system:

### Tier 1: SKILLS-REGISTRY.md (This File)
**Purpose**: Central lookup - ALL skills, when to use, who uses them
**Audience**: Any agent needing to discover skills
**Updated**: When skills added/removed/changed

### Tier 2: Agent Definitions (/.claude/agents/*.md)
**Purpose**: Role-specific skills - skills relevant to THIS agent
**Audience**: Individual agent during work
**Updated**: When agent's responsibilities change

### Tier 3: Lessons Learned (/docs/lessons-learned/*.md)
**Purpose**: Solution-specific skills - skill references in problem solutions
**Audience**: Agent researching how to solve a problem
**Updated**: When lessons created/updated

**Example Flow**:
1. Agent has task → Check Tier 2 (agent definition) for relevant skills
2. Agent encounters problem → Check Tier 3 (lessons learned) for solution that references skill
3. Agent needs full skill list → Check Tier 1 (this registry)

---

## 📝 Maintenance

### Weekly
- [ ] Verify all skills executable and passing
- [ ] Check for new duplication violations (single-source-validator)

### Monthly
- [ ] Review skill usage metrics
- [ ] Update skill descriptions based on feedback
- [ ] Audit agent definitions for correct skill references

### Quarterly
- [ ] Assess need for new skills
- [ ] Deprecate unused skills
- [ ] Update quality thresholds based on data

---

## 📚 Additional Documentation

- **Skills System Overview**: `/.claude/skills/README.md`
- **Skills Architecture Plan**: `/docs/functional-areas/ai-workflow-orchstration/new-work/2025-11-04-plugins-marketplace-research/SKILLS-ARCHITECTURE-PLAN.md`
- **Plugin System**: `/MARKETPLACE-README.md`
- **Individual Skill Details**: `/.claude/skills/[skill-name].md`

---

## Version History

- **2025-11-04**: Created registry with 13 skills (10 original + 3 new)
  - Added: container-restart, staging-deploy, single-source-validator
  - Established three-tier discovery system
  - Integrated enforcement mechanism

---

**Remember**: This registry is the SINGLE SOURCE OF TRUTH for skill discovery. Update when skills change.
