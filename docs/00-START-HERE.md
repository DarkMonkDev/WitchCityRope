# WitchCityRope Documentation - Start Here
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 1.0 -->
<!-- Owner: Project Management Team -->
<!-- Status: Active -->

## 🚀 Quick Navigation

Welcome to the WitchCityRope documentation. This guide helps you find exactly what you need based on your role and current task.

### For New Team Members
1. **Read First**: [/CLAUDE.md](/CLAUDE.md) - Critical project configuration and warnings
2. **Architecture**: [architecture/current-state.md](architecture/current-state.md) - System overview
3. **Development Setup**: [guides-setup/developer-quick-start.md](guides-setup/developer-quick-start.md)
4. **Your Role's Lessons**: [lessons-learned/](lessons-learned/) - Find your role-specific file

### By Role

#### 🎨 UI Developers (Blazor)
- **Lessons Learned**: [lessons-learned/ui-developers.md](lessons-learned/ui-developers.md)
- **Component Catalog**: [standards-processes/ui-components/component-catalog.md](standards-processes/ui-components/component-catalog.md)
- **Coding Standards**: [standards-processes/CODING_STANDARDS.md](standards-processes/CODING_STANDARDS.md)
- **Active Work**: Check relevant [functional-areas/*/new-work/status.md](functional-areas/)

#### 🔧 Backend Developers (C#/API)
- **Lessons Learned**: [lessons-learned/backend-developers.md](lessons-learned/backend-developers.md)
- **API Standards**: [standards-processes/development-standards/api-guidelines.md](standards-processes/development-standards/api-guidelines.md)
- **Architecture Decisions**: [architecture/decisions/](architecture/decisions/)
- **Active Work**: Check relevant [functional-areas/*/new-work/status.md](functional-areas/)

#### 🧪 Test Writers
- **Lessons Learned**: [lessons-learned/test-writers.md](lessons-learned/test-writers.md)
- **Test Catalog**: [standards-processes/testing/test-catalog.md](standards-processes/testing/test-catalog.md)
- **Testing Standards**: [standards-processes/testing/](standards-processes/testing/)
- **E2E Tests**: [tests/e2e/test-catalog.md](/tests/e2e/test-catalog.md)

#### 🎯 Wireframe/UX Designers
- **Lessons Learned**: [lessons-learned/wireframe-designers.md](lessons-learned/wireframe-designers.md)
- **Current Wireframes**: Browse [functional-areas/*/wireframes/](functional-areas/)
- **Design System**: [standards-processes/ui-components/](standards-processes/ui-components/)

#### 🗄️ Database Developers
- **Lessons Learned**: [lessons-learned/database-developers.md](lessons-learned/database-developers.md)
- **PostgreSQL Config**: [functional-areas/database/POSTGRESQL_CONFIGURATION.md](functional-areas/database/POSTGRESQL_CONFIGURATION.md)
- **Migration Guide**: [functional-areas/database/postgresql-migration.md](functional-areas/database/postgresql-migration.md)

#### 🚀 DevOps Engineers
- **Lessons Learned**: [lessons-learned/devops-engineers.md](lessons-learned/devops-engineers.md)
- **Docker Setup**: [DOCKER_DEV_GUIDE.md](DOCKER_DEV_GUIDE.md)
- **CI/CD**: [functional-areas/deployment/CI_CD_GUIDE.md](functional-areas/deployment/CI_CD_GUIDE.md)

### By Task

#### 📝 Starting New Feature Work
1. Find your functional area in [functional-areas/](functional-areas/)
2. Read `current-state/business-requirements.md` for context
3. Check `new-work/status.md` for any active work
4. Update `new-work/status.md` with your session info

#### 🐛 Fixing Bugs
1. Check relevant [lessons-learned/](lessons-learned/) for known issues
2. Find functional area in [functional-areas/](functional-areas/)
3. Read `current-state/functional-design.md` for implementation details
4. Update `new-work/status.md` during work

#### 📊 Understanding Current System
- **Business View**: [functional-areas/*/current-state/business-requirements.md](functional-areas/)
- **Technical View**: [functional-areas/*/current-state/functional-design.md](functional-areas/)
- **UI/UX View**: [functional-areas/*/current-state/wireframes.md](functional-areas/)
- **Test Coverage**: [functional-areas/*/current-state/test-coverage.md](functional-areas/)

### 📂 Documentation Structure

```
docs/
├── 00-START-HERE.md              # You are here
├── functional-areas/             # Feature-specific documentation
│   ├── authentication/           # Login, users, roles
│   ├── events-management/        # Event creation, RSVP
│   ├── membership-vetting/       # Vetting process
│   ├── payments/                 # Payment processing
│   └── user-dashboard/           # Member dashboard
├── standards-processes/          # How we work
│   ├── documentation-process/    # How to document
│   ├── development-standards/    # Coding standards
│   ├── testing/                  # Test standards
│   └── ui-components/            # Component library
├── architecture/                 # System design
├── lessons-learned/              # Role-based learnings
└── guides-setup/                 # Setup guides
```

### 🔍 Finding Information

#### Current Work Status
- **Overall Progress**: [/PROGRESS.md](/PROGRESS.md)
- **Feature-Specific**: [functional-areas/*/new-work/status.md](functional-areas/)

#### Historical Information
- **Development History**: [functional-areas/*/development-history.md](functional-areas/)
- **Architecture Decisions**: [architecture/decisions/](architecture/decisions/)
- **Completed Work**: Check git history after archival

#### Standards & Processes
- **All Standards**: [standards-processes/](standards-processes/)
- **Documentation Process**: [standards-processes/documentation-process/](standards-processes/documentation-process/)
- **Component Catalog**: [standards-processes/ui-components/component-catalog.md](standards-processes/ui-components/component-catalog.md)

### ⚠️ Important Rules

1. **NEVER** create documentation outside this structure without approval
2. **ALWAYS** update `new-work/status.md` during work sessions
3. **REPLACE** content in current-state files (don't append history)
4. **COMMIT** to git before removing any archived files
5. **CHECK** your role's lessons learned before starting work

### 🆘 Need Help?

- **Can't find something?** Check if it was archived (git history)
- **Not sure where to put new docs?** Ask the project manager
- **Found outdated info?** Update it following the process guide
- **Have lessons to share?** Add to your role's lessons learned file

---

*This is your starting point for all documentation. Bookmark this page!*