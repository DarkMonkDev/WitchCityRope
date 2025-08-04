# Admin Events Management - Implementation Plan

## Current State Analysis

### Tab Structure ✅
The current implementation already has the correct 4 tabs as specified in the wireframes:
1. **Basic Info** - Event details, type, schedule, venue
2. **Tickets/Orders** - Pricing, ticket types, current orders, volunteer tickets
3. **Emails** - Email templates and custom email sending
4. **Volunteers/Staff** - Volunteer assignments and task management

### Implementation Status by Tab

#### 1. Basic Info Tab ✅
**Fields Present:**
- Event Type toggle (Class/Meetup) ✅
- Event Title ✅
- Event Image upload ✅
- Rich text description ✅
- Lead Teacher dropdown ✅
- Start/End DateTime ✅
- Venue Address ✅
- Capacity ✅
- Registration Opens/Closes ✅

**Status:** Fully implemented according to wireframe

#### 2. Tickets/Orders Tab 🔄
**Fields Present:**
- Pricing Type (Fixed/Sliding Scale/Free) ✅
- Ticket Types (Individual/Couples/Both) ✅
- Individual/Couples Price fields ✅
- Refund Cutoff dropdown ✅
- Current Orders table ✅
- Add Tickets for Volunteers section ✅

**Issues Found:**
- Sliding scale fields (minimum/suggested/maximum) may not be shown conditionally
- Orders table may not be fetching real data
- Volunteer ticket assignment needs verification

#### 3. Emails Tab 🔄
**Fields Present:**
- Email Template selection cards ✅
- Subject line editor ✅
- Rich text body editor ✅
- Available variables display ✅
- Send Custom Email section ✅
- Recipient selection ✅
- Draft/Test/Send buttons ✅

**Issues Found:**
- Email templates may not be loading from backend
- Variable substitution may not be implemented
- Send functionality needs API connection verification

#### 4. Volunteers/Staff Tab 🔄
**Fields Present:**
- Volunteer summary statistics ✅
- Volunteers table ✅
- Volunteer Tasks management ✅
- Add/Edit/Delete task functionality ✅

**Issues Found:**
- Volunteer data may not be loading from API
- Task assignment functionality needs verification
- Background check status display missing

## Implementation Tasks

### Phase 1: Verify and Fix Data Loading (Priority: High)
1. **Tickets/Orders Tab**
   - Ensure current orders are loaded from API
   - Implement sliding scale field visibility based on pricing type
   - Verify volunteer ticket assignment works

2. **Emails Tab**
   - Load email templates from API
   - Implement variable substitution in preview
   - Connect send functionality to API endpoints

3. **Volunteers/Staff Tab**
   - Load volunteer assignments from API
   - Implement task CRUD operations
   - Add background check verification display

### Phase 2: Field Validation and Error Handling (Priority: Medium)
1. Add comprehensive form validation
2. Implement error messages for API failures
3. Add loading states for async operations
4. Add success notifications

### Phase 3: Integration Testing (Priority: High)
1. Create Puppeteer tests for each tab
2. Test all CRUD operations
3. Verify data persistence
4. Test edge cases and error scenarios

### Phase 4: User-Facing Verification (Priority: Medium)
1. Verify event changes appear on public Events page
2. Test registration flow with new pricing
3. Verify email sending to attendees
4. Check volunteer portal integration

## API Endpoints to Verify

### Events
- GET /api/events/{eventId} - Load event details
- PUT /api/events/{eventId} - Update event
- GET /api/admin/events - Admin event list

### Orders/Tickets
- GET /api/events/{eventId}/attendees - Get registrations
- POST /api/events/{eventId}/tickets/volunteer - Add volunteer ticket

### Emails
- GET /api/events/{eventId}/emails/templates - Get templates
- POST /api/events/{eventId}/emails/templates - Save template
- POST /api/events/{eventId}/emails/send - Send email

### Volunteers
- GET /api/volunteer/events/{eventId}/tasks - Get tasks
- POST /api/volunteer/events/{eventId}/tasks - Create task
- PUT /api/volunteer/tasks/{taskId} - Update task
- DELETE /api/volunteer/tasks/{taskId} - Delete task
- POST /api/volunteer/tasks/{taskId}/assignments - Assign volunteer

## Success Criteria

1. All fields in wireframe are present and functional
2. Data persists correctly to database
3. Changes reflect on user-facing pages
4. All API endpoints return expected data
5. Comprehensive test coverage (>80%)
6. No console errors or warnings
7. Responsive design works on mobile/tablet
8. Performance metrics meet standards

## Testing Checklist

### Manual Testing
- [ ] Create new event with all fields
- [ ] Edit existing event
- [ ] Change pricing types and verify field updates
- [ ] Upload and remove event image
- [ ] Add/edit/delete volunteer tasks
- [ ] Send test emails
- [ ] Add volunteer tickets
- [ ] View current orders

### Automated Testing
- [ ] All form validations work
- [ ] API error handling works
- [ ] Loading states display correctly
- [ ] Success notifications appear
- [ ] Data saves persist after refresh
- [ ] Tab navigation maintains state
- [ ] Rich text editors work properly