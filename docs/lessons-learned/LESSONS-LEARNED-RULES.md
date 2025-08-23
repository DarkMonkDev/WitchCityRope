# STRICT RULES FOR LESSONS LEARNED FILES

## PURPOSE
Lessons learned files contain ONLY prevention patterns and mistakes to avoid. They are NOT progress reports, project history, or celebration documents.

## ONLY ALLOWED CONTENT:

✅ **Prevention patterns** (what to avoid and why)
✅ **Common mistakes** and their solutions  
✅ **Brief "Problem/Solution" pairs** (max 2 sentences each)
✅ **Code snippets** showing wrong vs right (max 3 lines)
✅ **Error patterns** and how to prevent them

## ABSOLUTELY FORBIDDEN:

❌ **Status reports** or progress updates
❌ **"Successfully completed" entries**
❌ **Project history** or timelines
❌ **Implementation guides** or tutorials
❌ **Code examples** longer than 3 lines
❌ **Celebration or achievement entries**
❌ **Task completion summaries**
❌ **"MAJOR SUCCESS" declarations**
❌ **Integration strategy descriptions**
❌ **Detailed workflow explanations**
❌ **Results summaries**
❌ **Context sections with project details**

## CORRECT FORMAT EXAMPLES:

### ✅ RIGHT:
```markdown
**Problem**: Creating files in /docs/ root breaks navigation.
**Solution**: Always use functional area subdirectories.
```

```markdown
**Problem**: Agents add status reports to lessons learned.
**Solution**: Use /session-work/ for progress, lessons for mistakes only.
```

### ❌ WRONG:
```markdown
## MAJOR SUCCESS: File Consolidation Complete (2025-08-23)

**ACHIEVEMENT**: Successfully cleaned up and consolidated all duplicate files...
```

```markdown
**FILES CONSOLIDATED**:
- ✅ React Developer: Merged files successfully
- ✅ Backend Developer: Preserved 1,430 lines
**RESULTS**: Zero duplicate files, no information loss...
```

## FORMAT VIOLATIONS = IMMEDIATE REJECTION

- Any lessons learned entry that includes status reports will be rejected
- Files must be rewritten to contain only prevention patterns
- Maximum 75 lines per file to prevent bloat
- Must be scannable in under 30 seconds

## ENFORCEMENT PROCESS:

1. **Detection**: Automated validation checks for forbidden content
2. **Rejection**: Non-compliant entries rejected immediately  
3. **Rewrite Required**: Agent must rewrite in proper format
4. **Template Reference**: Use LESSONS-LEARNED-TEMPLATE.md for structure
5. **No Exceptions**: Rules apply to ALL agents and ALL lessons learned files

## WHY THIS MATTERS:

- **Agents need prevention patterns**, not project history
- **Lessons learned ≠ Progress reports** - they serve different purposes
- **Quick reference required** - agents scan for specific patterns
- **Prevent file bloat** - keep focused on actionable knowledge
- **Clear boundaries** - prevents confusion about file purpose

Remember: If it's not a mistake or prevention pattern, it doesn't belong in lessons learned.