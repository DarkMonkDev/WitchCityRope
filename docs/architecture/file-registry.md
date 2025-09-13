# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
> 
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-09-12 | `/docs/functional-areas/testing-infrastructure/new-work/2025-09-12-containerized-testing/requirements/business-requirements.md` | CREATED | Comprehensive business requirements for enhanced containerized testing infrastructure with TestContainers v4.7.0 - addresses orphaned container prevention, production parity, CI/CD integration | Business Requirements Agent - Containerized Testing Infrastructure | ACTIVE | Keep permanent |
| 2025-09-12 | `/docs/functional-areas/testing-infrastructure/new-work/2025-09-12-containerized-testing/handoffs/business-requirements-2025-09-12-handoff.md` | CREATED | Mandatory agent handoff document for containerized testing requirements - critical business rules, stakeholder decisions, constraints, success criteria for design phase | Business Requirements Agent Handoff | ACTIVE | Keep permanent |
| 2025-09-12 | `/docs/functional-areas/testing-infrastructure/new-work/2025-09-12-containerized-testing/research/2025-09-12-containerized-testing-infrastructure-research.md` | CREATED | Comprehensive research on containerized testing infrastructure with PostgreSQL for WitchCityRope React migration - TestContainers evaluation, GitHub Actions integration, performance analysis | Technology Researcher Containerized Testing Infrastructure Research | ACTIVE | Keep permanent |
| 2025-09-12 | `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/requirements/dashboard-events-comparison.md` | CREATED | Comprehensive analysis comparing Dashboard and Events Enhancement features between legacy and modern APIs for extraction decision | Dashboard and Events feature analysis | ACTIVE | N/A |
| 2025-09-12 | `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/requirements/safety-system-functional-spec.md` | CREATED | Complete functional specification for Safety incident reporting system based on approved UI design - defines workflows, API endpoints, security requirements | Safety System Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Safety/Entities/SafetyIncident.cs` | CREATED | Safety incident entity with encrypted sensitive data fields, PostgreSQL optimized schema, comprehensive audit trail support | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Safety/Entities/IncidentAuditLog.cs` | CREATED | Audit log entity for complete incident action tracking with JSONB support for PostgreSQL | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Safety/Entities/IncidentNotification.cs` | CREATED | Email notification tracking entity for incident alerts and notifications | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Safety/Models/CreateIncidentRequest.cs` | CREATED | Request model for incident submission with full validation requirements | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Safety/Models/IncidentResponse.cs` | CREATED | Complete response models for incident tracking, admin dashboard, and status reporting | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Safety/Services/IEncryptionService.cs` | CREATED | Encryption service interface for AES-256 sensitive data protection | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Safety/Services/EncryptionService.cs` | CREATED | AES-256 encryption service implementation for protecting sensitive incident data | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Safety/Services/IAuditService.cs` | CREATED | Audit service interface for comprehensive incident action logging | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Safety/Services/AuditService.cs` | CREATED | Audit service implementation with privacy protection for anonymous reports | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Safety/Services/ISafetyService.cs` | CREATED | Main safety service interface following vertical slice architecture pattern | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Safety/Services/SafetyService.cs` | CREATED | Core safety service with direct Entity Framework access, encryption, reference number generation | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Safety/Validation/CreateIncidentValidator.cs` | CREATED | FluentValidation validator for incident submission with comprehensive business rules | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Safety/Endpoints/SafetyEndpoints.cs` | CREATED | Minimal API endpoints for incident submission, tracking, admin dashboard - supports anonymous access | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/WitchCityRope.Api.csproj` | MODIFIED | Added FluentValidation packages for safety system validation requirements | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Shared/Extensions/ServiceCollectionExtensions.cs` | MODIFIED | Registered Safety services and FluentValidation for DI container | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Shared/Extensions/WebApplicationExtensions.cs` | MODIFIED | Added Safety endpoint mapping to application pipeline | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Data/ApplicationDbContext.cs` | MODIFIED | Added Safety entities, comprehensive Entity Framework configuration, audit field handling | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Migrations/20250913030950_AddSafetyIncidentSystem.cs` | CREATED | EF Core migration with PostgreSQL sequence and reference number generation function | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Migrations/20250913030950_AddSafetyIncidentSystem.Designer.cs` | CREATED | Auto-generated migration designer file for Safety System database schema | Safety System Backend Implementation | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/appsettings.Development.json` | MODIFIED | Fixed AES-256 encryption key with proper 256-bit cryptographic key for Safety System | Safety System Encryption Fix | ACTIVE | Keep permanent |
| 2025-09-13 | `/apps/api/Features/Safety/README.md` | CREATED | Comprehensive documentation for Safety System encryption configuration and key management | Safety System Encryption Fix | ACTIVE | Keep permanent |
| 2025-09-12 | `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/design/safety-system-database-design.md` | CREATED | Comprehensive database schema design for Safety incident reporting system with PostgreSQL optimization, encrypted storage, audit trails, and EF Core configuration | Database Designer Safety System | ACTIVE | Keep permanent |
| 2025-09-12 | `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/handoffs/database-designer-2025-09-12-handoff.md` | CREATED | Mandatory database designer handoff documenting completed schema design, security patterns, performance optimization, and next steps for backend implementation | Database Designer Handoff | ACTIVE | Keep permanent |
| 2025-09-12 | `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/design/safety-system-technical-design.md` | CREATED | Comprehensive technical architecture for Safety System implementation - vertical slice structure, service layers, API endpoints, validation, encryption, testing strategy, and implementation roadmap | Backend Developer Safety System Technical Design | ACTIVE | Keep permanent |
| 2025-09-12 | `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/design/checkin-system-ui-design.md` | CREATED | Comprehensive UI/UX design for CheckIn System - mobile-first, offline-capable event attendee management with touch optimization, responsive design, and volunteer-friendly interface | UI Designer CheckIn System Design | ACTIVE | Keep permanent |
| 2025-09-13 | `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/requirements/checkin-system-functional-spec.md` | CREATED | Comprehensive functional specification for CheckIn System based on approved UI design - defines staff workflows, API specifications, business rules, offline capability for event attendee management | Business Requirements Agent CheckIn System | ACTIVE | Keep permanent |
| 2025-09-13 | `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/design/checkin-system-database-design.md` | CREATED | Comprehensive database schema design for CheckIn System with PostgreSQL optimization, offline sync capability, audit trails, Entity Framework configuration for event attendee management | Database Designer Agent CheckIn System | ACTIVE | Keep permanent |
| 2025-09-13 | `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/design/checkin-system-technical-design.md` | CREATED | Comprehensive technical architecture and implementation design for CheckIn System - vertical slice structure, service layers, API endpoints, offline sync, performance optimization, security, testing strategy | Backend Developer Agent CheckIn System Technical Design | ACTIVE | Keep permanent |

## Active File Registry

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-09-12 | /docs/functional-areas/authentication/research/2025-09-12-authentication-best-practices-research.md | CREATED | Comprehensive research on modern authentication patterns for React + ASP.NET Core 2025 - BFF pattern analysis and recommendations | Technology researcher authentication research | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/backend-developer-lessons-learned.md | MODIFIED | Added mandatory agent handoff documentation process section for workflow continuity | Workflow handoff system implementation | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/business-requirements-lessons-learned.md | MODIFIED | Added mandatory agent handoff documentation process section for workflow continuity | Workflow handoff system implementation | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/database-designer-lessons-learned.md | MODIFIED | Added mandatory agent handoff documentation process section for workflow continuity | Workflow handoff system implementation | ACTIVE | Keep permanent |
| 2025-09-13 | /.github/workflows/main-pipeline.yml | CREATED | Comprehensive testing pipeline with strategic container usage - separates unit, integration, and E2E tests | Containerized testing continuation | ACTIVE | Review 2025-10-13 |
| 2025-09-13 | /docs/functional-areas/testing-infrastructure/containerized-testing-summary-2025-09-13.md | CREATED | Implementation summary documenting strategic containerized testing approach and deployment guide | Testing infrastructure documentation | ACTIVE | Keep permanent |
| 2025-09-13 | /docs/standards-processes/testing/TESTING.md | MODIFIED | Added "When to Use Containerized Testing" section with decision matrix | Testing standards enhancement | ACTIVE | Keep permanent |
| 2025-09-13 | /docs/lessons-learned/test-executor-lessons-learned.md | MODIFIED | Added containerized testing infrastructure usage guidance | Agent knowledge update | ACTIVE | Keep permanent |
| 2025-09-13 | /docs/lessons-learned/test-developer-lessons-learned.md | MODIFIED | Added TestContainers vs simple unit tests decision framework | Agent knowledge update | ACTIVE | Keep permanent |
| 2025-09-13 | /.github/workflows/ci.yml | MODIFIED | Separated unit and integration test execution with proper container configuration | CI workflow enhancement | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/functional-spec-lessons-learned.md | MODIFIED | Added mandatory agent handoff documentation process section for workflow continuity | Workflow handoff system implementation | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/git-manager-lessons-learned.md | MODIFIED | Added mandatory agent handoff documentation process section for workflow continuity | Workflow handoff system implementation | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/librarian-lessons-learned.md | MODIFIED | Added mandatory agent handoff documentation process section for workflow continuity | Workflow handoff system implementation | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/react-developer-lessons-learned.md | MODIFIED | Added mandatory agent handoff documentation process section for workflow continuity | Workflow handoff system implementation | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/test-developer-lessons-learned.md | MODIFIED | Added mandatory agent handoff documentation process section for workflow continuity | Workflow handoff system implementation | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/test-executor-lessons-learned.md | MODIFIED | Added mandatory agent handoff documentation process section for workflow continuity | Workflow handoff system implementation | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/ui-designer-lessons-learned.md | MODIFIED | Added mandatory agent handoff documentation process section for workflow continuity | Workflow handoff system implementation | ACTIVE | Keep permanent |
| 2025-09-13 | /docs/lessons-learned/test-developer-lessons-learned.md | MODIFIED | Added comprehensive guidance on when to use TestContainers vs simple unit tests - decision matrix, performance targets, architecture integration with SeedDataService | Test Developer Agent - TestContainers Decision Matrix | ACTIVE | Keep permanent |

## Archived/Completed Files

| Date | File Path | Action | Purpose | Status |
|------|-----------|--------|---------|--------|
| *Coming Soon* | Various archived documents | ARCHIVED | Historical reference | ARCHIVED |

## File Cleanup Guidelines

### When to Clean Up
- **Temporary files**: After session/task completion
- **Draft documents**: When final version is created
- **Duplicate files**: Immediately when identified
- **Outdated research**: When newer versions exist

### When to Keep Permanent
- **Architecture decisions**: Always permanent
- **API specifications**: Always permanent
- **Database schemas**: Always permanent
- **Business requirements**: Always permanent
- **Implementation guides**: Keep unless superseded
- **Lessons learned**: Always permanent

### Regular Maintenance
- Weekly review of temporary files
- Monthly archive of completed work
- Quarterly cleanup of outdated documents
- Annual review of permanent file status