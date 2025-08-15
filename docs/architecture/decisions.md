# Architectural Decisions

## Project Overview
Witch City Rope is a membership and event management system for a 600-member rope bondage educational community. This document captures key architectural decisions made during the initial design phase.

## Decision Log

### ADR-001: Simplified Vertical Slice Architecture
**Date:** 2024-12-25  
**Status:** Accepted

**Context:**  
We need an architecture that balances maintainability with simplicity for a relatively small-scale application (600 users).

**Decision:**  
We will use a simplified vertical slice architecture organized by business features rather than technical layers.

**Consequences:**  
- ✅ Easy to understand and navigate
- ✅ Features are self-contained
- ✅ Reduced complexity compared to full DDD
- ⚠️ Some code duplication might occur between features
- ✅ New developers can quickly understand the codebase

**Structure:**
```
src/Features/
├── Vetting/      # Member applications
├── Events/       # Event management
├── CheckIn/      # Event day operations
├── Payments/     # PayPal integration
└── Safety/       # Incident reporting
```

---

### ADR-002: SQLite Database Instead of PostgreSQL
**Date:** 2024-12-25  
**Status:** Accepted

**Context:**  
Originally planned for PostgreSQL, but analyzing the requirements shows a simple data model with low concurrent users.

**Decision:**  
Use SQLite as the primary database.

**Consequences:**  
- ✅ Zero database hosting costs
- ✅ Simple backup strategy (file copy)
- ✅ No connection string management
- ✅ Perfect for <1000 concurrent users
- ⚠️ Limited concurrent write performance
- ✅ Easy local development
- ⚠️ No built-in replication

**Mitigation:**  
- Nightly backups to cloud storage
- Read-heavy workload fits SQLite perfectly
- Can migrate to PostgreSQL later if needed

---

### ADR-003: Monolithic Deployment
**Date:** 2024-12-25  
**Status:** Accepted

**Context:**  
With 600 users and simple requirements, we need to choose between microservices and monolithic architecture.

**Decision:**  
Deploy as a single containerized monolith.

**Consequences:**  
- ✅ Simple deployment and operations
- ✅ Lower hosting costs ($5-10/month)
- ✅ No service coordination complexity
- ✅ Easy debugging and monitoring
- ⚠️ Must scale vertically only
- ✅ Perfect for current scale

---

### ADR-004: Minimal Abstractions
**Date:** 2024-12-25  
**Status:** Accepted

**Context:**  
Need to balance SOLID principles with pragmatic simplicity.

**Decision:**  
Avoid repository pattern, use DbContext directly in features. Minimize interfaces and abstractions.

**Consequences:**  
- ✅ Less boilerplate code
- ✅ Faster development
- ✅ Easier to understand
- ⚠️ Tighter coupling to EF Core
- ✅ Can add abstractions later if needed

---

### ADR-005: PayPal as Sole Payment Provider
**Date:** 2024-12-25  
**Status:** Accepted

**Context:**  
Need payment processing for event tickets ($0-65 range).

**Decision:**  
Use PayPal Checkout exclusively, no direct credit card processing.

**Consequences:**  
- ✅ No PCI compliance requirements
- ✅ Trusted payment provider
- ✅ Handles refunds automatically
- ⚠️ PayPal fees (~3%)
- ⚠️ Some users may not have PayPal
- ✅ Simple integration

---

### ADR-006: Server-Side Blazor for UI
**Date:** 2024-12-25  
**Status:** Accepted

**Context:**  
Need to choose between Blazor Server, Blazor WebAssembly, or traditional MVC.

**Decision:**  
Use Blazor Server for all UI components with Syncfusion component library.

**Consequences:**  
- ✅ Real-time UI updates
- ✅ No API layer needed
- ✅ Smaller download size
- ✅ Better for mobile devices
- ⚠️ Requires constant connection
- ⚠️ Higher server memory usage
- ✅ Perfect for event check-in scenarios
- ✅ Professional UI components via Syncfusion

**UI Framework:**  
See [UI Framework Decision](./ui-framework-decision.md) for details on Syncfusion selection.

---

### ADR-007: Feature-First Organization
**Date:** 2024-12-25  
**Status:** Accepted

**Context:**  
Need to organize code for maximum clarity and maintainability.

**Decision:**  
Organize code by business feature rather than technical concerns.

**Consequences:**  
- ✅ Business logic is cohesive
- ✅ Easy to find related code
- ✅ Natural boundaries for work
- ✅ Supports future modularization
- ⚠️ Some shared code duplication

**Example:**
```
Features/Events/
├── CreateEvent.cs        # Command handler
├── RegisterForEvent.cs   # Command handler
├── EventModels.cs        # DTOs and entities
└── EventService.cs       # Business logic
```

---

### ADR-008: GitHub Actions for CI/CD
**Date:** 2024-12-25  
**Status:** Accepted

**Context:**  
Need automated deployment pipeline.

**Decision:**  
Use GitHub Actions for all CI/CD needs.

**Consequences:**  
- ✅ Free for public repos
- ✅ Integrated with source control
- ✅ Good .NET support
- ✅ Easy secret management
- ✅ Supports Docker deployment

---

### ADR-009: Document-Driven Development with Claude
**Date:** 2024-12-25  
**Status:** Accepted

**Context:**  
Using Claude Code for AI-assisted development requires good documentation structure.

**Decision:**  
Maintain design docs in `/docs` folder with clear navigation for AI tools.

**Consequences:**  
- ✅ AI can understand project context
- ✅ Documentation stays updated
- ✅ Clear design-to-code traceability
- ⚠️ Requires documentation discipline
- ✅ Better onboarding for humans too

---

### ADR-010: Complete Design Phase Before Implementation
**Date:** 2024-12-25  
**Status:** Accepted

**Context:**  
Need to decide whether to start development with partial wireframes or complete all design work first.

**Decision:**  
Complete all wireframes and design documentation before beginning Blazor implementation.

**Consequences:**  
- ✅ Avoid costly rework during development
- ✅ Complete user journey validation
- ✅ Consistent design patterns across all screens
- ✅ Accurate development time estimates
- ✅ Better Syncfusion component mapping
- ⚠️ Longer time before coding starts
- ✅ Higher quality final product
- ✅ Reduced technical debt

**Remaining Design Work:**
- Authentication flows (login, register, 2FA, password reset)
- Member pages (My Tickets, Profile/Settings)
- Error pages (404, 403, 500)
- Admin pages (Members, Reports, Safety)
- Mobile navigation patterns
- Landing page copy finalization

---

## Future Considerations

### Potential Migration Paths
1. **SQLite → PostgreSQL**: If concurrent writes become an issue
2. **Monolith → Modular Monolith → Microservices**: If scale demands
3. **Blazor Server → Blazor WebAssembly**: If offline support needed

### Monitoring Decisions
- Start with basic logging to files
- Add Application Insights when budget allows
- Use health checks from day one

### Security Considerations
- All personal data encrypted at rest
- 2FA required for all accounts
- Admin actions logged
- Regular security reviews

---

### ADR-011: Direct Service Injection Instead of MediatR
**Date:** 2025-01-26  
**Status:** Accepted

**Context:**  
Need to decide between using MediatR for CQRS pattern or direct service injection for handling business logic.

**Decision:**  
Use direct service injection with dependency injection instead of MediatR.

**Consequences:**  
- ✅ Simpler codebase with fewer abstractions
- ✅ Better IntelliSense and IDE navigation support
- ✅ Easier debugging with direct call stacks
- ✅ Faster development without command/query boilerplate
- ✅ Lower learning curve for new developers
- ⚠️ Less decoupling between UI and business logic
- ✅ Can add messaging patterns later if needed

**Implementation:**
```csharp
// Direct service injection
public class EventService
{
    private readonly WcrDbContext _db;
    
    public async Task<Result> CreateEventAsync(CreateEventDto dto)
    {
        // Direct implementation
    }
}

// In Blazor component
@inject EventService EventService
```

---

## Decision Template

### ADR-XXX: [Decision Title]
**Date:** YYYY-MM-DD  
**Status:** Proposed/Accepted/Deprecated/Superseded

**Context:**  
What is the issue we're addressing?

**Decision:**  
What have we decided to do?

**Consequences:**  
- ✅ Positive outcomes
- ⚠️ Risks or trade-offs
- ❌ Negative outcomes (if any)

**Alternatives Considered:**
1. Alternative 1 - Why rejected
2. Alternative 2 - Why rejected
## ADR-012: Use SendGrid for Email Service

**Status**: Accepted  
**Date**: 2025-01-26

### Context
We need reliable email delivery for:
- Transactional emails (welcome, password reset, event confirmations)
- Bulk emails (event announcements to 600+ members)
- Template management for consistent branding

### Decision
Use SendGrid as our email service provider.

### Rationale
- **Reliability**: Industry-leading deliverability rates
- **Scalability**: Can handle our volume with room to grow
- **Templates**: Dynamic templates for consistent emails
- **Analytics**: Track opens, clicks, and bounces
- **Cost**: Free tier (100/day) then affordable at scale
- **.NET SDK**: Excellent integration with ASP.NET Core

### Consequences
- **Positive**: Professional email delivery without managing SMTP
- **Positive**: Built-in unsubscribe handling for compliance
- **Negative**: Monthly cost after free tier
- **Negative**: Dependency on external service

## ADR-013: Use IMemoryCache for Caching

**Status**: Accepted  
**Date**: 2025-01-26

### Context
We need caching to improve performance for:
- Event listings (frequently accessed)
- User profiles and permissions
- Static configuration data

### Decision
Use .NET's built-in IMemoryCache for caching.

### Rationale
- **Simplicity**: No external dependencies (Redis not needed)
- **Performance**: In-memory caching is very fast
- **Cost**: No additional infrastructure
- **Scale**: Perfect for single-server deployment
- **Integration**: Native to ASP.NET Core

### Consequences
- **Positive**: Simple implementation and deployment
- **Positive**: No additional costs or services
- **Negative**: Cache lost on application restart
- **Negative**: Cannot share cache across multiple servers
- **Trade-off**: If we scale to multiple servers, we'll need Redis

---

These decisions shape the architecture to be simple, cost-effective, and maintainable for a solo developer while still delivering professional results.
