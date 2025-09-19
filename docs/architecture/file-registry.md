# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
>
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

## Registry Table

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-01-13 | /docs/functional-areas/events/research/2025-01-13-entity-id-generation-patterns-research.md | CREATED | Comprehensive research on React + .NET API entity ID generation patterns for temporary entities in forms | Technology research for entity ID patterns | ACTIVE | Never |
| 2025-01-19 | /apps/api/Features/Events/Models/UpdateEventRequest.cs | MODIFIED | Added VolunteerPositions property to enable volunteer position persistence | Events admin persistence fixes | ACTIVE | N/A |
| 2025-01-19 | /apps/api/Features/Events/Services/EventService.cs | MODIFIED | Implemented UpdateEventVolunteerPositionsAsync and fixed ticket type ID handling | Events admin persistence fixes | ACTIVE | N/A |
| 2025-01-19 | /apps/web/src/utils/eventDataTransformation.ts | MODIFIED | Fixed volunteer position field mapping to match API DTOs | Events admin persistence fixes | ACTIVE | N/A |
| 2025-09-19 | /docs/lessons-learned/events-persistence-debugging-2025-09-19.md | CREATED | Comprehensive documentation of debugging session misdiagnoses and root causes | Events persistence debugging documentation | ACTIVE | Never |
| 2025-09-19 | /docs/lessons-learned/orchestrator-lessons-learned.md | MODIFIED | Added critical pattern about not assuming infrastructure issues when code fails | Events persistence debugging documentation | ACTIVE | N/A |
| 2025-09-19 | /docs/lessons-learned/backend-developer-lessons-learned.md | MODIFIED | Added ultra critical section on Entity Framework navigation property requirements | Events persistence debugging documentation | ACTIVE | N/A |
| 2025-09-19 | /tests/playwright/persistence-validation.spec.ts | CREATED | Comprehensive E2E test for persistence fix validation | Persistence testing after backend fixes | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/basic-app-check.spec.ts | CREATED | Basic React app functionality verification test | Persistence testing after backend fixes | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/api-persistence-test.spec.ts | CREATED | Direct API testing for persistence validation | Persistence testing after backend fixes | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/working-persistence-test.spec.ts | CREATED | Focused test for working persistence components | Persistence testing after backend fixes | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /test-results/persistence-fix-validation-report.md | CREATED | Comprehensive report on persistence testing results | Persistence testing after backend fixes | ACTIVE | - |
| 2025-09-19 | /tests/playwright/events-admin-add-buttons-verification.spec.ts | CREATED | Comprehensive E2E test for verifying Add buttons fixes | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/focused-add-buttons-test.spec.ts | CREATED | Focused test for Add buttons with error monitoring | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/complete-add-buttons-verification.spec.ts | CREATED | Complete verification test for all three Add buttons | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/volunteers-tab-test.spec.ts | CREATED | Specific test for Volunteers tab and Add New Position button | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/test-add-new-position-button.spec.ts | CREATED | Final verification test for Add New Position modal functionality | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /test-results/events-admin-add-buttons-verification-report.md | CREATED | Comprehensive verification report documenting all Add buttons fixes are working | Events admin Add buttons verification | ACTIVE | - |
| 2025-09-19 | /apps/web/src/components/events/SessionFormModal.tsx | MODIFIED | Fixed undefined property errors in Add Session modal | Events admin page bug fixes | ACTIVE | N/A |
| 2025-09-19 | /apps/web/src/components/events/TicketTypeFormModal.tsx | MODIFIED | Fixed undefined property errors in Add Ticket Type modal | Events admin page bug fixes | ACTIVE | N/A |
| 2025-09-19 | /apps/web/src/components/events/VolunteerPositionFormModal.tsx | MODIFIED | Fixed undefined property errors in Add Volunteer Position modal | Events admin page bug fixes | ACTIVE | N/A |
| 2025-09-19 | /apps/web/src/components/events/EventForm.tsx | MODIFIED | Added safety checks for undefined arrays in modal props | Events admin page bug fixes | ACTIVE | N/A |
| 2025-08-17 | /docs/guides-setup/docker-operations-guide.md | CREATED | Comprehensive guide for Docker container operations | Docker development workflow | ACTIVE | N/A |
| 2025-08-17 | /scripts/docker-dev.sh | CREATED | Development environment startup script with health checks | Docker development workflow | ACTIVE | N/A |
| 2025-08-17 | /scripts/docker-stop.sh | CREATED | Environment cleanup and shutdown script | Docker development workflow | ACTIVE | N/A |
| 2025-08-17 | /scripts/docker-logs.sh | CREATED | Log viewer with filtering capabilities | Docker development workflow | ACTIVE | N/A |
| 2025-08-17 | /scripts/docker-health.sh | CREATED | Comprehensive health check validation | Docker development workflow | ACTIVE | N/A |
| 2025-08-17 | /scripts/docker-rebuild.sh | CREATED | Service rebuilding utility | Docker development workflow | ACTIVE | N/A |
| 2025-08-17 | /scripts/docker-clean.sh | CREATED | Container and volume cleanup utility | Docker development workflow | ACTIVE | N/A |
| 2025-08-17 | /docker-compose.dev.yml | CREATED | Development environment Docker Compose override | Docker development configuration | ACTIVE | N/A |
| 2025-08-17 | /docker-compose.test.yml | CREATED | Testing environment Docker Compose override | Docker testing configuration | ACTIVE | N/A |
| 2025-08-17 | /docker-compose.prod.yml | CREATED | Production environment Docker Compose override | Docker production configuration | ACTIVE | N/A |
| 2025-08-17 | /nginx/nginx.conf | CREATED | Nginx reverse proxy configuration for production | Production deployment | ACTIVE | N/A |
| 2025-08-17 | /nginx/ssl.conf | CREATED | SSL/TLS configuration for Nginx | Production security | ACTIVE | N/A |
| 2025-08-17 | /.dockerignore | CREATED | Docker build context exclusions | Docker optimization | ACTIVE | N/A |
| 2025-08-17 | /apps/web/Dockerfile.dev | CREATED | Development-optimized React container | React development | ACTIVE | N/A |
| 2025-08-17 | /apps/web/Dockerfile.prod | CREATED | Production-optimized React container | React production | ACTIVE | N/A |
| 2025-08-17 | /apps/api/Dockerfile.dev | CREATED | Development-optimized API container | API development | ACTIVE | N/A |
| 2025-08-17 | /apps/api/Dockerfile.prod | CREATED | Production-optimized API container | API production | ACTIVE | N/A |
| 2025-08-15 | /docs/architecture/functional-area-master-index.md | CREATED | Master index for navigating all functional areas and documentation | Documentation organization | ACTIVE | N/A |
| 2025-08-15 | /docs/standards-processes/documentation-organization-standard.md | CREATED | Standards for organizing documentation by functional areas | Documentation standards | ACTIVE | N/A |
| 2025-08-15 | /docs/architecture/docs-structure-validator.sh | CREATED | Script to validate documentation structure compliance | Documentation validation | ACTIVE | N/A |
| 2025-08-14 | /docs/functional-areas/events/requirements/business-requirements.md | CREATED | Complete business requirements for events functionality | Events feature development | ACTIVE | N/A |
| 2025-08-14 | /docs/functional-areas/events/components/event-management-components.md | CREATED | Detailed component specifications for event management | Events UI development | ACTIVE | N/A |
| 2025-08-14 | /docs/functional-areas/events/api/events-api-specification.md | CREATED | Complete API specification for events functionality | Events API development | ACTIVE | N/A |
| 2025-08-13 | /docs/functional-areas/testing/testing-strategy.md | CREATED | Comprehensive testing strategy and standards | Testing infrastructure | ACTIVE | N/A |
| 2025-08-13 | /docs/functional-areas/testing/playwright-test-patterns.md | CREATED | Playwright testing patterns and best practices | E2E testing | ACTIVE | N/A |
| 2025-08-13 | /docs/lessons-learned/test-executor-lessons-learned.md | CREATED | Test execution lessons learned and patterns | Testing knowledge base | ACTIVE | N/A |
| 2025-08-12 | /docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md | CREATED | Strategy for aligning TypeScript interfaces with C# DTOs | React migration | ACTIVE | N/A |
| 2025-08-12 | /packages/shared-types/src/index.ts | CREATED | Shared TypeScript types generated from API | Type safety | ACTIVE | N/A |
| 2025-08-12 | /packages/shared-types/package.json | CREATED | Shared types package configuration | Type management | ACTIVE | N/A |
| 2025-08-11 | ARCHITECTURE.md | MODIFIED | Updated with React migration information and microservices pattern | Architecture documentation | ACTIVE | N/A |
| 2025-08-11 | DOCKER_DEV_GUIDE.md | CREATED | Comprehensive Docker development guide | Docker development | ACTIVE | N/A |
| 2025-08-11 | DOCKER_ONLY_DEVELOPMENT.md | CREATED | Documentation for Docker-only development requirement | Development standards | ACTIVE | N/A |
| 2025-08-10 | /apps/web/src/main.tsx | CREATED | React application entry point | React application | ACTIVE | N/A |
| 2025-08-10 | /apps/web/src/App.tsx | CREATED | Main React application component | React application | ACTIVE | N/A |
| 2025-08-10 | /apps/web/vite.config.ts | CREATED | Vite configuration for React development | React tooling | ACTIVE | N/A |
| 2025-08-10 | /apps/web/package.json | CREATED | React application dependencies and scripts | React configuration | ACTIVE | N/A |
| 2025-08-09 | /apps/api/Program.cs | CREATED | ASP.NET Core Minimal API entry point | API application | ACTIVE | N/A |
| 2025-08-09 | /apps/api/appsettings.json | CREATED | API configuration settings | API configuration | ACTIVE | N/A |
| 2025-08-09 | /apps/api/WitchCityRope.Api.csproj | CREATED | API project file and dependencies | API project | ACTIVE | N/A |

## Cleanup Schedule

### 2025-09-26 (Week from 2025-09-19)
- Review and clean up temporary test files created for persistence validation
- Migrate valuable test patterns to permanent test suite
- Delete temporary E2E test files if no longer needed

### 2025-08-24 (Week from 2025-08-17)
- Review Docker helper scripts for production readiness
- Clean up any temporary Docker configurations
- Evaluate container optimization opportunities

### Monthly Reviews
- Review all TEMPORARY files for relevance
- Update STATUS as needed (ACTIVE, ARCHIVED, DEPRECATED)
- Clean up orphaned documentation
- Validate all ACTIVE files are still in use

## Status Definitions

- **ACTIVE**: File is currently in use and should be maintained
- **TEMPORARY**: File created for specific task, scheduled for cleanup
- **ARCHIVED**: File moved to archive, kept for historical reference
- **DEPRECATED**: File no longer used but kept for transition period