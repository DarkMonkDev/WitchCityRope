# WitchCityRope Skills System

**Created**: 2025-11-04
**Purpose**: Complementary automation Skills for the 16-agent system
**Status**: Complete (10/10 Skills)

## Overview

This Skills system provides **automation** capabilities that complement the **prevention-focused** lessons learned system. Skills automate repetitive tasks, while lessons learned prevent mistakes.

**Key Distinction**:
- **Lessons Learned** = "What went wrong, why debugging was hard, how to avoid it"
- **Skills** = "How to automate a task, what to check, how to validate"

## The 10 Skills

### Phase Validators (5 Skills)

Automate quality gate validation at phase transitions:

1. **phase-1-validator.md** - Requirements Phase (95% for Features)
   - Validates business requirements completeness
   - Checks user stories, acceptance criteria, security requirements
   - Enforces WitchCityRope-specific considerations

2. **phase-2-validator.md** - Design Phase (90% for Features)
   - Validates functional specs, database design, UI/UX design
   - Checks Web+API architecture compliance
   - Enforces PostgreSQL syntax, Mantine v7 components

3. **phase-3-validator.md** - Implementation Phase (85% for Features)
   - Validates code compilation, test coverage, quality
   - Checks architectural violations
   - Enforces first vertical slice checkpoint

4. **phase-4-validator.md** - Testing Phase (100% for ALL work types)
   - **ZERO TOLERANCE**: 100% test pass rate required
   - Validates environment health
   - Checks coverage targets, test quality

5. **phase-5-validator.md** - Finalization Phase (80% for Features)
   - Validates git commits, documentation, cleanup
   - Checks file registry updates, lessons learned contributions
   - Enforces deployment readiness

### Workflow Automation (5 Skills)

Automate documentation and tracking tasks:

6. **handoff-document-generator.md**
   - Generates standardized agent-to-agent handoff documents
   - Prevents 90%+ implementation failures from missing context
   - Enforces mandatory handoffs at phase transitions

7. **lessons-learned-validator.md**
   - Validates lessons follow Problem → Solution → Example format
   - Checks prevention-focused language
   - Maintains 14,814+ line lessons learned quality

8. **test-catalog-updater.md**
   - Updates TEST_CATALOG after every test execution
   - Records metrics (pass/fail, coverage, execution time)
   - Maintains single source of truth for test health

9. **quality-gate-calculator.md**
   - Calculates context-appropriate quality gates by work type
   - Feature (85-95%), Bug (70-80%), Hotfix (60-70%), Refactor (85-90%)
   - **ALL work types require 100% test pass in Phase 4**

10. **master-index-updater.md**
    - Updates Functional Area Master Index when features added
    - Maintains navigation for `/docs/functional-areas/`
    - Organizes by domain (Core/Events/Content/Operations)

## How They Work Together

### Workflow Orchestration Integration

```
Start Workflow
     |
     v
quality-gate-calculator → Set thresholds for work type
     |
     v
Phase 1: Requirements
     |
     v
phase-1-validator → Check requirements quality
     |
     v
handoff-document-generator → Create Requirements → Design handoff
     |
     v
Phase 2: Design
     |
     v
phase-2-validator → Check design quality
     |
     v
handoff-document-generator → Create Design → Implementation handoff
     |
     v
Phase 3: Implementation
     |
     v
phase-3-validator → Check code quality
     |
     v
handoff-document-generator → Create Implementation → Testing handoff
     |
     v
Phase 4: Testing
     |
     v
test-catalog-updater → Record test metrics
     |
     v
phase-4-validator → Enforce 100% test pass
     |
     v
Phase 5: Finalization
     |
     v
lessons-learned-validator → Check lesson quality
     |
     v
master-index-updater → Update navigation
     |
     v
phase-5-validator → Check finalization complete
     |
     v
Workflow Complete
```

## Usage Patterns

### From Orchestrator
Skills are automatically invoked during workflow:

```
Use the phase-1-validator skill to check if requirements are ready
Use the handoff-document-generator skill to create handoff document
Use the quality-gate-calculator skill for bug fix work
```

### From Agents
Agents can self-service invoke Skills:

```
Before completing work, I'll use the lessons-learned-validator skill
After running tests, I'll use the test-catalog-updater skill
```

### Manual Invocation
Skills can be run directly:

```bash
# Validate requirements
bash .claude/skills/phase-1-validator.md docs/functional-areas/.../business-requirements.md

# Generate handoff
bash .claude/skills/handoff-document-generator.md business-requirements functional-spec user-management

# Update test catalog
bash .claude/skills/test-catalog-updater.md unit 45 0 45 12.3 85
```

## Key Innovations

### 1. Complementary to Lessons Learned
**Problem**: Initially considered replacing lessons with Skills
**Solution**: Keep both - different purposes
- Lessons = Prevention (historical context)
- Skills = Automation (current tasks)

### 2. Context-Appropriate Rigor
**Problem**: One-size-fits-all quality gates too rigid
**Solution**: Quality gates by work type
- Features: Highest rigor (85-95%)
- Bugs: Moderate rigor (70-80%)
- Hotfixes: Minimal rigor (60-70%)
- **Testing: 100% for ALL** (non-negotiable)

### 3. Progressive Disclosure
**Problem**: Overwhelming information in skills
**Solution**: Progressive disclosure pattern
- Initial: Show summary/pass-fail
- On request: Show full details
- On failure: Show specific fixes needed

### 4. Handoff System
**Problem**: 90%+ implementation failures from missing context
**Solution**: Mandatory handoff documents
- Template with TODO sections
- Source agent completes
- Target agent reads before starting

### 5. Zero Tolerance Testing
**Problem**: Flaky tests accepted, bugs slip to production
**Solution**: 100% test pass rate required
- No exceptions for any work type
- One failing test = Phase 4 fails
- Protects production quality

## File Structure

```
/.claude/skills/
├── README.md                                  # This file
├── phase-1-validator.md                      # Requirements validation
├── phase-2-validator.md                      # Design validation
├── phase-3-validator.md                      # Implementation validation
├── phase-4-validator.md                      # Testing validation (100% required)
├── phase-5-validator.md                      # Finalization validation
├── handoff-document-generator.md             # Agent handoff automation
├── lessons-learned-validator.md              # Lesson quality checking
├── test-catalog-updater.md                   # TEST_CATALOG maintenance
├── quality-gate-calculator.md                # Context-appropriate gates
└── master-index-updater.md                   # Navigation maintenance
```

## Skill Characteristics

### Auto-Invoked
Claude Code decides when to invoke Skills based on context:
- "check if requirements are ready" → phase-1-validator
- "after running tests" → test-catalog-updater
- "create handoff" → handoff-document-generator

### Progressive Disclosure
Skills reveal information based on need:
- Quick check: Pass/fail only
- Full validation: Detailed scoring
- Failure: Specific issues and fixes

### Automation-Focused
Skills automate tasks, not provide general knowledge:
- ✅ "How to validate requirements completeness"
- ✅ "How to update TEST_CATALOG with metrics"
- ❌ "Why tests are important" (that's lessons learned)

### Executable
Skills often include bash scripts:
- Can be run manually for testing
- Provide consistent validation
- Generate structured output (JSON)

## Integration Points

### With Agents
- **business-requirements** uses phase-1-validator
- **functional-spec** uses phase-2-validator
- **react-developer** uses phase-3-validator
- **test-executor** uses test-catalog-updater + phase-4-validator
- **librarian** uses master-index-updater

### With Lessons Learned
- Skills automate what lessons prevent
- Lessons document why automation is needed
- Skills reference relevant lessons

### With Orchestrator
- Orchestrator invokes phase validators
- Quality gate calculator sets thresholds
- Handoff generator enforces context transfer

### With Quality Gates
- Quality gate calculator computes thresholds
- Phase validators enforce thresholds
- Work type determines rigor level

## Maintenance

### Adding New Skills
1. Create skill in `.claude/skills/`
2. Use YAML frontmatter:
   ```yaml
   ---
   name: skill-name
   description: What it does
   ---
   ```
3. Follow progressive disclosure pattern
4. Include automation scripts if applicable
5. Document in this README

### Updating Existing Skills
- Skills should evolve based on usage
- Add common patterns discovered
- Refine validation criteria
- Update thresholds if needed

### Deprecating Skills
- Mark as deprecated in frontmatter
- Document replacement skill
- Keep for historical reference

## Success Metrics

### Quality Improvement
- Reduction in implementation failures
- Fewer phase rework cycles
- Higher first-time pass rate

### Efficiency Gains
- Time saved on manual validation
- Fewer context-loss issues
- Faster phase transitions

### Consistency
- Standardized quality checks
- Predictable validation criteria
- Reliable handoff format

## Future Enhancements

Potential additional Skills:

1. **deployment-validator** - Check deployment readiness
2. **performance-analyzer** - Validate performance metrics
3. **security-scanner** - Automated security checks
4. **dependency-updater** - Manage package updates
5. **documentation-generator** - Auto-generate API docs

## Conclusion

The 10 Skills system provides essential automation that:
- Complements the prevention-focused lessons learned
- Enforces quality gates appropriate to work type
- Automates repetitive validation tasks
- Maintains critical documentation systems
- Preserves context through mandatory handoffs

Together with 16 agents and 14,814+ lines of lessons learned, these Skills complete the WitchCityRope development system.

---

**Created by**: research and planning agents
**Date**: 2025-11-04
**Version**: 1.0.0
