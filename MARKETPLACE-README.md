# WitchCityRope Internal Marketplace

## Overview

This is an internal Claude Code plugin marketplace for WitchCityRope and company-wide projects.

## Installation

### Add this marketplace to your Claude Code:

```bash
# From GitHub (when pushed)
/plugin marketplace add DarkMonkDev/WitchCityRope

# From local path (for development/testing)
/plugin marketplace add /home/chad/repos/witchcityrope
```

### Install the WitchCityRope agent system:

```bash
/plugin install witchcityrope-agents@witchcityrope-internal
```

## What's Included

### 16 Specialized Agents

**Planning (3 agents):**
- `business-requirements` - Requirements analysis for community platforms
- `functional-spec` - Technical specifications for React + .NET
- `technology-researcher` - Architecture decisions and tech evaluation

**Development (4 agents):**
- `react-developer` - React + TypeScript + Mantine v7 development
- `backend-developer` - .NET 10 API + Entity Framework + PostgreSQL
- `database-designer` - Schema design and optimization
- `ui-designer` - UI/UX design with Mantine framework

**Testing (3 agents):**
- `test-developer` - Test suite creation (Vitest, Playwright, xUnit)
- `test-executor` - Test execution and environment management
- `code-reviewer` - Code quality and security review

**Quality (2 agents):**
- `lint-validator` - ESLint and code quality validation
- `prettier-formatter` - Code formatting automation

**Utility (2 agents):**
- `librarian` - Documentation and file organization
- `git-manager` - Version control operations

**Research (1 agent):**
- `technology-researcher` - Technology evaluation and ADRs

### Orchestration System

- **5-phase workflow**: Requirements → Design → Implementation → Testing → Finalization
- **Quality gates by work type**: Context-appropriate rigor
- **Mandatory human reviews**: After requirements, after first vertical slice
- **Phase validation**: Librarian blocking authority prevents disasters

### Unique Innovations

#### 1. The Three Laws of Agent Tools
```
1. An agent will use any tool it has access to
2. No instruction can reliably prevent tool usage  
3. Tool restriction is the only reliable control
```

**Application**: Remove tools to enforce delegation, not instructions.

#### 2. Lessons Learned System (14,814+ lines)
- 30 role-specific files
- Prevention-focused format (Problem → Solution)
- Format enforcement with validation checklist
- Continuous updates as new mistakes discovered

#### 3. Phase Validation with Blocking Authority
- Librarian agent can BLOCK workflow progression
- Zero tolerance for violations
- Prevents file organization disasters

#### 4. Exclusive Test Ownership
- test-executor owns ALL test infrastructure
- backend-developer FORBIDDEN from test files
- Prevents role violations that break systems

#### 5. Quality Gates by Work Type
- Features: R:95% → D:90% → I:85% → T:100%
- Bugs: R:80% → D:70% → I:75% → T:100%
- Hotfixes: R:70% → D:60% → I:70% → T:100%

## Usage

### Start a new feature:

```bash
/orchestrate
```

The orchestrator will guide you through the 5-phase workflow.

### Use specific agents directly:

```bash
# Research technology options
Use the technology-researcher agent

# Create UI designs
Use the ui-designer agent

# Review code
Use the code-reviewer agent
```

## Project Stack

- **Frontend**: React 18 + TypeScript + Vite + Mantine v7
- **Backend**: .NET 10 Minimal API + C# 12
- **Database**: PostgreSQL 15+ + Entity Framework Core 10
- **Testing**: Playwright (E2E), Vitest (React), xUnit (API)
- **Architecture**: Microservices (Web + API), Vertical slice pattern

## Documentation

Complete documentation available in:
- `/docs/functional-areas/` - Feature-specific documentation
- `/docs/lessons-learned/` - 30 role-specific lessons files
- `/docs/standards-processes/` - Development standards
- `/docs/architecture/` - Architecture decisions (ADRs)

## Version History

### 1.0.0 (2025-11-04)
- Initial plugin release
- 16 specialized agents
- 5-phase orchestration workflow
- 14,814+ lines of lessons learned
- Complete React + .NET development system

## Support

For issues or questions:
- **Internal**: Contact WitchCityRope development team
- **GitHub**: https://github.com/DarkMonkDev/WitchCityRope/issues

## License

MIT License - See LICENSE file for details
