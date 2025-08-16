# Vertical Slice: Home Page with Events Display

## Current Status
**Ready to Begin** - All infrastructure and agents configured

## Purpose
Test the complete workflow process and technology stack by implementing a minimal home page that displays events from the API. This will validate:
1. The full 5-phase workflow process
2. All sub-agent coordination
3. API ↔ React communication
4. Quality gates (linting, formatting)
5. Testing infrastructure

## Scope Definition

### What We're Building
A simple home page that:
- Fetches a list of upcoming events from the API
- Displays them in a card layout
- Shows loading and error states
- Works on mobile and desktop

### Technical Components
1. **Backend**: GET /api/events endpoint returning mock data
2. **Frontend**: HomePage component with event cards
3. **Integration**: Axios + TanStack Query for data fetching
4. **Testing**: E2E test verifying events display

## Workflow Phases

### Phase 1: Requirements & Planning ⏳
- [ ] Analyze current home page in Blazor app
- [ ] Define minimal event data structure
- [ ] Create business requirements document
- [ ] Specify success criteria
- [ ] **HUMAN REVIEW POINT** - Approve requirements

### Phase 2: Design & Architecture ⏳
- [ ] UI mockup for home page (ui-designer)
- [ ] API endpoint specification
- [ ] Component structure design
- [ ] Data flow diagram

### Phase 3: Implementation ⏳
- [ ] Backend: Create Events controller with mock data (backend-developer)
- [ ] Frontend: Create HomePage component (react-developer)
- [ ] Frontend: Setup API integration (react-developer)
- [ ] **HUMAN REVIEW POINT** - Review first working version

### Phase 4: Testing & Validation ⏳
- [ ] Write unit tests for API endpoint (test-developer)
- [ ] Write component tests (test-developer)
- [ ] Write E2E test with Playwright (test-developer)
- [ ] Run all tests (test-executor)
- [ ] **MANDATORY**: Run lint validation (lint-validator)
- [ ] Fix any issues found

### Phase 5: Finalization ⏳
- [ ] **MANDATORY**: Format all code (prettier-formatter)
- [ ] Update documentation (librarian)
- [ ] Document lessons learned
- [ ] Update migration patterns
- [ ] Clean up temporary files

## Sub-Agents Involved

| Agent | Role | Phase |
|-------|------|-------|
| ui-designer | Create mockups and wireframes | Phase 2 |
| backend-developer | Implement API endpoint | Phase 3 |
| react-developer | Implement React components | Phase 3 |
| test-developer | Write all test code | Phase 4 |
| test-executor | Run tests and report results | Phase 4 |
| lint-validator | Validate code quality | Phase 4 (MANDATORY) |
| prettier-formatter | Format all code | Phase 5 (MANDATORY) |
| librarian | Update documentation | Phase 5 |

## Success Criteria

### Functional Requirements
- [ ] Home page loads successfully
- [ ] Events are fetched from API
- [ ] Events display in card format
- [ ] Loading state shows while fetching
- [ ] Error state handles API failures
- [ ] Responsive on mobile and desktop

### Technical Requirements
- [ ] All tests pass (unit, component, E2E)
- [ ] No lint errors or warnings
- [ ] Code properly formatted
- [ ] TypeScript types fully defined
- [ ] API documented in Swagger

### Process Requirements
- [ ] All 5 workflow phases executed
- [ ] All sub-agents properly invoked
- [ ] Quality gates enforced
- [ ] Lessons learned documented
- [ ] File registry updated

## Files to Create/Modify

### Backend
- `/apps/api/Controllers/EventsController.cs`
- `/apps/api/Models/Event.cs`

### Frontend
- `/apps/web/src/pages/HomePage.tsx`
- `/apps/web/src/components/EventCard.tsx`
- `/apps/web/src/services/api.ts`
- `/apps/web/src/hooks/useEvents.ts`

### Tests
- `/apps/api/Controllers/EventsController.test.cs`
- `/apps/web/src/pages/HomePage.test.tsx`
- `/tests/e2e/home-page.spec.ts`

## How to Execute

### Using the Orchestrate Command
```
/orchestrate Implement a vertical slice for the home page that displays events. This is a test of the full workflow process. Create a simple GET /api/events endpoint with mock data, a React HomePage component that fetches and displays the events, and an E2E test. Follow all 5 phases with proper sub-agent delegation and enforce quality gates.
```

## Notes for Implementation

### Reference the Old Project
When implementing, reference the original Blazor implementation:
```bash
# View original home page
../witchcityrope/src/WitchCityRope.Web/Pages/Index.razor

# View original events controller
../witchcityrope/src/WitchCityRope.Api/Features/Events/EventsController.cs
```

### Mock Data Structure
Keep the event structure simple for this test:
```typescript
interface Event {
  id: string;
  title: string;
  description: string;
  date: string;
  location: string;
  capacity: number;
  imageUrl?: string;
}
```

### Quality Gates Reminder
- **Phase 4**: lint-validator MUST run and pass
- **Phase 5**: prettier-formatter MUST run
- These are MANDATORY, not optional

## Expected Outcome

After successful completion:
1. Working home page displaying events
2. Full workflow process validated
3. All agents functioning correctly
4. Patterns established for rest of migration
5. Lessons learned documented for future work

## Ready to Begin

All infrastructure is in place. Use the orchestrate command above to begin the vertical slice implementation with full workflow coordination.