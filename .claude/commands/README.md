# Claude Code Commands Guide - WitchCityRope Project
<!-- Last Updated: 2025-01-20 -->

## Overview

This directory contains Claude Code command files that streamline development workflows for the WitchCityRope project. Commands are markdown files that can be invoked using the `/command_name` syntax in Claude Code.

## Available Commands

### 1. `/new_session` - Session Initialization
**Purpose**: Quick environment setup and verification for new Claude Code sessions
**When to use**: 
- Starting a fresh Claude Code session
- After system restart or Docker rebuild
- When switching between projects

**What it does**:
- Verifies Docker environment is running
- Provides quick access to project configuration
- Reviews recent progress
- Sets up initial context

### 2. `/new_work` - Task Execution
**Purpose**: Structured approach to implementing specific development tasks
**When to use**:
- Starting any new feature or bug fix
- Continuing work from a previous session
- Beginning any development task

**Requires**: `/new_session` to be run first if starting fresh
**Usage**: `/new_work "implement user profile editing feature"`

## Command Relationship

```
┌─────────────────┐
│  /new_session   │ ← Run first (once per session)
│                 │
│ • Check env     │
│ • Load context  │
│ • Quick setup   │
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│   /new_work     │ ← Run for each task
│                 │
│ • Pre-checklist │
│ • Execute task  │
│ • Post-checklist│
└─────────────────┘
```

## Key Files Referenced

1. **CLAUDE.md** 
   - Location: `/home/chad/repos/witchcityrope/WitchCityRope/CLAUDE.md`
   - Main project configuration
   - Persistent project knowledge
   - Architecture decisions
   - Updated by developers as project evolves

2. **PROGRESS.md** 
   - Location: `/home/chad/repos/witchcityrope/WitchCityRope/PROGRESS.md`
   - Development history
   - Current work status
   - Phase tracking
   - Updated after each work session

3. **Test Credentials**
   - Location: `/home/chad/repos/witchcityrope/.claude/test-credentials.env`
   - Contains test account information
   - Referenced by both commands

## Version Management

### Simple Version Tracking
- **Timestamps**: Each command file includes `<!-- Last Updated: YYYY-MM-DD -->`
- **Version Numbers**: Semantic versioning in comments `<!-- Version: X.Y -->`
- **Staleness Check**: Commands prompt review if key files are >7 days old
- **Manual Updates**: Developers update timestamps when making changes

### Maintaining Current Information
1. **CLAUDE.md**: Update when architecture or major decisions change
2. **PROGRESS.md**: Update after each work session
3. **Command Files**: Update when workflow changes significantly
4. **Documentation**: Keep phase documentation current in `/home/chad/repos/witchcityrope/WitchCityRope/docs/enhancements/`

## Best Practices

1. **Always run `/new_session` first** when starting fresh
2. **Use `/new_work` for each discrete task** to maintain structure
3. **Update PROGRESS.md** at the end of each work session
4. **Document phase transitions** when moving between development phases
5. **Keep commands lightweight** - detailed info goes in CLAUDE.md

## Extending Commands

To add new commands:
1. Create a new `.md` file in this directory
2. Use `$ARGUMENTS` placeholder for dynamic input
3. Follow the structured format with XML-style tags
4. Include version tracking comments
5. Update this README with the new command

## Troubleshooting

**Command not found**: Ensure you're in the project directory
**Outdated information**: Check timestamps and update if needed
**Phase confusion**: Review current phase in PROGRESS.md
**Missing context**: Run `/new_session` to reload project context

---

For detailed project information, always refer to the main CLAUDE.md file at `/home/chad/repos/witchcityrope/WitchCityRope/CLAUDE.md`.