# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
>
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

## Registry Table

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
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
| 2025-01-13 | /docs/functional-areas/deployment/2025-01-13-digitalocean-deployment/handoffs/deployment-complete-2025-01-13-handoff.md | CREATED | Complete deployment handoff with GitHub Actions, Docker, and Cloudflare tunnel configuration | DigitalOcean Deployment Complete | ACTIVE | - |
| 2025-01-13 | /docs/functional-areas/deployment/2025-01-13-digitalocean-deployment/handoffs/paypal-webhook-cloudflare-2025-01-13-handoff.md | CREATED | PayPal webhook integration with Cloudflare tunnel configuration handoff | PayPal Webhook Cloudflare Integration | ACTIVE | - |
| 2025-01-20 | /apps/api/Features/Participation/Models/CreateTicketPurchaseRequest.cs | CREATED | Ticket purchase request model for class events | Backend RSVP Complete Phase | ACTIVE | - |
| 2025-01-20 | /apps/web/src/features/payments/components/PayPalButton.tsx | MODIFIED | Restored functional PayPal integration with sliding scale pricing | PayPal Integration Phase 1 | ACTIVE | - |
| 2025-01-20 | /apps/web/src/lib/api/services/payments.ts | CREATED | Payment API service layer with ticket purchase integration | PayPal Integration Phase 1 | ACTIVE | - |
| 2025-01-20 | /apps/web/src/lib/api/hooks/usePayments.ts | CREATED | React Query payment hooks for API integration | PayPal Integration Phase 1 | ACTIVE | - |
| 2025-09-20 | /apps/web/src/lib/api/services/payments.ts | MODIFIED | Fixed critical import error - changed '../apiClient' to '../client' | Critical React App Fix | ACTIVE | - |
| 2025-09-20 | /tests/playwright/rsvp-ticketing-test.spec.ts | CREATED | Basic RSVP and ticketing implementation tests | RSVP Ticketing Testing | TEMPORARY | 2025-10-01 |
| 2025-09-20 | /tests/playwright/comprehensive-rsvp-ticketing.spec.ts | CREATED | Comprehensive RSVP and ticketing test suite | RSVP Ticketing Testing | TEMPORARY | 2025-10-01 |
| 2025-09-20 | /test-rsvp-api.sh | CREATED | API testing script for RSVP and ticketing endpoints | RSVP Ticketing Testing | TEMPORARY | 2025-09-21 |
| 2025-09-20 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/test-report-2025-09-20.md | CREATED | Comprehensive test report for RSVP and ticketing implementation with results and recommendations | RSVP Ticketing Test Report | ACTIVE | Never |

## Registry Statistics

- **Total Files Tracked**: 51
- **Active Files**: 47
- **Temporary Files**: 4
- **Last Updated**: 2025-09-20

## Cleanup Schedule

- **2025-09-21**: Remove debug logging files and temporary test scripts
- **2025-10-01**: Archive completed session work files
- **2025-10-15**: Review and archive old temporary files

## File Categories

### Backend Implementation
- Participation entities, services, and endpoints
- Authentication model updates
- Database configurations

### Frontend Implementation
- Payment service integration
- React component fixes
- PayPal integration restoration

### Testing & Documentation
- Playwright test suites
- Comprehensive test reports
- API testing scripts

### Standards & Patterns
- React development patterns
- Agent handoff documents
- Lessons learned documentation

---

**Registry Maintenance**: Updated automatically during development sessions. Manual review quarterly.