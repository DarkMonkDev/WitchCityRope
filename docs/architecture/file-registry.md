# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
>
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

## Registry Table

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/design/role-stacking-research.md | CREATED | Comprehensive research on role/status stacking patterns for user authentication with decision matrix and recommended hybrid claims-based approach | Technology Research - Role Stacking System | ACTIVE | Never |
| 2025-09-20 | /apps/web/src/components/dashboard/UserParticipations.tsx | MODIFIED | Fixed Mantine Empty import error - removed non-existent component | React compilation fix | ACTIVE | - |
| 2025-09-20 | /apps/web/src/components/events/ParticipationCard.tsx | MODIFIED | Fixed TypeScript unknown type errors with proper type guards | React compilation fix | ACTIVE | - |
| 2025-09-20 | /apps/web/src/lib/api/hooks/useEvents.ts | MODIFIED | Fixed EventSessionDto transformation missing required fields | React compilation fix | ACTIVE | - |
| 2025-09-20 | /apps/web/src/utils/eventFieldMapping.ts | MODIFIED | Fixed EventDto import path to use local types instead of shared-types | React compilation fix | ACTIVE | - |
| 2025-09-20 | /terminology-fix-verification.md | CREATED | Document all terminology fixes and ParticipationCard bug fix | Critical terminology fixes | ACTIVE | 2025-10-01 |
| 2025-09-20 | /apps/web/src/components/events/ParticipationCard.tsx | MODIFIED | Fixed "Register for Class" → "Purchase Ticket" and added Admin role | Critical terminology fixes | ACTIVE | - |
| 2025-09-20 | /apps/web/src/components/events/public/EventCard.tsx | MODIFIED | Fixed multiple "register" instances to appropriate alternatives | Critical terminology fixes | ACTIVE | - |
| 2025-09-20 | /apps/web/src/pages/admin/AdminEventDetailsPage.tsx | MODIFIED | Fixed publish/unpublish modal terminology | Critical terminology fixes | ACTIVE | - |
| 2025-09-20 | /apps/web/src/pages/dashboard/RegistrationsPage.tsx | MODIFIED | Fixed empty state and date label terminology | Critical terminology fixes | ACTIVE | - |
| 2025-09-20 | /apps/api/Features/Authentication/Models/AuthUserResponse.cs | MODIFIED | Added missing IsVetted and IsActive properties for RSVP button fix | RSVP button debug - admin user access | ACTIVE | - |
| 2025-09-20 | /apps/api/Models/Auth/UserDto.cs | MODIFIED | Added missing Role, Roles, IsVetted, IsActive properties for API consistency | RSVP button debug - admin user access | ACTIVE | - |
| 2025-09-20 | /apps/web/src/components/events/ParticipationCard.tsx | MODIFIED | Added comprehensive debug logging for vetting logic troubleshooting | RSVP button debug - admin user access | TEMPORARY | 2025-09-21 |
| 2025-09-20 | /apps/web/src/pages/events/EventDetailPage.tsx | MODIFIED | Added debug logging for event type determination | RSVP button debug - admin user access | TEMPORARY | 2025-09-21 |
| 2025-09-20 | /apps/web/src/components/events/ParticipationCard.tsx | MODIFIED | Fixed RSVP button visibility bug - handle null participation state during loading | RSVP button visibility fix | ACTIVE | - |
| 2025-09-20 | /docs/lessons-learned/react-developer-lessons-learned.md | MODIFIED | Added critical lesson for RSVP button visibility fix with null state handling | React Developer Lessons | ACTIVE | - |
| 2025-09-20 | /docs/standards-processes/development-standards/react-patterns.md | CREATED | Standard React patterns document with API-dependent button visibility pattern | React Development Standards | ACTIVE | Never |
| 2025-09-20 | /apps/web/src/pages/dashboard/MembershipPage.tsx | MODIFIED | Fixed membership feature descriptions | Critical terminology fixes | ACTIVE | - |
| 2025-09-20 | /apps/web/src/pages/dashboard/EventsPage.tsx | MODIFIED | Fixed note text and empty state messaging | Critical terminology fixes | ACTIVE | - |
| 2025-09-20 | /apps/web/src/components/dashboard/RegistrationHistory.tsx | MODIFIED | Fixed empty state and date label terminology | Critical terminology fixes | ACTIVE | - |
| 2025-09-20 | /apps/web/src/features/payments/components/PaymentConfirmation.tsx | MODIFIED | Fixed call-to-action button text | Critical terminology fixes | ACTIVE | - |
| 2025-09-20 | /apps/web/src/components/profile/ProfileForm.tsx | MODIFIED | Fixed notification description text | Critical terminology fixes | ACTIVE | - |
| 2025-09-20 | /apps/web/src/features/dashboard/components/MembershipStatistics.tsx | MODIFIED | Fixed statistics labels and encouragement text | Critical terminology fixes | ACTIVE | - |
| 2025-09-20 | /apps/web/src/features/dashboard/components/UpcomingEvents.tsx | MODIFIED | Fixed empty state messaging | Critical terminology fixes | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Entities/ParticipationType.cs | CREATED | Enum for RSVP vs Ticket participation types | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Entities/ParticipationStatus.cs | CREATED | Enum for participation status (Active/Cancelled/etc) | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Entities/EventParticipation.cs | CREATED | Core entity for tracking RSVPs and tickets | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Entities/ParticipationHistory.cs | CREATED | Audit trail entity for participation changes | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Data/EventParticipationConfiguration.cs | CREATED | EF Core configuration for EventParticipation | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Data/ParticipationHistoryConfiguration.cs | CREATED | EF Core configuration for ParticipationHistory | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Models/ParticipationStatusDto.cs | CREATED | DTO for user participation status (NSwag) | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Models/CreateRSVPRequest.cs | CREATED | Request DTO for creating RSVPs | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Models/UserParticipationDto.cs | CREATED | DTO for user's participation list (NSwag) | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Services/IParticipationService.cs | CREATED | Service interface for participation management | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Services/ParticipationService.cs | CREATED | Service implementation with business logic | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs | CREATED | Minimal API endpoints for RSVP functionality | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /tests/unit/api/Features/Participation/ParticipationServiceTests.cs | CREATED | Integration tests for ParticipationService | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/handoffs/backend-vertical-slice-2025-01-19-handoff.md | CREATED | Handoff document for completed backend work | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Data/ApplicationDbContext.cs | MODIFIED | Added DbSets and configurations for participation entities | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Shared/Extensions/ServiceCollectionExtensions.cs | MODIFIED | Added ParticipationService registration | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Shared/Extensions/WebApplicationExtensions.cs | MODIFIED | Added ParticipationEndpoints registration | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/design/database-design.md | CREATED | Comprehensive database schema design for RSVP and Ticketing System with PostgreSQL optimizations, Entity Framework patterns, and performance considerations | RSVP/Ticketing Database Design | ACTIVE | Never |
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/design/ef-core-models.md | CREATED | Complete Entity Framework Core models implementation with 4 entities, configurations, and PostgreSQL-specific patterns for RSVP and Ticketing System | RSVP/Ticketing Database Design | ACTIVE | Never |
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/handoffs/database-design-2025-01-19-handoff.md | CREATED | Database design phase completion handoff with implementation guidance, migration scripts, and critical patterns for backend developer | RSVP/Ticketing Database Design | ACTIVE | Never |
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/requirements/functional-specification.md | CREATED | Comprehensive functional specification for RSVP and Ticketing System with API contracts, data models, business logic, security, and performance requirements | RSVP/Ticketing Functional Specification Creation | ACTIVE | Never |
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/handoffs/functional-spec-2025-01-19-handoff.md | CREATED | Functional specification handoff document with implementation guidance, architecture compliance, and developer coordination | RSVP/Ticketing Functional Specification Creation | ACTIVE | Never |
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/design/sendgrid-implementation-research.md | CREATED | Comprehensive SendGrid implementation research for RSVP and ticketing email confirmations, including development safety, production deployment, and Docker integration | SendGrid Email Implementation Research | ACTIVE | Never |
| 2025-09-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/design/wireframe-analysis.md | CREATED | Comprehensive analysis of existing wireframes against approved business requirements for RSVP and Ticketing System | RSVP/Ticketing UI Design Phase | ACTIVE | N/A |
| 2025-09-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/design/ui-specifications.md | CREATED | Complete UI specifications including component hierarchy, user flows, Mantine v7 implementation, and accessibility requirements | RSVP/Ticketing UI Design Phase | ACTIVE | N/A |
| 2025-09-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/handoffs/ui-design-2025-01-19-handoff.md | CREATED | UI Design phase completion handoff with critical design decisions, component specifications, and implementation requirements for React development | RSVP/Ticketing UI Design Phase | ACTIVE | N/A |
| 2025-09-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/requirements/business-requirements.md | MODIFIED | Final stakeholder approvals applied: RSVP simplification, social event clarification, canceled tracking, role stacking | RSVP/Ticketing Final Requirements Approval | ACTIVE | N/A |
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/reviews/phase2-design-review.md | CREATED | Comprehensive Phase 2 Design & Architecture completion review documenting all deliverables, quality gate achievement (93%), and readiness for Phase 3 implementation | Phase 2 Design Completion Review | ACTIVE | Never |
| 2025-09-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/handoffs/business-requirements-2025-01-19-handoff.md | MODIFIED | Final handoff with approved requirements: single RSVP, optional tickets, role stacking, banned user enforcement | RSVP/Ticketing Final Requirements Approval | ACTIVE | N/A |
| 2025-09-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/reviews/phase1-requirements-review.md | CREATED | Comprehensive Phase 1 Requirements Review document summarizing completed business requirements and awaiting stakeholder approval | Phase 1 Requirements Review creation | ACTIVE | Never |
| 2025-09-19 | /docker-compose.yml | MODIFIED | Added BuildKit inline cache args to service build configs | Docker BuildKit migration | ACTIVE | N/A |
| 2025-09-19 | /docker-compose.dev.yml | MODIFIED | Added BuildKit configuration and updated to modern docker compose commands | Docker BuildKit migration | ACTIVE | N/A |
| 2025-09-19 | /dev.sh | MODIFIED | Smart detection of docker-compose vs docker compose, optional BuildKit support | Docker BuildKit migration | ACTIVE | N/A |
| 2025-09-19 | /apps/web/src/lib/api/hooks/useEvents.ts | MODIFIED | Removed excessive debug console.log statements causing memory retention | Memory leak fix - debug logging cleanup | ACTIVE | N/A |
| 2025-09-19 | /apps/web/src/pages/admin/AdminEventDetailsPage.tsx | MODIFIED | Removed debug logging causing component re-render memory leaks | Memory leak fix - debug logging cleanup | ACTIVE | N/A |
| 2025-09-19 | /apps/web/src/utils/eventDataTransformation.ts | MODIFIED | Eliminated debug logs holding references to large transformation objects | Memory leak fix - debug logging cleanup | ACTIVE | N/A |
| 2025-09-19 | /apps/web/src/components/events/EventForm.tsx | MODIFIED | Removed memory leak causing console.log statements in form handling | Memory leak fix - debug logging cleanup | ACTIVE | N/A |
| 2025-09-19 | /apps/web/src/lib/api/queryClient.ts | MODIFIED | Fixed memory monitoring thresholds to appropriate levels for small webapp | Memory leak fix - debug logging cleanup | ACTIVE | N/A |
| 2025-09-19 | /home/chad/repos/witchcityrope-react/PROGRESS.md | MODIFIED | Updated with Events Admin Memory Leak Fix completion documentation | End-of-work cleanup and documentation | ACTIVE | N/A |
| 2025-09-19 | /home/chad/repos/witchcityrope-react/docs/architecture/file-registry.md | MODIFIED | Updated registry with memory leak fix documentation | End-of-work cleanup and documentation | ACTIVE | N/A |
| 2025-09-20 | /apps/web/src/types/participation.types.ts | CREATED | TypeScript types for RSVP functionality (DTOs, enums, component props) | Frontend RSVP Vertical Slice | ACTIVE | N/A |
| 2025-09-20 | /apps/web/src/hooks/useParticipation.ts | CREATED | React Query hooks for participation API calls with mock data fallbacks | Frontend RSVP Vertical Slice | ACTIVE | N/A |
| 2025-09-20 | /apps/web/src/components/events/ParticipationCard.tsx | CREATED | Main RSVP/ticket component for event detail pages with role validation | Frontend RSVP Vertical Slice | ACTIVE | N/A |
| 2025-09-20 | /apps/web/src/components/dashboard/UserParticipations.tsx | CREATED | Dashboard component showing user's RSVPs and tickets | Frontend RSVP Vertical Slice | ACTIVE | N/A |
| 2025-09-20 | /apps/web/src/pages/events/EventDetailPage.tsx | MODIFIED | Integrated ParticipationCard, added RSVP hooks, replaced old RegistrationCard | Frontend RSVP Vertical Slice | ACTIVE | N/A |
| 2025-09-20 | /apps/web/src/pages/dashboard/DashboardPage.tsx | MODIFIED | Added UserParticipations component to dashboard layout | Frontend RSVP Vertical Slice | ACTIVE | N/A |
| 2025-09-20 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/handoffs/frontend-vertical-slice-2025-01-19-handoff.md | CREATED | Comprehensive handoff document for completed frontend RSVP implementation | Frontend RSVP Vertical Slice | ACTIVE | Never |
| 2025-09-19 | /docs/functional-areas/events/handoffs/librarian-2025-09-19-memory-leak-fix-completion.md | CREATED | Comprehensive handoff document summarizing memory leak fix completion | End-of-work cleanup and documentation | ACTIVE | Never |
| 2025-09-19 | /docs/lessons-learned/librarian-lessons-learned.md | MODIFIED | Added end-of-work cleanup excellence pattern for systematic documentation updates | End-of-work cleanup and documentation | ACTIVE | N/A |
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
| 2025-09-19 | /docs/lessons-learned/backend-developer-lessons-learned.md | MODIFIED | Added ULTRA CRITICAL Entity Framework ID generation pattern - never initialize IDs in models | Critical EF pattern to prevent UPDATE instead of INSERT errors | ACTIVE | Never |
| 2025-09-19 | /docs/lessons-learned/database-designer-lessons-learned.md | MODIFIED | Added ULTRA CRITICAL Entity Framework entity model ID pattern guidance | Critical database design pattern to prevent concurrency exceptions | ACTIVE | Never |
| 2025-09-19 | /tests/playwright/working-persistence-test.spec.ts | CREATED | Focused test for working persistence components | Persistence testing after backend fixes | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /test-results/persistence-fix-validation-report.md | CREATED | Comprehensive report on persistence testing results | Persistence testing after backend fixes | ACTIVE | - |
| 2025-09-19 | /tests/playwright/events-admin-add-buttons-verification.spec.ts | CREATED | Comprehensive E2E test for verifying Add buttons fixes | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/focused-add-buttons-test.spec.ts | CREATED | Focused test for Add buttons with error monitoring | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/complete-add-buttons-verification.spec.ts | CREATED | Complete verification test for all three Add buttons | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/volunteers-tab-test.spec.ts | CREATED | Specific test for Volunteers tab and Add New Position button | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/test-add-new-position-button.spec.ts | CREATED | Final verification test for Add New Position modal functionality | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-20 | /apps/web/src/components/events/ParticipationCard.tsx | MODIFIED | URGENT FIX: Updated user role checking to handle backend structure (isVetted boolean + role string) instead of roles array | Critical RSVP button visibility fix for admin users | ACTIVE | N/A |

## Cleanup Schedule

### Weekly Review (Sundays)
- Review TEMPORARY files approaching cleanup dates
- Archive completed session work
- Update status of documentation files
- Remove outdated test files

### Monthly Archive (1st of month)
- Move completed documentation to appropriate archives
- Clean up old test results
- Review ACTIVE file relevance
- Update cleanup dates as needed

## Status Definitions

- **ACTIVE**: Permanent files in active use
- **TEMPORARY**: Files scheduled for cleanup
- **ARCHIVED**: Moved to archive locations
- **DELETED**: Removed from project