# Events Management - Test Coverage
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 2.0 -->
<!-- Owner: Events Team -->
<!-- Status: Active -->

## Overview
This document outlines the comprehensive test coverage for the events management system, including unit tests, integration tests, and end-to-end tests.

## Test Coverage Summary

### Overall Coverage
- **Unit Tests**: 87% coverage (Core domain logic)
- **Integration Tests**: 73% coverage (API and database)
- **E2E Tests**: 65% coverage (Critical user flows)
- **Total Coverage**: 78% weighted average

### Coverage by Component
| Component | Unit | Integration | E2E | Total |
|-----------|------|-------------|-----|-------|
| Event Entity | 95% | 85% | N/A | 90% |
| RSVP Logic | 92% | 80% | 70% | 81% |
| Ticket Processing | 88% | 75% | 65% | 76% |
| Check-in Flow | 85% | 70% | 80% | 78% |
| Email Service | 78% | 65% | 50% | 64% |

## Unit Tests

### Core Domain Tests
Location: `/tests/WitchCityRope.Core.Tests/Entities/`

#### Event Entity Tests (`EventTests.cs`)
```csharp
✅ Event_Creation_ValidatesRequiredFields
✅ Event_CannotCreateWithPastStartDate
✅ Event_CannotExceedCapacity
✅ Event_CalculatesAvailableSpotsCorrectly
✅ Event_SocialEventsRequireVetting
✅ Event_ClassesRequirePricing
✅ Event_CanAddMultipleOrganizers
✅ Event_CannotRemoveLastOrganizer
✅ Event_StatusTransitionsCorrectly
⚠️ Event_HandlesTimezoneConversion (flaky)
```

#### RSVP Tests (`RsvpTests.cs`)
```csharp
✅ Rsvp_GeneratesUniqueConfirmationCode
✅ Rsvp_OnlyForSocialEvents
✅ Rsvp_RequiresVettedUser
✅ Rsvp_CannotDoubleRsvp
✅ Rsvp_WaitlistWhenFull
✅ Rsvp_CanCheckIn
✅ Rsvp_CannotCheckInIfCancelled
✅ Rsvp_TracksCheckInTime
```

#### Ticket Tests (`TicketTests.cs`)
```csharp
✅ Ticket_RequiresPayment
✅ Ticket_GeneratesConfirmationCode
✅ Ticket_CalculatesPriceCorrectly
✅ Ticket_HandlesRefunds
✅ Ticket_TracksEmergencyContact
✅ Ticket_ValidatesRequiredFields
❌ Ticket_ProcessesPartialRefunds (not implemented)
```

### Value Object Tests
```csharp
✅ Money_CreatesValidCurrency
✅ Money_ComparesCorrectly
✅ Money_HandlesArithmetic
✅ Location_ValidatesAddress
✅ Location_HandlesOnlineEvents
```

## Integration Tests

### API Integration Tests
Location: `/tests/WitchCityRope.Api.Tests/Controllers/`

#### Events Controller Tests
```csharp
✅ GET /api/events - Returns filtered list
✅ GET /api/events/{id} - Returns event details
✅ POST /api/events - Creates event (admin only)
✅ PUT /api/events/{id} - Updates event
✅ DELETE /api/events/{id} - Soft deletes event
✅ GET /api/events/{id}/attendees - Lists attendees
⚠️ POST /api/events/{id}/cancel - Processes cancellation (intermittent)
```

#### RSVP Endpoints Tests
```csharp
✅ POST /api/events/{id}/rsvp - Creates RSVP
✅ GET /api/rsvps/{code} - Retrieves by code
✅ POST /api/rsvps/{id}/cancel - Cancels RSVP
✅ POST /api/events/{id}/checkin - Checks in RSVP
❌ PUT /api/rsvps/{id} - Update RSVP (not needed)
```

#### Ticket Endpoints Tests
```csharp
✅ POST /api/events/{id}/tickets - Purchase ticket
✅ GET /api/tickets/{code} - Retrieve by code
✅ POST /api/tickets/{id}/refund - Process refund
✅ GET /api/users/{id}/tickets - User's tickets
⚠️ POST /api/tickets/validate-payment - Payment validation (external dependency)
```

### Database Integration Tests
Location: `/tests/WitchCityRope.IntegrationTests/`

```csharp
✅ EventRepository_CreatesAndRetrieves
✅ EventRepository_FiltersCorrectly
✅ EventRepository_IncludesRelatedData
✅ RsvpRepository_HandlesUniqueConstraints
✅ TicketRepository_ProcessesRefunds
⚠️ ConcurrentRegistration_HandlesRaceCondition (timing sensitive)
```

## End-to-End Tests (Playwright)

### Test Files
Location: `/tests/playwright/`

#### Event Creation Flow (`admin/admin-event-creation.spec.ts`)
```typescript
✅ "Admin can access event creation page"
✅ "Can create a class event with all fields"
✅ "Can create a social event with RSVP"
✅ "Validates required fields"
✅ "Can save as draft"
❌ "Can use event template" (feature not implemented)
```

#### Event Display (`events/event-display.spec.ts`)
```typescript
✅ "Shows event list with filters"
✅ "Displays event details correctly"
✅ "Shows correct buttons for event type"
✅ "Handles sold out events"
✅ "Shows waitlist when full"
```

#### Registration Flow (`events/event-registration.spec.ts`)
```typescript
✅ "Member can RSVP for social event"
✅ "Member can purchase class ticket"
✅ "Non-vetted user sees vetting message"
✅ "Handles payment failures gracefully"
⚠️ "Processes waitlist promotion" (requires manual trigger)
```

#### Check-in Flow (`admin/event-checkin.spec.ts`)
```typescript
✅ "Staff can access check-in page"
✅ "Can search attendees by name"
✅ "Can check in via confirmation code"
✅ "Shows attendee details"
✅ "Updates count in real-time"
❌ "Works offline" (PWA not implemented)
```

## Performance Tests

### Load Testing Results
- **Event List API**: 200ms avg response (100 concurrent users)
- **Registration API**: 500ms avg response (50 concurrent)
- **Check-in API**: 150ms avg response (20 concurrent)

### Database Performance
```sql
-- Optimized queries with indexes
✅ Event list with capacity calculation: <50ms
✅ Attendee list with user data: <100ms
✅ RSVP duplicate check: <10ms
✅ Waitlist promotion query: <30ms
```

## Security Tests

### Authorization Tests
```csharp
✅ Only admins can create events
✅ Only organizers can edit their events
✅ Only staff can access check-in
✅ Members can only cancel own registrations
✅ API requires authentication
```

### Input Validation Tests
```csharp
✅ XSS prevention in event descriptions
✅ SQL injection prevention
✅ File upload restrictions
✅ Rate limiting on registration
```

## Test Data Management

### Seed Data
```csharp
// Test accounts (DbInitializer)
- admin@witchcityrope.com (Admin role)
- organizer@witchcityrope.com (Event organizer)
- vetted@witchcityrope.com (Vetted member)
- member@witchcityrope.com (Regular member)

// Test events
- "Introduction to Rope" (Class, upcoming)
- "Community Rope Jam" (Social, requires vetting)
- "Advanced Techniques" (Class, sold out)
- "Monthly Social" (Social, has waitlist)
```

### Test Utilities
Location: `/tests/WitchCityRope.Tests.Common/`

```csharp
- EventBuilder - Fluent API for test events
- RsvpBuilder - Creates test RSVPs
- TicketBuilder - Creates test tickets
- TestDataGenerator - Random test data
```

## Continuous Integration

### GitHub Actions Workflow
```yaml
✅ Unit tests run on every push
✅ Integration tests run on PR
✅ E2E tests run nightly
✅ Performance tests run weekly
⚠️ Security scan monthly (some false positives)
```

### Test Execution Time
- Unit tests: ~2 seconds
- Integration tests: ~45 seconds
- E2E tests: ~3 minutes
- Full suite: ~4 minutes

## Known Test Issues

### Flaky Tests
1. **Timezone conversion tests** - Fail during DST changes
2. **Concurrent registration** - Race conditions in test environment
3. **Payment webhook tests** - External dependency timeouts
4. **Email delivery tests** - SMTP server availability

### Test Debt
1. Missing volunteer management tests
2. Limited email template testing
3. No accessibility testing
4. Incomplete mobile E2E tests

## Test Improvement Plan

### Q1 2025
- [ ] Increase E2E coverage to 80%
- [ ] Add accessibility tests
- [ ] Implement visual regression tests
- [ ] Add contract tests for payment API

### Q2 2025
- [ ] Performance test automation
- [ ] Chaos engineering tests
- [ ] Mobile-specific E2E suite
- [ ] API versioning tests

## Running Tests

### Local Development
```bash
# Unit tests only
dotnet test tests/WitchCityRope.Core.Tests

# Integration tests (requires Docker)
dotnet test tests/WitchCityRope.IntegrationTests

# E2E tests (requires running app)
npm run test:e2e:playwright

# Specific test file
npx playwright test events/event-creation.spec.ts

# With UI debugging
npx playwright test --ui
```

### CI Environment
```bash
# Full test suite
./scripts/run-all-tests.sh

# Coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Performance tests
k6 run tests/performance/events-load-test.js
```

---

*For test writing guidelines, see [/docs/standards-processes/testing/README.md](/docs/standards-processes/testing/README.md)*