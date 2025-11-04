# WitchCityRope Plugin Installation Test Plan

**Date**: 2025-11-04
**Version**: 1.0
**Status**: Ready for Testing

## Prerequisites

- Claude Code installed
- Terminal access
- Git repository at `/home/chad/repos/witchcityrope`

## Test Procedure

### Step 1: Add Internal Marketplace

```bash
# From GitHub (when pushed)
/plugin marketplace add DarkMonkDev/WitchCityRope

# OR from local path (for development/testing)
/plugin marketplace add /home/chad/repos/witchcityrope
```

**Expected Result**: Marketplace `witchcityrope-internal` added successfully.

### Step 2: List Available Plugins

```bash
/plugin list
```

**Expected Result**: Should show `witchcityrope-agents` plugin available from `witchcityrope-internal` marketplace.

### Step 3: Install Plugin

```bash
/plugin install witchcityrope-agents@witchcityrope-internal
```

**Expected Result**: Plugin installs successfully with message showing:
- 16 agents installed
- 2 commands installed (if any exist in `.claude/commands/`)

### Step 4: Verify Agents Available

After installation, check that agents are available:

```bash
# Try using an agent (in a Claude Code conversation)
Use the business-requirements agent to analyze...
```

**Expected Result**: Agent should be available and invoke correctly.

### Step 5: Verify Agent Functionality

Test a few key agents:

1. **business-requirements agent**: Create sample requirements
2. **react-developer agent**: Check it can read React files
3. **test-executor agent**: Verify it can run tests
4. **librarian agent**: Test documentation organization

### Step 6: Test Cross-Project Usage

In another project (like accounting-automation):

```bash
# Add marketplace
/plugin marketplace add /home/chad/repos/witchcityrope

# Install plugin
/plugin install witchcityrope-agents@witchcityrope-internal

# Use an agent
Use the git-manager agent to check status
```

**Expected Result**: Agents work in different project contexts.

## Verification Checklist

- [ ] Marketplace added successfully
- [ ] Plugin shows in available list
- [ ] Plugin installs without errors
- [ ] All 16 agents are accessible
- [ ] Agents can be invoked via Task tool
- [ ] Agents function correctly in their roles
- [ ] Plugin works across multiple projects
- [ ] Agent tool restrictions are enforced
- [ ] Lessons learned files are accessible to agents

## Known Issues

None identified yet. Document any issues found during testing.

## Troubleshooting

### Issue: Marketplace Not Found
**Solution**: Verify path is correct and `marketplace.json` exists in repo root.

### Issue: Plugin Won't Install
**Solution**: Check that `.claude-plugin/plugin.json` exists and is valid JSON.

### Issue: Agents Not Available
**Solution**: Verify agents are in `.claude/agents/` directory and use correct YAML frontmatter.

## Success Criteria

✅ Plugin installs cleanly in WitchCityRope project
✅ Plugin installs cleanly in other projects
✅ All 16 agents are functional
✅ Tool restrictions work as designed
✅ No errors or warnings during installation

## Next Steps After Testing

Once testing confirms the plugin works:

1. Push to GitHub repository
2. Update other company projects to use the plugin
3. Begin creating complementary Skills
4. Document any lessons learned from testing

---

**Testing Notes Section**

Add your testing notes below:

```
Date:
Tester:
Results:


Issues Found:


Resolution:

```
