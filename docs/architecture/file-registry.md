# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
>
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

## Registry Table

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-09-20 | `/session-work/2025-09-20/webapp-api-connectivity-fix-report.md` | CREATED | Comprehensive report documenting webapp-API connectivity issue root cause and resolution | Test Executor - Connectivity Issue Resolution | ACTIVE | 2025-10-20 |
| 2025-09-20 | `/apps/web/src/pages/checkout/CheckoutPage.tsx` | CREATED | Full page checkout component replacing CheckoutModal with responsive grid layout | Checkout Redesign - Modal to Page | ACTIVE | Never |
| 2025-09-20 | `/apps/web/src/components/checkout/VenmoButton.tsx` | CREATED | Branded Venmo payment button component with proper styling | Checkout Redesign - Branded Buttons | ACTIVE | Never |
| 2025-09-20 | `/apps/web/src/routes/router.tsx` | MODIFIED | Added `/checkout/:eventId` route for new checkout page | Checkout Redesign - Routing | ACTIVE | Never |
| 2025-09-20 | `/apps/web/src/components/events/ParticipationCard.tsx` | MODIFIED | Updated to use React Router navigation instead of modal | Checkout Redesign - Navigation | ACTIVE | Never |
| 2025-09-20 | `/apps/web/src/components/checkout/CheckoutForm.tsx` | MODIFIED | Updated to use branded PayPal and Venmo buttons | Checkout Redesign - Branded Buttons | ACTIVE | Never |
| 2025-09-20 | `/apps/web/src/components/checkout/PaymentMethodSelector.tsx` | MODIFIED | Added branded icons for PayPal and Venmo payment methods | Checkout Redesign - Branded Buttons | ACTIVE | Never |
| 2025-09-20 | `/apps/web/src/features/payments/components/PayPalButton.tsx` | MODIFIED | Enhanced PayPal button styling for better branding | Checkout Redesign - Branded Buttons | ACTIVE | Never |
| 2025-09-20 | `/apps/web/src/components/checkout/CheckoutModal.tsx` | DELETED | Removed modal-based checkout in favor of full page | Checkout Redesign - Modal to Page | ARCHIVED | Never |
| 2025-09-20 | `/apps/web/src/components/checkout/index.ts` | MODIFIED | Updated exports to remove CheckoutModal and add VenmoButton | Checkout Redesign - Clean up exports | ACTIVE | Never |
| 2025-09-20 | `/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/implementation-complete-2025-01-20.md` | CREATED | Comprehensive implementation summary for RSVP and ticketing system completion | RSVP/Ticketing Documentation | ACTIVE | Never |
| 2025-09-20 | `/docs/architecture/functional-area-master-index.md` | MODIFIED | Added RSVP/Ticketing functional area entry | RSVP/Ticketing Documentation | ACTIVE | Never |
| 2025-09-20 | `/PROGRESS.md` | MODIFIED | Updated current status to reflect RSVP/ticketing completion | RSVP/Ticketing Documentation | ACTIVE | Never |
| 2025-09-20 | `/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/_archive/rsvp-button-fix-summary.md` | MOVED | Archived temporary implementation fix documentation | RSVP/Ticketing Documentation | ARCHIVED | Never |
| 2025-09-20 | `/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/_archive/terminology-fix-verification.md` | MOVED | Archived temporary terminology fix documentation | RSVP/Ticketing Documentation | ARCHIVED | Never |
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
| 2025-09-20 | `/apps/web/src/pages/events/EventDetailPage.tsx` | MODIFIED | Fixed admin role check - removed array check for single role string | React UI Fixes - Admin Edit Link | ACTIVE | - |
| 2025-09-20 | `/apps/web/src/pages/events/EventsListPage.tsx` | MODIFIED | Fixed double dollar sign in event prices by removing extra $ before template literal | React UI Fixes - Double Dollar Sign | ACTIVE | - |
| 2025-09-20 | `/apps/web/src/components/homepage/EventCard.tsx` | MODIFIED | Updated to use EventDto instead of Event type for proper RSVP/ticket count display | React UI Fixes - Event Card RSVP/Ticket Counts | ACTIVE | - |
| 2025-09-20 | `/apps/web/src/components/homepage/EventsList.tsx` | MODIFIED | Updated to use EventDto instead of Event type for API compatibility | React UI Fixes - Event Card RSVP/Ticket Counts | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Entities/ParticipationStatus.cs | CREATED | Enum for participation status (Active/Cancelled/etc) | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Entities/Participation.cs | CREATED | Main participation entity linking users to events with type/status | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Queries/GetUserParticipationsQuery.cs | CREATED | Query to retrieve user's event participations | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Commands/CreateParticipationCommand.cs | CREATED | Command to create new event participation (RSVP/ticket) | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Commands/CancelParticipationCommand.cs | CREATED | Command to cancel existing participation | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Services/ParticipationService.cs | CREATED | Core business logic for participation management | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Participation/Controllers/ParticipationController.cs | CREATED | REST API endpoints for participation operations | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Migrations/AddParticipationTables.cs | CREATED | EF Core migration to add participation tables to database | Backend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/web/src/features/participation/types/index.ts | CREATED | TypeScript types for participation system | Frontend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/web/src/features/participation/api/participationApi.ts | CREATED | API client for participation endpoints | Frontend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/web/src/features/participation/hooks/useParticipation.ts | CREATED | React hooks for participation state management | Frontend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/web/src/components/events/ParticipationButton.tsx | CREATED | Smart button component for RSVP/ticket actions | Frontend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/web/src/components/dashboard/ParticipationsList.tsx | CREATED | Dashboard component showing user's participations | Frontend RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /tests/playwright/participation-flow.spec.ts | CREATED | E2E tests for complete participation user journey | Testing RSVP Vertical Slice | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Events/Queries/GetEventWithCapacityQuery.cs | MODIFIED | Added capacity checking logic for RSVP/ticket limits | Backend RSVP Integration | ACTIVE | - |
| 2025-01-19 | /apps/api/Features/Events/Models/EventDto.cs | MODIFIED | Added participation-related fields to event responses | Backend RSVP Integration | ACTIVE | - |
| 2025-01-19 | /apps/web/src/pages/events/EventDetailPage.tsx | MODIFIED | Integrated ParticipationButton into event details | Frontend RSVP Integration | ACTIVE | - |
| 2025-01-19 | /apps/web/src/pages/dashboard/DashboardPage.tsx | MODIFIED | Added ParticipationsList to dashboard | Frontend RSVP Integration | ACTIVE | - |
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/requirements/business-requirements.md | CREATED | Complete business requirements for RSVP and ticketing system | RSVP/Ticketing Requirements | ACTIVE | Never |
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/requirements/technical-requirements.md | CREATED | Technical specifications and API contracts for RSVP/ticketing | RSVP/Ticketing Requirements | ACTIVE | Never |
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/design/system-architecture.md | CREATED | System architecture and data flow for participation system | RSVP/Ticketing Design | ACTIVE | Never |
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/implementation/backend-implementation.md | CREATED | Backend implementation guide and patterns | RSVP/Ticketing Implementation | ACTIVE | Never |
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/implementation/frontend-implementation.md | CREATED | Frontend implementation guide and patterns | RSVP/Ticketing Implementation | ACTIVE | Never |
| 2025-01-19 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/testing/test-plan.md | CREATED | Comprehensive test plan for RSVP/ticketing features | RSVP/Ticketing Testing | ACTIVE | Never |

## Archive Log

Files that have been deleted or moved to archive:

| Date | Original Path | Action | Reason | Archive Location |
|------|---------------|--------|--------|------------------|
| 2025-09-20 | `/apps/web/src/components/checkout/CheckoutModal.tsx` | DELETED | Replaced by full page checkout design | N/A |
| 2025-09-20 | Various temporary fix files | MOVED | Consolidated into final implementation | `/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/_archive/` |

## Cleanup Schedule

| Cleanup Date | Files to Review | Action Required |
|--------------|------------------|-----------------|
| 2025-09-21 | Debug logging in ParticipationCard.tsx and EventDetailPage.tsx | Remove debug statements |
| 2025-10-01 | `/terminology-fix-verification.md` | Archive or integrate into main docs |
| 2025-10-20 | `/session-work/2025-09-20/webapp-api-connectivity-fix-report.md` | Archive to appropriate functional area |

---

## Registry Maintenance Notes

- **Weekly Review**: Every Friday, review TEMPORARY status files
- **Monthly Cleanup**: Archive outdated session work and temporary files
- **Status Updates**: Update STATUS when files become obsolete or are superseded
- **Path Changes**: Log when files are moved between directories

Last Updated: 2025-09-20