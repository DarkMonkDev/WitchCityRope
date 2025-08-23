# Lessons Learned Validation Checklist

## FOR ORCHESTRATOR: Reviewing Agent Lessons Learned Updates

### MANDATORY VALIDATION CHECKS

Before accepting ANY lessons learned file updates, verify:

#### 1. FORMAT COMPLIANCE
- [ ] **Header present**: Contains strict format comment header
- [ ] **No status reports**: Zero "Successfully completed" entries
- [ ] **No celebrations**: Zero "MAJOR SUCCESS" entries  
- [ ] **No project history**: Zero timeline or "On date X we did Y" entries
- [ ] **No implementation guides**: No code examples > 3 lines
- [ ] **File size**: Under 150 lines maximum

#### 2. CONTENT VALIDATION
- [ ] **Prevention patterns only**: Each entry follows "Problem/Solution" format
- [ ] **Concise entries**: Maximum 2 sentences per lesson
- [ ] **Actionable content**: Focuses on "what went wrong and how to prevent it"
- [ ] **No verbose explanations**: Removes project-specific context
- [ ] **Mistake-focused**: Documents errors to avoid, not successes

#### 3. FORBIDDEN CONTENT DETECTION
IMMEDIATELY REJECT if file contains:

❌ **Any of these phrases**:
- "Successfully completed"
- "MAJOR SUCCESS"  
- "ACHIEVEMENT"
- "FILES CONSOLIDATED"
- "RESULTS"
- "Integration Strategy"
- "Key Success Factors"
- "Context sections"
- Date-based headers (e.g., "2025-08-23 Update")

❌ **Content types**:
- Progress reports
- Project timelines
- Implementation checklists
- Workflow descriptions
- Achievement summaries
- Success stories

#### 4. REJECTION RESPONSE
If validation fails:

1. **Reject immediately**: "This lessons learned update violates format rules"
2. **Reference template**: "See LESSONS-LEARNED-TEMPLATE.md for correct format"
3. **Specify violations**: List exactly what needs to be removed/rewritten
4. **Require rewrite**: "Must rewrite as prevention patterns only"
5. **Block workflow**: Do not proceed until compliant

#### 5. ACCEPTANCE CRITERIA

✅ **Only accept if**:
- Contains prevention patterns and mistakes only
- Uses Problem/Solution format (1-2 sentences each)
- No status reports, celebrations, or project history
- Under 150 lines total
- Scannable in under 30 seconds
- Focuses on actionable knowledge

### EXAMPLE VALIDATION DIALOGUE

**WRONG SUBMISSION**:
```
## MAJOR SUCCESS: File Migration Complete
Successfully moved 47 files with zero data loss...
```

**CORRECT REJECTION**:
```
❌ REJECTED: Contains status report ("MAJOR SUCCESS", "Successfully completed")
📋 REQUIRED: Rewrite as prevention patterns only
📖 TEMPLATE: See LESSONS-LEARNED-TEMPLATE.md
🔄 ACTION: Extract prevention patterns from this success story
```

**CORRECT REWRITE**:
```
**Problem**: File migrations risk data loss without validation.
**Solution**: Always validate file contents before and after moves.
```

### ENFORCEMENT AUTHORITY

As orchestrator, you have ABSOLUTE AUTHORITY to:
- Reject non-compliant lessons learned updates
- Block workflow progression until compliance achieved  
- Require complete rewrites of violating content
- Enforce the strict format rules without exception

**NO EXCEPTIONS**: These rules apply to ALL agents, ALL files, ALL situations.