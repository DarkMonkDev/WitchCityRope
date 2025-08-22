# KEY PROJECT DOCUMENTS - SINGLE SOURCE OF TRUTH
<!-- Last Updated: 2025-08-22 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: ENFORCEMENT ACTIVE -->

## 🚨 PURPOSE: PREVENT DOCUMENTATION DISASTERS 🚨

This document defines the **EXACT CANONICAL LOCATIONS** and **OWNERSHIP** of all critical project documents to prevent the duplicate file disasters we experienced on 2025-08-22 where we discovered:
- 4 duplicate archive folders
- 32 misplaced MD files in wrong locations
- Duplicate key documents (ARCHITECTURE.md in both root and docs)
- Complete violation of documentation standards

**THIS IS THE FINAL AUTHORITY ON DOCUMENT OWNERSHIP AND LOCATION.**

## 🏛️ ROOT DIRECTORY DOCUMENTS - THE SACRED SIX

**These 6 files MUST exist ONLY in project root (`/`) - NEVER DUPLICATED anywhere:**

| Document | Canonical Location | Purpose | Owner | Update Frequency | GitHub Standard |
|----------|-------------------|---------|-------|------------------|----------------|
| **CLAUDE.md** | `/CLAUDE.md` | AI assistant configuration, project instructions for Claude Code | All Agents | Per major workflow change | ✅ Claude Code Standard |
| **PROGRESS.md** | `/PROGRESS.md` | Current project status, sprint progress, active work tracking | Orchestrator | Daily/Per session | ✅ Project Management |
| **README.md** | `/README.md` | Project overview, quick start, technology stack | Product Owner | Per major release | ✅ GitHub Standard |
| **ARCHITECTURE.md** | `/ARCHITECTURE.md` | System architecture overview, key decisions, patterns | Solution Architect | Per architecture change | ✅ Documentation Standard |
| **SECURITY.md** | `/SECURITY.md` | Security policies, vulnerability reporting | Security Team | Per security review | ✅ GitHub Security Standard |
| **ROADMAP.md** | `/ROADMAP.md` | Product roadmap, feature timeline, strategic direction | Product Owner | Quarterly | ✅ Planning Standard |

### ⚠️ CRITICAL ENFORCEMENT RULES FOR ROOT DOCUMENTS

1. **ZERO TOLERANCE**: ANY duplicate of these files = EMERGENCY
2. **SINGLE SOURCE OF TRUTH**: Each exists in EXACTLY ONE location
3. **NO /docs/ COPIES**: These files NEVER belong in `/docs/` folder
4. **IMMEDIATE VIOLATION**: Creation of duplicate triggers emergency fix
5. **VERSION CONTROL**: Only ONE version maintained, historical content archived properly

## 📁 DOCUMENTATION STRUCTURE AUTHORITY

### /docs/ Directory Structure - NO FILES IN ROOT

**CRITICAL**: The `/docs/` directory contains ZERO files in its root level. Only subdirectories are allowed:

```
/docs/                              # ZERO FILES ALLOWED IN ROOT
├── functional-areas/               # Feature-specific documentation
├── standards-processes/            # Development standards and processes
├── architecture/                   # System design and technical decisions
├── lessons-learned/                # Agent-specific experience and failures
├── guides-setup/                   # Setup and operational guides
├── design/                         # UI/UX designs, wireframes, mockups
└── _archive/                       # Historical documentation (SINGLE archive only)
```

### Functional Area Document Authority

**Pattern**: `/docs/functional-areas/[area-name]/`

| Document Type | Location Pattern | Owner | Purpose | Update Trigger |
|---------------|------------------|-------|---------|----------------|
| **README.md** | `/docs/functional-areas/[area]/README.md` | Feature Team | Area overview, navigation, status | Per feature milestone |
| **Business Requirements** | `/docs/functional-areas/[area]/new-work/YYYY-MM-DD-[feature]/requirements/business-requirements.md` | Business Analyst | User stories, acceptance criteria | Requirements phase |
| **Functional Specification** | `/docs/functional-areas/[area]/new-work/YYYY-MM-DD-[feature]/requirements/functional-spec.md` | Solution Architect | Technical design, API specs | Design phase |
| **Test Plan** | `/docs/functional-areas/[area]/new-work/YYYY-MM-DD-[feature]/testing/test-plan.md` | Test Engineer | Test strategy, coverage, scenarios | Testing phase |
| **Implementation Status** | `/docs/functional-areas/[area]/new-work/YYYY-MM-DD-[feature]/STATUS.md` | Development Team | Progress, blockers, decisions | Per development session |

## 🧭 NAVIGATION AND DISCOVERY DOCUMENTS

### Master Navigation Authority

| Document | Canonical Location | Purpose | Owner | Critical For |
|----------|-------------------|---------|-------|-------------|
| **Functional Area Master Index** | `/docs/architecture/functional-area-master-index.md` | Single source of truth for all functional areas and active work | Librarian | ALL agents - MANDATORY first check |
| **File Registry** | `/docs/architecture/file-registry.md` | Complete log of all file operations | Librarian | File operation tracking |
| **Document Authority** | `/docs/standards-processes/CANONICAL-DOCUMENT-LOCATIONS.md` | Canonical locations for all critical files | Librarian | Emergency reference |
| **Key Project Documents** | `/docs/standards-processes/KEY-PROJECT-DOCUMENTS.md` | THIS FILE - Single source of truth for document ownership | Librarian | Document creation decisions |

## 🚨 DISASTER PREVENTION RULES

### What We NEVER Allow Again:

#### 1. Duplicate Key Files
- ❌ **CLAUDE.md** in both `/` and `/docs/`
- ❌ **ARCHITECTURE.md** in both `/` and `/docs/`
- ❌ **PROGRESS.md** in both `/` and `/docs/`
- ❌ **README.md** files scattered everywhere
- ❌ Multiple **ROADMAP.md** files

#### 2. Multiple Archive Disasters
- ❌ 4 different archive folders
- ❌ Uncoordinated archiving efforts
- ❌ Lost content during archiving
- ✅ ONLY `/docs/_archive/` allowed

#### 3. Root Directory Pollution
- ❌ ANY file in project root except the Sacred Six
- ❌ Temporary files left in root
- ❌ Session work files in root
- ❌ Documentation files created in root

#### 4. Structure Violations
- ❌ Files created in `/docs/` root level
- ❌ `/docs/docs/` nested folder disasters
- ❌ Random file placement
- ❌ Ignoring functional area structure

## 🔍 VALIDATION CHECKPOINTS

### Before ANY File Operation:
1. **Check THIS Document**: Verify canonical location
2. **Check Master Index**: Confirm functional area exists
3. **Check File Registry**: See if similar file exists
4. **Validate Structure**: Ensure proper location
5. **Update Registry**: Log the operation

### Mandatory Validation Tools:
- **Structure Validator**: `/docs/architecture/docs-structure-validator.sh`
- **File Registry**: `/docs/architecture/file-registry.md`
- **Canonical Locations**: `/docs/standards-processes/CANONICAL-DOCUMENT-LOCATIONS.md`

## 📋 DOCUMENT LIFECYCLE MANAGEMENT

### Creation Process:
1. **Purpose Check**: Does this document serve a unique purpose?
2. **Location Verification**: Using this document, where does it belong?
3. **Duplicate Check**: Does similar content exist elsewhere?
4. **Owner Assignment**: Who maintains this document?
5. **Registry Entry**: Log in file registry
6. **Index Update**: Update master index if functional area

### Update Process:
1. **Canonical Check**: Confirm updating the authoritative version
2. **Content Review**: Ensure no duplication with other docs
3. **Version Update**: Update header metadata
4. **Cross-Reference**: Update any navigation or index references
5. **Registry Update**: Log the modification

### Archive Process:
1. **Content Assessment**: What unique value does this provide?
2. **Consolidation**: Merge valuable content into active docs
3. **Archive Creation**: Move to `/docs/_archive/[area]/YYYY-MM-DD/`
4. **Reference Updates**: Update any links or references
5. **Registry Archive**: Mark as archived in file registry

## 🚦 ENFORCEMENT ACTIONS

### Phase-Based Validation (Implemented Below):
- **Requirements Phase**: Validate functional area structure
- **Design Phase**: Check for duplicate architecture documents
- **Implementation Phase**: Monitor file creation locations
- **Testing Phase**: Validate test documentation placement
- **Finalization Phase**: Comprehensive structure validation

### Violation Response:
1. **IMMEDIATE**: Stop all work, assess violation
2. **PRESERVE**: Backup any unique content
3. **LOCATE**: Determine canonical location from this document
4. **MIGRATE**: Move content to proper location
5. **VALIDATE**: Confirm resolution with structure validator
6. **REGISTRY**: Update file registry with corrective action

## 📊 SUCCESS METRICS

### Zero Tolerance Targets:
- **Duplicate Key Documents**: 0
- **Files in /docs/ Root**: 0
- **Multiple Archive Folders**: 0 (only `_archive` allowed)
- **Structure Validator Failures**: 0
- **Canonical Location Violations**: 0

### Compliance Tracking:
- Daily structure validation
- Weekly document audit
- Monthly archive review
- Quarterly canonical location verification

## 🔧 INTEGRATION WITH AI WORKFLOW

### Orchestrator Responsibilities:
- Check this document before ANY file creation
- Validate structure at each phase boundary
- Block workflow progression on violations
- Require librarian validation for file operations

### Agent Responsibilities:
- Reference this document for file placement decisions
- Never create files without checking canonical locations
- Report violations immediately
- Update file registry for all operations

### Librarian Authority:
- Final authority on document placement
- Power to block workflows for violations
- Responsibility for structure validation
- Maintenance of all navigation documents

---

**This document represents the final authority on project document management. Any deviation from these rules constitutes a critical violation requiring immediate correction.**

**Last Emergency**: 2025-08-22 - Catastrophic duplicate file discovery
**Prevention Status**: ACTIVE - Zero tolerance enforcement
**Validation**: Daily automated checks + phase-gate validation