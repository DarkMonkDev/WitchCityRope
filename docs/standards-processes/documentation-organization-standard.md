# Documentation Organization Standard
<!-- Last Updated: 2025-09-08 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Purpose
This standard provides clear guidance on organizing documentation for features that span multiple areas, preventing confusion and duplication while maintaining logical structure.

## Critical Rule: NO FILES IN /docs/ ROOT
**ZERO TOLERANCE**: No files are permitted in `/docs/` root directory. All content must be organized in subdirectories. See `/docs/standards-processes/CANONICAL-DOCUMENT-LOCATIONS.md` for enforcement details.

## Cross-Cutting Feature Organization

### Primary Principle: Domain-Based Organization
When a feature spans multiple user interfaces or contexts, organize by **primary domain** with **context-specific subfolders**.

### Example: Events Feature Structure
```
/docs/functional-areas/events/
├── README.md                              # Overview of all events functionality
├── requirements/                          # Domain-wide requirements
├── public-events/                         # Public-facing event discovery
│   ├── event-list.html
│   ├── event-detail.html
│   └── wireframes/
├── admin-events-management/               # Admin event creation/management
│   ├── admin-events.html
│   ├── event-creation.html
│   └── current-state/
├── user-dashboard/                        # Member event features in dashboard
│   ├── phase5-requirements.md
│   ├── registration-tracking.md
│   └── member-features.md
└── new-work/                              # Active development across contexts
    ├── 2025-08-24-events-management/     # Backend work
    └── 2025-09-07-public-events-pages/   # Frontend work
```

## Organization Decision Matrix

### When to Use Subfolders vs Separate Functional Areas

| Scenario | Approach | Structure | Example |
|----------|----------|-----------|---------|
| **Single Domain, Multiple Contexts** | Subfolders under primary domain | `/docs/functional-areas/events/[context]/` | Events: public, admin, user dashboard |
| **Shared Infrastructure** | Separate functional area | `/docs/functional-areas/authentication/` | Auth used by all features |
| **Independent Features** | Separate functional areas | `/docs/functional-areas/payments/` | Payment processing |
| **Cross-Feature Infrastructure** | Architecture docs | `/docs/architecture/` | Database design, API patterns |

### Context-Specific Subfolder Guidelines

#### When to Create Context Subfolders
- ✅ **Same core business domain** (e.g., Events)
- ✅ **Shared data models** (e.g., Event entity)
- ✅ **Related user workflows** (e.g., view events → register → manage)
- ✅ **Common business rules** (e.g., ticket types, session matrices)

#### When to Create Separate Functional Areas
- ✅ **Independent business domains** (e.g., User Management vs Payment Processing)
- ✅ **Different data models** (e.g., User vs Event entities)
- ✅ **Separate user workflows** (e.g., Profile Settings vs Event Registration)
- ✅ **Distinct business rules** (e.g., Authentication vs Event Ticketing)

## Resolving Documentation Location Conflicts

### Events vs User Dashboard Example
**Problem**: Phase 5 user dashboard event features could go in either:
- `/docs/functional-areas/events/user-dashboard/`
- `/docs/functional-areas/user-dashboard/events/`

**Resolution**: Use **primary business domain**
- ✅ **CORRECT**: `/docs/functional-areas/events/user-dashboard/` (Events is primary domain)
- ❌ **INCORRECT**: `/docs/functional-areas/user-dashboard/events/` (Dashboard is UI context, not business domain)

**Rationale**: 
- Events drive the business logic and data models
- Dashboard is a presentation layer context
- Event features share business rules across contexts
- Consolidates all event documentation for easier maintenance

## Standard Functional Area Structure

### Required Structure for Cross-Cutting Features
```
/docs/functional-areas/[primary-domain]/
├── README.md                          # Domain overview linking all contexts
├── requirements/                      # Domain-wide business requirements
│   └── business-requirements.md
├── [context-1]/                       # First user context
│   ├── README.md                     # Context-specific overview
│   ├── requirements.md               # Context-specific requirements
│   └── wireframes/
├── [context-2]/                       # Second user context
│   ├── README.md
│   └── specifications.md
├── new-work/                          # Active development
│   └── YYYY-MM-DD-[work-description]/
└── current-state/                     # Existing implementation docs
    └── technical-design.md
```

### Context Subfolder Naming Conventions
- **public-[domain]**: Public-facing features (e.g., `public-events`)
- **admin-[domain]**: Admin management features (e.g., `admin-events-management`)
- **user-dashboard**: Member features within dashboard
- **api-[domain]**: Backend API documentation
- **mobile-[domain]**: Mobile-specific features (if applicable)

## Cross-Reference Guidelines

### Linking Between Contexts
When features span contexts, use clear cross-references:

```markdown
## Related Documentation
- **Public Events**: [Event List Page](../public-events/event-list.html)
- **Admin Management**: [Event Creation](../admin-events-management/event-creation.html)
- **API Integration**: [Backend APIs](../new-work/2025-08-24-events-management/)
```

### Shared Resources Location
Place shared resources at the domain level:
- `/docs/functional-areas/events/requirements/` - Domain business rules
- `/docs/functional-areas/events/wireframes/` - Cross-context designs
- `/docs/functional-areas/events/test-coverage.md` - Complete test strategy

## File Registry Updates Required

### Current Incorrect Locations
Some Phase 5 documentation may have been placed in:
- `/docs/functional-areas/user-dashboard/` (incorrect - UI context, not business domain)

### Correct Locations (Update Registry)
- ✅ `/docs/functional-areas/events/user-dashboard/phase5-requirements.md`
- ✅ `/docs/functional-areas/events/user-dashboard/member-features.md`
- ✅ `/docs/functional-areas/events/user-dashboard/registration-tracking.md`

## Agent Guidance

### For Business Requirements Agents
When documenting cross-cutting features:
1. **Identify primary business domain** (e.g., Events, Users, Payments)
2. **Create requirements at domain level** (`/docs/functional-areas/[domain]/requirements/`)
3. **Use context subfolders** for context-specific details
4. **Cross-reference related contexts** in all documents

### For React Developers
When implementing cross-cutting UI features:
1. **Check domain documentation first** (`/docs/functional-areas/[domain]/`)
2. **Find context-specific docs** (`/docs/functional-areas/[domain]/[context]/`)
3. **Document UI decisions** in appropriate context subfolder
4. **Link to shared business rules** at domain level

### For Backend Developers
When implementing domain logic:
1. **Use domain-level new-work folders** (`/docs/functional-areas/[domain]/new-work/`)
2. **Document APIs** that serve multiple contexts together
3. **Reference context-specific requirements** for UI integration
4. **Maintain business rule documentation** at domain level

## Validation Rules

### Structure Compliance Checklist
- [ ] No files in `/docs/` root directory
- [ ] Cross-cutting features organized by primary business domain
- [ ] Context subfolders use standard naming conventions
- [ ] README.md files link all related contexts
- [ ] Shared resources placed at appropriate level
- [ ] File registry entries use correct paths

### Quality Standards
- [ ] Clear cross-references between contexts
- [ ] Business rules documented once at domain level
- [ ] Context-specific details in appropriate subfolders
- [ ] No duplicate content across contexts
- [ ] Consistent naming conventions throughout

## Migration Guidelines

### When Reorganizing Existing Documentation
1. **Identify the primary business domain** for scattered docs
2. **Create the proper domain structure** if it doesn't exist
3. **Move documents to correct locations** preserving content
4. **Update all cross-references** to new locations
5. **Archive old structure** with explanation
6. **Update file registry** with all moves

### Handling Legacy Structures
- Archive obsolete structures to `/docs/_archive/` with date
- Create migration notes explaining new organization
- Update agent training materials
- Validate all links and references work

## Benefits of This Standard

### Maintainability
- Single source of truth for business domains
- Clear location expectations
- Consistent cross-referencing

### Discoverability  
- Logical organization by business domain
- Context subfolders for specific use cases
- Clear naming conventions

### Agent Coordination
- Reduces confusion about document placement
- Enables consistent handoff documentation
- Supports collaborative development

---

**This standard is mandatory for all documentation organization. Any deviations require librarian agent approval and must be documented with clear rationale.**