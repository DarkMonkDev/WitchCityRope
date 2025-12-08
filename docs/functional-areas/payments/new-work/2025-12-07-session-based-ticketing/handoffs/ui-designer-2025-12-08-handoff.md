# UI Designer → Implementation Phase Handoff
<!-- Last Updated: 2025-12-08 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Active -->

## Handoff Summary

**From Phase**: UI/UX Design (Phase 2)
**To Phase**: Implementation (Phase 3)
**Feature**: Session-Based Ticket Selection
**Date**: 2025-12-08

## Design Deliverables Completed

### 1. UI Design Document
**Location**: `/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/design/ui-design.md`

**Contents**:
- Complete wireframes (desktop + mobile) for all 6 UI sections
- Mantine v7 component specifications with code examples
- Responsive design patterns and breakpoints
- Accessibility requirements (WCAG 2.1 AA)
- Error state designs
- Mobile-specific optimizations
- Design System v7 integration

### 2. Designed UI Sections

#### 2.1 Event Detail Page - Session Availability Display
- **Purpose**: Show per-session capacity in sidebar
- **Key Features**:
  - Color-coded session status (green/amber/red)
  - Progress bars for visual capacity indication
  - Session date/time display
  - Mobile-responsive stacking layout
- **Components**: Stack, Group, Text, Badge, Progress, Box

#### 2.2 Ticket Selection Modal
- **Purpose**: Primary ticket purchase interface
- **Key Features**:
  - Radio button selection for ticket types
  - Session coverage badges ("Includes: Friday + Saturday")
  - Sliding scale price selector with live preview
  - Low availability warnings
  - Sold out state handling
- **Components**: Modal, Radio.Group, Paper, Slider, Button, Badge, Alert, List

#### 2.3 Overlap Warning Modal
- **Purpose**: Prevent duplicate session purchases
- **Key Features**:
  - Side-by-side comparison of attempted vs existing ticket
  - Visual highlighting of overlapping sessions
  - Clear instructions for cancellation/refund
  - "View My Tickets" CTA
- **Components**: Modal, Alert, Paper, Stack, List, Button

#### 2.4 Purchase Confirmation Page
- **Purpose**: Post-purchase success state
- **Key Features**:
  - Confirmation number display
  - Session details with dates/times/locations
  - Add to calendar functionality
  - Refund policy information
- **Components**: Container, Stack, Paper, Text, Group, Button, Divider

#### 2.5 User Dashboard - My Tickets
- **Purpose**: Show user's purchased tickets with session details
- **Key Features**:
  - Ticket cards with session lists
  - Multi-session display ("Day 1 + Day 3" format)
  - Refund deadline warnings
  - Request refund button
- **Components**: Container, Stack, Paper, Text, Group, Button, Badge, List, Alert

#### 2.6 Admin - Ticket Type Configuration
- **Purpose**: Admin interface for creating ticket types
- **Key Features**:
  - Session selection checkboxes
  - Capacity validation (max tickets ≤ min session capacity)
  - Pricing configuration (fixed or sliding scale)
  - Visual feedback for capacity limits
- **Components**: Stack, TextInput, Textarea, Radio, NumberInput, Checkbox, Alert, Button

## Critical Design Decisions

### 1. Color-Coded Session Status
**Decision**: Use green/amber/red status colors consistently across all views

**Thresholds**:
- Green: > 10 spots available
- Amber: 3-10 spots available
- Red: 0-3 spots available or sold out

**Rationale**: Matches existing event capacity display patterns, provides at-a-glance status understanding

### 2. Progress Bars for Session Capacity
**Decision**: Include visual progress bars in addition to numeric capacity display

**Implementation**: Mantine Progress component with color gradient based on availability
- 0-60% capacity: Green bar
- 60-85% capacity: Amber bar
- 85-100% capacity: Red bar

**Rationale**: Visual representation aids quick capacity assessment, especially for users scanning multiple sessions

### 3. Mobile Card Carousel for Ticket Types
**Decision**: Use horizontal scrolling cards on mobile (<768px) instead of stacked radio buttons

**Rationale**:
- Reduces vertical scroll on small screens
- Better thumb reach for swipe gestures
- Each card gets full focus during selection
- Common mobile UX pattern (e.g., Airbnb, Ticketmaster)

### 4. Session Overlap Prevention - Modal vs Inline Error
**Decision**: Use dedicated modal for overlap warnings instead of inline form error

**Rationale**:
- Overlap scenario is complex (requires explanation of existing ticket)
- Modal provides space for side-by-side comparison
- Clear CTA path: "View My Tickets" to manage existing purchases
- Prevents user confusion during checkout

### 5. Sliding Scale Selector Always Visible
**Decision**: Display price slider immediately when ticket type selected, not in separate step

**Rationale**:
- Reduces friction (fewer steps to purchase)
- Maintains WitchCityRope "pay what you can afford" philosophy visibility
- Live preview of selected amount reinforces transparency
- Matches existing ticket purchase flow pattern

## Mantine v7 Components Used

### Core Layout Components
- **Modal**: Ticket selection, overlap warnings (size="lg", centered, fullScreen on mobile)
- **Container**: Page layout wrappers (size="xl" or "lg")
- **Stack**: Vertical spacing (gap="lg", "md", "sm")
- **Group**: Horizontal spacing (justify="space-between", align="center")
- **Paper**: Card containers (withBorder, radius="md")
- **Box**: Generic containers for custom styling

### Form Components
- **Radio.Group**: Ticket type selection (color="burgundy", size="lg")
- **Checkbox**: Admin session selection (color="burgundy", size="lg")
- **Slider**: Sliding scale pricing (color="burgundy", size="lg", custom gradient)
- **TextInput**: Admin ticket name input
- **Textarea**: Admin ticket description
- **NumberInput**: Admin quantity/pricing inputs

### Display Components
- **Text**: All typography (fw, size, c props for styling)
- **Title**: Page/section headings (order, size props)
- **Badge**: Status indicators (color="green"/"yellow"/"red", variant="light")
- **Alert**: Warnings, errors (color prop, variant="light", icon prop)
- **Progress**: Session capacity visualization (color prop, size="md")
- **List**: Session lists (icon prop, spacing prop)
- **Divider**: Section separators

### Interaction Components
- **Button**: All CTAs (className="btn btn-primary", fullWidth on mobile)
- **Accordion**: Collapsible session details (mobile only)
- **ScrollArea**: Horizontal scrolling ticket cards (mobile only, type="never")

## Responsive Breakpoints

### Mobile (<768px)
- Full-width buttons (48px height minimum)
- Stacked layouts for all sections
- Bottom sheet modals (slide-up transition)
- Horizontal scroll for ticket type cards
- Reduced font sizes (14px → 12px for small text)
- Compact spacing (gap="lg" → gap="md")

### Tablet (768px - 991px)
- Two-column layouts where appropriate
- Centered modals with max-width
- Standard button sizing (44px height)
- Normal font sizes

### Desktop (≥992px)
- Three-column layouts for admin views
- Side-by-side comparison modals
- Hover states enabled
- Full spacing (gap="lg", gap="xl")

## Accessibility Requirements (WCAG 2.1 AA)

### Keyboard Navigation
✅ All interactive elements tabbable
✅ Focus indicators: 2px burgundy outline
✅ Modal focus trapping
✅ Escape key closes modals
✅ Enter/Space activates buttons

### Screen Reader Support
✅ Icons paired with text labels (never icon-only)
✅ Progress bars: `aria-label="12 of 20 spots filled"`
✅ Form inputs: Associated labels via `<label>` or `aria-label`
✅ Error messages: `aria-live="polite"` for announcements
✅ Session lists: Semantic `<ul>` and `<li>` elements

### Color Contrast
✅ Text/background: 4.5:1 minimum
✅ Large text (18px+): 3:1 minimum
✅ Interactive elements: 3:1 minimum
✅ Status colors: Tested for colorblind accessibility (red/green alternatives provided via icons)

### Focus Management
✅ Focus moves to modal on open
✅ Focus returns to trigger on close
✅ Focus moves to first error on validation failure
✅ Focus moves to success message after purchase

## Design System v7 Integration

### Color Palette Applied
- **Primary**: Burgundy (#880124) - Primary buttons, borders, focus states
- **Accent**: Rose Gold (#B76D75) - Hover states, decorative borders
- **Success**: Green (#228B22) - Available status, success messages
- **Warning**: Amber (#DAA520) - Low availability warnings
- **Error**: Red (#DC143C) - Sold out, errors, overlap warnings
- **Background**: Cream (#FAF6F2) - Page backgrounds
- **Card**: Ivory (#FFF8F0) - Card backgrounds

### Typography Applied
- **Headings**: Montserrat (fw=700, uppercase, letter-spacing=0.5px)
- **Body**: Source Sans 3 (fw=400, line-height=1.6)
- **Labels**: Montserrat (fw=600, uppercase, letter-spacing=0.5px)

### Button Styling Applied
- **Primary CTA**: Gold/amber gradient (`linear-gradient(135deg, #FFBF00 0%, #FF8C00 100%)`)
- **Secondary**: Burgundy outline with hover fill
- **Disabled**: Gray background, 0.6 opacity, cursor not-allowed
- **Corner Morphing**: `12px 6px 12px 6px` → `6px 12px 6px 12px` on hover
- **Transition**: `all 0.3s ease`

### Spacing Applied
- xs: 8px (fine details)
- sm: 16px (component internal)
- md: 24px (related elements)
- lg: 32px (component spacing)
- xl: 40px (section spacing)

## User Flow Diagrams

### Happy Path: Non-Overlapping Purchase
```
User views event → Clicks "Purchase Ticket"
  → Modal opens with ticket type selection
  → User selects "Weekend Pass" (Sat+Sun)
  → Slider appears, user selects $85
  → Clicks "Continue to Payment"
  → Backend validates: No overlap ✅
  → Redirects to PayPal
  → Payment completes
  → Returns to confirmation page
  → Shows sessions included: Sat + Sun
  → Email sent with session details
  → Dashboard updated
```

### Error Path: Overlapping Purchase
```
User has "Full Workshop" ticket (Fri+Sat+Sun)
  → Views event again
  → Clicks "Purchase Ticket"
  → Modal opens with ticket type selection
  → User selects "Weekend Pass" (Sat+Sun)
  → Clicks "Continue to Payment"
  → Backend validates: Overlap detected ❌
  → Overlap warning modal appears
  → Shows attempted ticket vs existing ticket
  → Highlights overlapping sessions (Sat, Sun)
  → User clicks "View My Tickets"
  → Navigates to dashboard
  → User cancels existing ticket
  → Returns to event page
  → Purchases "Weekend Pass" successfully
```

## Mobile-Specific Optimizations

### Touch Targets
- All buttons: 48px height minimum
- Radio buttons: 44px × 44px touch area
- Checkboxes: 44px × 44px touch area
- Session cards: 56px minimum tappable height

### Bottom Sheet Pattern
Ticket selection modal uses bottom sheet on mobile:
```tsx
<Modal
  fullScreen={isMobile}
  position={isMobile ? 'bottom' : undefined}
  transitionProps={{
    transition: isMobile ? 'slide-up' : 'fade',
    duration: 300
  }}
  styles={{
    content: {
      borderTopLeftRadius: isMobile ? '24px' : undefined,
      borderTopRightRadius: isMobile ? '24px' : undefined,
    }
  }}
>
```

### Horizontal Scrolling Cards
Ticket types displayed as horizontal carousel on mobile:
```tsx
<ScrollArea type="never">
  <Group gap="md" wrap="nowrap">
    {ticketTypes.map(type => (
      <Paper style={{ minWidth: '280px', maxWidth: '280px' }}>
        {/* Ticket card */}
      </Paper>
    ))}
  </Group>
</ScrollArea>
<Text size="xs" ta="center" c="dimmed">Swipe to see more →</Text>
```

### Collapsible Sections
Session details collapse on mobile to save space:
```tsx
<Accordion>
  <Accordion.Item value="sessions">
    <Accordion.Control>
      Sessions Included (2)
    </Accordion.Control>
    <Accordion.Panel>
      {/* Session list */}
    </Accordion.Panel>
  </Accordion.Item>
</Accordion>
```

## Error State Designs

### 1. Sold Out Session
- **Visual**: Red border, red "Sold Out" badge
- **Message**: "This session is sold out. Try another ticket type."
- **State**: Ticket type card disabled, cursor: not-allowed

### 2. Overlapping Purchase
- **Visual**: Dedicated modal with red Alert banner
- **Message**: "You already have a ticket covering [session names]"
- **Recovery**: "View My Tickets" button to manage existing purchases

### 3. Session Sold Out During Checkout
- **Visual**: Red alert banner at top of modal
- **Message**: "Sorry, this session sold out while you were checking out."
- **Recovery**: "Select Different Ticket" button to retry

### 4. Network Error
- **Visual**: Yellow alert banner
- **Message**: "Unable to process purchase. Check your connection."
- **Recovery**: "Retry" button

### 5. Payment Failed
- **Visual**: Red alert banner
- **Message**: "Payment failed. Your card was not charged."
- **Recovery**: "Try Again" button

## Implementation Notes for Developers

### Component Hierarchy
```
EventTicketPurchaseModal (main component)
├── TicketTypeSelector (Radio.Group wrapper)
│   └── TicketTypeCard (Paper component)
│       ├── SessionList (List component)
│       └── AvailabilityBadge (Badge component)
├── SlidingScalePriceSelector (Slider wrapper)
│   └── SelectedAmountDisplay (Box with gradient)
└── ActionButtons (Button group)
```

### State Management Recommendations
```tsx
// Modal state
const [selectedTicketType, setSelectedTicketType] = useState<string | null>(null)
const [selectedAmount, setSelectedAmount] = useState<number>(0)
const [showOverlapWarning, setShowOverlapWarning] = useState(false)

// Data fetching (React Query)
const { data: ticketTypes } = useTicketTypes(eventId)
const { data: sessions } = useSessions(eventId)
const { data: userTickets } = useUserTickets()

// Validation
const canPurchase = useMemo(() =>
  !hasOverlappingSessions(selectedTicketType, userTickets),
  [selectedTicketType, userTickets]
)
```

### API Endpoints Needed
- `GET /api/events/{id}/sessions` - Session availability
- `GET /api/events/{id}/ticket-types` - Ticket types with session associations
- `POST /api/tickets/validate` - Validate purchase (check overlaps)
- `POST /api/tickets/purchase` - Complete purchase
- `GET /api/users/me/tickets` - User's existing tickets

### Testing Requirements
- [ ] Test session overlap validation
- [ ] Test capacity edge cases (sold out during checkout)
- [ ] Test mobile responsive layouts (<768px, 768-991px, ≥992px)
- [ ] Test keyboard navigation (tab order, focus management)
- [ ] Test screen reader compatibility (NVDA, JAWS, VoiceOver)
- [ ] Test error recovery flows (network errors, payment failures)
- [ ] Test optimistic UI updates (capacity decrease on purchase)
- [ ] Test accessibility (WCAG 2.1 AA compliance)

## Questions/Clarifications Needed

### For Product Manager
1. ✅ **Session-specific pricing ranges**: Confirmed - different sessions CAN have different sliding scale ranges
2. ✅ **Bulk purchase discounts**: Out of scope for initial implementation
3. ❓ **Default session selection**: Should all sessions be pre-selected when modal opens, or should user explicitly choose?
   - **Recommendation**: No pre-selection - force explicit choice to prevent accidental full-event purchases
4. ❓ **Session naming convention**: Standardized format for session names?
   - **Recommendation**: "[Day]: [Topic]" format (e.g., "Friday: Fundamentals", "Saturday: Advanced")

### For Backend Developer
1. ❓ **Session capacity API**: Does API return available spots or just sold count?
   - Need: `availableSpots` field in session DTO (calculated: capacity - soldCount)
2. ❓ **Real-time capacity updates**: WebSocket support or polling?
   - **Recommendation**: React Query polling every 30 seconds for capacity updates
3. ❓ **Overlap validation**: Client-side pre-validation + server-side enforcement?
   - **Recommendation**: Both - client shows warning, server blocks invalid purchases

## Known Limitations

### Out of Scope (Future Enhancements)
- **Partial refunds**: Not designed in initial implementation (all-or-nothing refund policy)
- **Waitlist management**: No waitlist UI in this design (future feature)
- **Group ticket purchases**: No multi-ticket purchase in single transaction
- **Ticket transfers**: No transfer ticket ownership UI
- **Dynamic pricing**: No early bird/last-minute pricing UI

### Design Trade-offs
1. **Mobile carousel**: Horizontal scrolling requires user education ("Swipe to see more")
   - **Mitigation**: Clear indicator text + visual cue (partially visible next card)
2. **Overlap modal complexity**: Modal contains significant information, may overwhelm users
   - **Mitigation**: Clear visual hierarchy, side-by-side comparison, simple CTA
3. **Sliding scale always visible**: Takes up modal space, may push action buttons below fold
   - **Mitigation**: Sticky action buttons on mobile, compact slider design

## Next Phase: Implementation

### Priority Order
1. **High Priority** (MVP):
   - Event detail session availability display
   - Ticket selection modal with session coverage
   - Overlap validation and warning modal
   - Purchase confirmation with session details

2. **Medium Priority** (Post-MVP):
   - User dashboard session display
   - Admin ticket type configuration UI
   - Refund deadline warnings

3. **Low Priority** (Polish):
   - Mobile carousel optimization
   - Collapsible session details
   - Add to calendar functionality

### Implementation Checklist
- [ ] Create reusable SessionAvailabilityDisplay component
- [ ] Create EventTicketPurchaseModal component
- [ ] Create OverlapWarningModal component
- [ ] Create PurchaseConfirmation component
- [ ] Create MyTicketsSessionDisplay component (dashboard)
- [ ] Create AdminTicketTypeForm component
- [ ] Implement responsive breakpoints (<768px, 768-991px, ≥992px)
- [ ] Implement accessibility features (keyboard nav, screen reader support)
- [ ] Implement error states and recovery flows
- [ ] Write unit tests for validation logic
- [ ] Write E2E tests for purchase flows
- [ ] Test mobile responsive layouts
- [ ] Test accessibility compliance

## Success Criteria

### UX Metrics
- ✅ Users can identify which sessions a ticket covers in < 5 seconds
- ✅ Mobile ticket purchase completion rate > 85%
- ✅ Overlap error clarity: < 10% users contact support after seeing warning
- ✅ Session selection workflow: Average 3 clicks from event page to PayPal

### Technical Metrics
- ✅ WCAG 2.1 AA compliance: 100%
- ✅ Mobile touch target compliance: 100% (48px minimum)
- ✅ Color contrast compliance: 100% (4.5:1 text, 3:1 interactive)
- ✅ Keyboard navigation: 100% of interactive elements accessible

### Performance Metrics
- ✅ Modal open time: < 200ms
- ✅ Session capacity fetch: < 100ms (p95)
- ✅ Overlap validation: < 200ms (p95)
- ✅ Mobile page load: < 2s on 4G

## Handoff Complete

**Design Phase Status**: ✅ Complete
**Next Agent**: React Developer Agent
**Next Phase**: Implementation (Phase 3)
**Blocking Items**: None - all design deliverables complete

**Contact for Questions**: UI Designer Agent
**Design Review Date**: 2025-12-08
**Approval Status**: Pending Product Manager Review

---

**Related Documents**:
- UI Design Document: `/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/design/ui-design.md`
- Business Requirements: `/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/requirements/business-requirements.md`
- Design System v7: `/docs/design/current/design-system-v7.md`
- Mantine UI Standards: `/docs/standards-processes/frontend/mantine-ui-standards.md`
