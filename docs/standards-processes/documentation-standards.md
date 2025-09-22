# Documentation Standards

This document defines the standards and conventions for documentation within the WitchCityRope project.

## Lessons Learned Standards

### File Naming Convention

Lessons learned files follow a strict naming convention to ensure consistency and discoverability:

- **Pattern**: `[domain]-lessons-learned.md`
- **Location**: `docs/lessons-learned/[domain]-lessons-learned.md`

### Supported Domains

The following domains are recognized for lessons learned documentation:

- `backend-lessons-learned.md` - Backend development, API design, and server-side logic
- `frontend-lessons-learned.md` - Frontend development, UI components, and client-side functionality
- `testing-lessons-learned.md` - Testing strategies, test automation, and quality assurance
- `database-lessons-learned.md` - Database design, migrations, and data management
- `devops-lessons-learned.md` - Deployment, infrastructure, and operational concerns
- `security-lessons-learned.md` - Security patterns, authentication, and authorization
- `performance-lessons-learned.md` - Performance optimization and monitoring
- `integration-lessons-learned.md` - Third-party integrations and API connections
- `ui-ux-lessons-learned.md` - User interface and user experience design
- `architecture-lessons-learned.md` - System architecture and design decisions

### Standard Format for Lessons Learned Entries

Each lessons learned file should follow this structure:

```markdown
# [Domain] Lessons Learned

## Overview
Brief description of the domain and purpose of this document.

## Lessons Learned

### [Lesson Title] - [Date]

**Context**: Brief description of the situation or problem.

**What We Learned**: Key insights and discoveries.

**Action Items**: 
- Specific actions to take in future similar situations
- Process improvements
- Documentation updates needed

**Impact**: How this lesson affects future work.

**Tags**: #keyword1 #keyword2 #keyword3

---

### [Next Lesson Title] - [Date]
[Continue with same format...]
```

### Entry Guidelines

1. **Date Format**: Use ISO format (YYYY-MM-DD) for consistency
2. **Context**: Provide enough background for future readers to understand the situation
3. **Actionable**: Each lesson should include specific, actionable takeaways
4. **Tagged**: Use consistent tags to enable cross-referencing and searching
5. **Impact**: Clearly state how this lesson changes future approach

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

## File Organization

### Active Lessons Learned
- **Location**: `docs/lessons-learned/`
- **Purpose**: Current, relevant lessons that actively inform development decisions
- **Maintenance**: Regularly reviewed and updated

### Archived Lessons
- **Location**: `docs/archive/obsolete-lessons/`
- **Purpose**: Historical lessons that are no longer applicable but preserved for reference
- **Criteria for Archiving**:
  - Technology has been completely replaced
  - Process is no longer used
  - Lesson is superseded by newer approaches
  - Context is no longer relevant to current system

### Migration Process

When archiving lessons learned:

1. Move the file to `docs/archive/obsolete-lessons/`
2. Add a header indicating archive date and reason
3. Update any cross-references in active documentation
4. Add entry to archive index if it exists

## Template Usage

A template file is provided at `docs/lessons-learned/TEMPLATE-lessons-learned.md` to ensure consistency when creating new domain-specific lessons learned files.

## Review and Maintenance

### Regular Review Schedule
- **Monthly**: Review recent entries for actionability and relevance
- **Quarterly**: Assess overall structure and identify patterns
- **Annually**: Archive obsolete lessons and reorganize as needed

### Quality Standards
- Entries must be specific and actionable
- Context must be sufficient for future understanding
- Each lesson should include measurable impact when possible
- Cross-references to related documentation should be included

## Integration with Development Process

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

## Compliance and Enforcement

This standard is enforced through:
- Code review processes
- Documentation audits
- Template usage requirements
- Regular team review sessions

## Multi-File Lessons Learned Management (MANDATORY)

### File Size Limits
- **Maximum file size**: ~2,000 lines (conservative limit for 25,000 token Read limit)
- **Check before writing**: Use `wc -l filename` to check line count
- **STOP if can't read**: DO NOT continue work if lessons learned files fail to load

### Multi-File Structure
When lessons learned files exceed size limits, they MUST be split:
- Part 1: `[agent]-lessons-learned.md` (original file)
- Part 2: `[agent]-lessons-learned-2.md`
- Part 3: `[agent]-lessons-learned-3.md`
- Continue sequential numbering as needed

### Reading Protocol (MANDATORY)
1. Read Part 1 header to get file count
2. Read ALL files in sequence
3. **IF ANY FILE FAILS TO READ**: STOP IMMEDIATELY and:
   - Create the missing/broken file if it doesn't exist
   - Split the file if it's too large
   - Update Part 1 header
   - THEN re-read all files before continuing

### Writing Protocol (MANDATORY)
1. ALWAYS write to the LAST file in the series
2. Before writing, check: `wc -l [last-file].md`
3. If > 2,000 lines:
   - Create next numbered file
   - Update Part 1 header with new count
   - Write to the NEW file

### Part 1 Header Format (REQUIRED)
```
## 📚 MULTI-FILE LESSONS LEARNED
**Files**: 3 total
**Read ALL**: Part 1, Part 2, Part 3
**Write to**: Part 3 ONLY
**Max size**: 2,000 lines per file (NOT 2,500)
**IF READ FAILS**: STOP and fix per documentation-standards.md
```

### HARD BLOCK ENFORCEMENT (CRITICAL)

**STARTUP VALIDATION GATE:**
1. Set flag: `LESSONS_LEARNED_READABLE=false`
2. Attempt to read ALL lessons learned files
3. If ANY file fails with token limit error:
   - STOP IMMEDIATELY
   - DO NOT PROCEED WITH ANY WORK
   - FIX THE PROBLEM using the procedure below
4. Only when ALL files read successfully: `LESSONS_LEARNED_READABLE=true`
5. ONLY proceed with work if flag is true

**FIX PROCEDURE When File Too Large:**
1. Check current line count: `wc -l [filename]`
2. If Part 1 > 2,000 lines:
   - Move content from line 2,001 onward to Part 2
   - Update Part 1 header with new file count
3. If Part 2 > 2,000 lines:
   - Create Part 3
   - Move excess content
   - Update Part 1 header
4. Re-read ALL files to verify they work
5. Only then proceed with original task

For questions about these standards, consult the project maintainers or create an issue in the project repository.