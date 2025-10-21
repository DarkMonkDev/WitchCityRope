# STRICT RULES FOR LESSONS LEARNED FILES

## PURPOSE
Lessons learned files contain ONLY prevention patterns and mistakes to avoid. They are NOT progress reports, project history, or celebration documents.

## ONLY ALLOWED CONTENT:

✅ **Prevention patterns** (what to avoid and why)
✅ **Common mistakes** and their solutions  
✅ **Brief "Problem/Solution" pairs** (max 2 sentences each)
✅ **Code snippets** showing wrong vs right (NO LINE LIMIT for valuable examples)
✅ **Error patterns** and how to prevent them

## VALUABLE PATTERNS TO KEEP (NOT REMOVE):

🔧 **"Pattern for Future Development"** sections WITH FULL CODE
🔧 **Complete working code examples** that show correct implementation
🔧 **Architecture decision patterns** with rationales
🔧 **Migration strategies** that prevent issues
🔧 **Configuration requirements** and examples
🔧 **Testing patterns** that avoid common failures
🔧 **Performance optimization** patterns
🔧 **Security patterns** and requirements
🔧 **Integration patterns** between systems
🔧 **Common pitfall warnings** with solutions
🔧 **Root cause analyses** that prevent recurrence
🔧 **Critical warnings** about system behavior
🔧 **API usage patterns** that prevent errors
🔧 **Deployment patterns** that avoid failures
🔧 **Debugging strategies** for common issues

These patterns are LESSONS LEARNED - they teach future sessions how to avoid problems!

## ABSOLUTELY FORBIDDEN IN LESSONS LEARNED:

❌ **Status reports** or progress updates
❌ **"Successfully completed" entries**
❌ **Project history** or timelines
❌ **Celebration or achievement entries**
❌ **Task completion summaries**
❌ **"MAJOR SUCCESS" declarations**
❌ **Results summaries**
❌ **Context sections with project details**
❌ **Business impact sections**

## VALUABLE CONTENT THAT BELONGS ELSEWHERE:

📚 **Implementation guides** → `/home/chad/repos/witchcityrope/docs/guides-setup/` or `/home/chad/repos/witchcityrope/docs/functional-areas/[area]/implementation/`
📚 **Code examples** (>3 lines) → `/home/chad/repos/witchcityrope/docs/functional-areas/[area]/code-examples/`
📚 **Integration strategies** → `/home/chad/repos/witchcityrope/docs/architecture/` or `/home/chad/repos/witchcityrope/docs/functional-areas/[area]/architecture/`
📚 **Detailed workflows** → `/home/chad/repos/witchcityrope/docs/standards-processes/` or `/home/chad/repos/witchcityrope/docs/functional-areas/[area]/workflows/`
📚 **API documentation** → `/home/chad/repos/witchcityrope/docs/functional-areas/[area]/api/`
📚 **Testing strategies** → `/home/chad/repos/witchcityrope/docs/functional-areas/testing/strategies/`

## CORRECT FORMAT EXAMPLES:

### ✅ RIGHT:
```markdown
**Problem**: Creating files in /home/chad/repos/witchcityrope/docs/ root breaks navigation.
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

## CRITICAL: CODE EXAMPLES ARE LESSONS LEARNED

**NEVER REMOVE:**
- ✅ CORRECT code examples (these show what works!)
- ❌ WRONG code examples (these show what to avoid!)
- Complete endpoint implementations
- Full configuration examples
- Multi-line code patterns that work
- JWT/authentication examples
- Error handling patterns
- Service layer implementations

**Code examples ARE the lesson** - they're not "implementation details" to remove!

## FORMAT GUIDELINES

- Any lessons learned entry that includes pure status reports should be removed
- Keep ALL technical patterns and code examples
- Focus on preserving knowledge over reducing file size
- Organize by topic for easy scanning

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

## IMPORTANT: PRESERVE VALUABLE CONTENT

When cleaning up lessons learned files:
1. **DON'T DELETE** implementation guides - move them to appropriate locations
2. **DON'T DELETE** useful code examples - move to code-examples folders
3. **DON'T DELETE** architectural decisions - move to architecture docs
4. **CREATE NEW FILES** for valuable content that doesn't fit lessons learned format
5. **UPDATE FILE REGISTRY** when moving content to new locations

Remember: If it's not a mistake or prevention pattern, it doesn't belong in lessons learned - but it may still be valuable documentation that belongs elsewhere!