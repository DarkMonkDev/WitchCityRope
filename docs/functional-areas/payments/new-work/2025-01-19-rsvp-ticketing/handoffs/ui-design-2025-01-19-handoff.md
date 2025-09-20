# AGENT HANDOFF DOCUMENT

## Phase: UI Design and Specifications
## Date: 2025-09-19
## Feature: RSVP and Ticketing System
## Version: 2.0 - STAKEHOLDER CORRECTIONS APPLIED - APPROVED

## 🚨 CRITICAL STAKEHOLDER CORRECTIONS APPLIED

**URGENT CORRECTIONS IMPLEMENTED**:

### 1. **TERMINOLOGY CORRECTION** ✅ FIXED
- **BEFORE**: Mixed usage of "register" and "RSVP"
- **AFTER**: ALL instances of "register" replaced with "RSVP"
- **Applied to**: All component names, function names, UI text, documentation

### 2. **RSVP FLOW CORRECTION** ✅ FIXED
- **BEFORE**: Section 3 showed "RSVP Only Flow" without ticket purchase option
- **AFTER**: Social events ALWAYS have option to purchase ticket after RSVP
- **Clarification**: There is NO "RSVP only" flow - ticket purchase is ALWAYS available

### 3. **BUTTON CSS FIX** ✅ FIXED
- **BEFORE**: Custom Mantine Button styling causing text cutoff
- **AFTER**: Standard CSS classes (`.btn .btn-primary`) used to prevent text cutoff
- **Reference**: `/docs/lessons-learned/react-developer-lessons-learned.md`

### 4. **EVENT DETAIL PAGE CLARIFICATION** ✅ FIXED
- **BEFORE**: Unclear distinction between social events and classes
- **AFTER**:
  - **Social Events**: Right panel shows "RSVP" button FIRST, THEN optional "Purchase Ticket"
  - **Classes**: Right panel shows ticket price selector and "Purchase Ticket" button
  - **Clarified**: These are DIFFERENT UI components for each event type

## 🚨 CRITICAL DESIGN DECISIONS (MUST IMPLEMENT)

### 1. **SIMPLIFIED RSVP MODEL**: Single participation per user per event
   - ✅ Implemented: User flows support ONE RSVP/ticket per user
   - ❌ NOT Implemented: Multi-person RSVP interfaces (explicitly removed per requirements)
   - **UI Pattern**: Single action buttons, no quantity selectors for RSVPs

### 2. **ROLE-BASED ACCESS CONTROL**: Visual enforcement of user permissions
   - ✅ Implemented: Complete role-checking UI patterns in ParticipationCard component
   - **Social Events**: Vetted Members only - clear error states for General Members
   - **Classes**: All active members allowed - unified purchase flow
   - **Banned Users**: Hard block with access denied messages

### 3. **DUAL PARTICIPATION TRACKING**: Separate RSVP and Ticket concepts
   - ✅ Implemented: Distinct status badges, separate dashboard sections
   - **RSVP**: Green badges, "RSVP Confirmed" status
   - **Ticket**: Burgundy badges, "Ticket Purchased" status
   - **Both**: Purple badges, "Fully RSVPed" status

### 4. **MANTINE v7 COMPLIANCE**: Signature design system implementation
   - ✅ Implemented: All components use Mantine v7 with WCR customizations
   - **Floating Labels**: ALL form inputs use floating label animation
   - **Button Fix**: Standard CSS classes prevent text cutoff issues
   - **Color Palette**: Exact WCR colors (#880124 burgundy, #FFBF00 amber)

### 5. **OPTIONAL TICKET PURCHASES**: Social event revenue model
   - ✅ **FIXED**: Two-step flow - RSVP first, then optional ticket purchase ALWAYS available
   - **UI Flow**: "RSVP Now (Free)" + "Purchase Ticket ($X suggested donation)"
   - **After RSVP**: Ticket purchase ALWAYS available as secondary action

## 📍 KEY DOCUMENTS UPDATED

| Document | Path | Critical Updates |
|----------|------|------------------|
| **Wireframe Analysis** | `/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/design/wireframe-analysis.md` | ✅ All stakeholder corrections applied |
| **UI Specifications** | `/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/design/ui-specifications.md` | ✅ All stakeholder corrections applied |

## 🚨 COMPONENT HIERARCHY (CRITICAL FOR IMPLEMENTATION)

### Primary Components to Build:

#### 1. **ParticipationCard** (Highest Priority)
```typescript
interface ParticipationCardProps {
  event: Event;
  user: User | null;
  participation: Participation | null;
  onRSVP: () => void;
  onPurchaseTicket: (amount: number) => void;
  onCancel: () => void;
}

// States:
// - AnonymousState: Show login prompt
// - BannedState: Show access denied
// - IneligibleState: Show vetting required (General members for social events)
// - AvailableState: Show RSVP/purchase options
// - ParticipatingState: Show current status + cancel options
```

#### 2. **UserDashboard** (Medium Priority)
```typescript
interface UserDashboardProps {
  user: User;
  upcomingEvents: ParticipationWithEvent[];
  pastEvents: ParticipationWithEvent[];
}

// Sections:
// - Upcoming Events (with active RSVPs and tickets)
// - Past Events (with participation history)
// - Canceled Events (with status tracking)
```

#### 3. **AdminEventParticipants** (Lower Priority)
```typescript
interface AdminEventParticipantsProps {
  event: Event;
  participants: ParticipationWithUser[];
  filters: ParticipantFilters;
  onFilterChange: (filters: ParticipantFilters) => void;
  onExport: () => void;
}

// Features:
// - Filterable participant table
// - Export functionality
// - Status tracking (Active/Canceled)
```

## 🚨 BUTTON IMPLEMENTATION FIX (CRITICAL)

### MANDATORY: Use Standard CSS Classes to Prevent Text Cutoff

```jsx
// ✅ CORRECT - Use standard CSS classes from /apps/web/src/index.css
<button className="btn btn-primary" onClick={handleRSVP}>
  RSVP Now (Free)
</button>

<button className="btn btn-secondary" onClick={handleTicketPurchase}>
  Purchase Ticket (${ticketPrice})
</button>

<button className="btn btn-secondary" onClick={openCancelModal}>
  Cancel RSVP
</button>

// ❌ WRONG - Inline styles cause text cutoff issues
<Button
  variant="filled"
  styles={{
    root: {
      background: 'linear-gradient(...)', // Custom styling causes cutoff
      borderRadius: '12px 6px 12px 6px',
      // ... other inline styles
    }
  }}
>
  Text gets cut off
</Button>
```

**Available Standard Classes**:
- `.btn .btn-primary` - Amber gradient for primary RSVP actions
- `.btn .btn-secondary` - Burgundy outline for secondary actions
- `.btn .btn-large` - Large button modifier

## 🚨 USER FLOW IMPLEMENTATIONS (CRITICAL CORRECTIONS)

### 1. **Social Event RSVP Flow (CORRECTED)**
```
[Social Event Detail Page]
        |
    Check User Role (useUserRoles() - supports role stacking)
        |
    ├─ Anonymous ─────── [Login Prompt]
    ├─ Banned ──────── [Access Denied Alert]
    ├─ General Member ── [Vetting Required Alert with CTA]
    └─ Vetted Member ───┐
                        ├─ No Participation ──── [RSVP Now (Free)] + [Purchase Ticket ($X)]
                        ├─ Has RSVP ─────── [Purchase Ticket ($X)] + [Cancel RSVP]
                        └─ Has Ticket ────── [Fully RSVPed ✓] + [Cancel Ticket]
```

### 2. **Social Event RSVP Confirmation Flow (CORRECTED)**
```
[RSVP Now Button] → [Confirmation Modal] → [Email Sent] → [Success Page]
                                      ↓
                              [ALWAYS AVAILABLE: Purchase Ticket ($X)]
```

**CRITICAL**: Social events ALWAYS show ticket purchase option - there is NO "RSVP only" flow.

### 3. **Dashboard Event Management**
```
[User Dashboard]
        |
    ├─ Upcoming Events Section ──┐
    │                            ├─ Event Card (RSVP only) ── [Cancel RSVP] [Purchase Ticket]
    │                            ├─ Event Card (Ticket only) ── [Cancel Ticket]
    │                            └─ Event Card (Both) ─── [Manage Participation]
    │
    └─ Past Events Section ──── [Event Cards with Participation History]
```

## 🚨 ROLE-BASED ACCESS PATTERNS (IMPLEMENTATION CRITICAL)

### User Role Checking
```typescript
// Use existing auth store - supports role stacking
const user = useUser();
const userRoles = useUserRoles(); // Returns array of roles

// Access control functions
const canAccessSocialEvent = (user: User) => {
  if (user.status === 'Banned') return false;
  return user.roles.some(role => ['Vetted', 'Teacher', 'Administrator'].includes(role));
};

const canPurchaseClassTicket = (user: User) => {
  if (user.status === 'Banned') return false;
  return user.roles.some(role => ['General', 'Vetted', 'Teacher', 'Administrator'].includes(role));
};

// Role stacking example: User can be Vetted + Teacher + Admin simultaneously
// UI should show highest privilege level while respecting all roles
```

### Access Denied UI States
```jsx
// Banned Users - Hard Block
<Alert type="error" title="Access Denied">
  Your account access has been restricted. Please contact support for assistance.
  <Button component={Link} href="/contact">Contact Support</Button>
</Alert>

// General Members on Social Events
<Alert type="info" title="Vetting Required">
  This social event is limited to vetted members. Complete the vetting process to attend.
  <Button component={Link} href="/vetting">Start Vetting Process</Button>
</Alert>
```

## 🚨 RESPONSIVE DESIGN REQUIREMENTS

### Mobile-First Breakpoints (Mantine v7)
- **xs**: 0px - 575px (Mobile)
- **sm**: 576px - 767px (Large Mobile)
- **md**: 768px - 991px (Tablet)
- **lg**: 992px - 1199px (Desktop)
- **xl**: 1200px+ (Large Desktop)

### Critical Mobile Patterns
```css
/* Mobile Event Detail Layout */
@media (max-width: 768px) {
  .event-detail-container {
    grid-template-columns: 1fr; /* Stack layout */
  }

  .participation-card {
    position: sticky;
    bottom: 0;
    /* Thumb-reach zone optimization */
  }

  .btn-rsvp, .btn-purchase {
    min-height: 48px; /* Touch target compliance */
  }
}
```

## 🚨 ACCESSIBILITY REQUIREMENTS (WCAG 2.1 AA)

### Color Contrast Compliance
- **All status colors MUST meet 4.5:1 contrast ratio**
- **Never rely on color alone** - always include text/icons
- **High contrast mode support** for users with visual impairments

### Keyboard Navigation
```typescript
// MANDATORY: All interactive elements keyboard accessible
const handleKeyDown = (event: KeyboardEvent) => {
  switch (event.key) {
    case 'Enter':
    case ' ': // Space bar
      event.preventDefault();
      handleAction();
      break;
    case 'Escape':
      handleCancel();
      break;
  }
};
```

### Screen Reader Support
```jsx
// MANDATORY: Proper ARIA labels
<Button
  aria-label="RSVP for Introduction to Rope Bondage on March 15th"
  aria-describedby="rsvp-description"
>
  RSVP Now (Free)
</Button>

<div id="rsvp-description" className="sr-only">
  Confirms your attendance at this social event. This is free and does not include a ticket.
</div>
```

## 🚨 PERFORMANCE REQUIREMENTS

### Code Splitting Strategy
```typescript
// Lazy load admin components (not needed for general users)
const AdminEventParticipants = lazy(() => import('./AdminEventParticipants'));

// Lazy load checkout flow (only when purchasing)
const CheckoutFlow = lazy(() => import('./CheckoutFlow'));
```

### Data Fetching Optimization
```typescript
// Use React Query for participation data
const useParticipation = (eventId: string) => {
  return useQuery({
    queryKey: ['participation', eventId],
    queryFn: () => fetchParticipation(eventId),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

// Optimistic updates for RSVP (instant UI feedback)
const useCreateRSVP = () => {
  return useMutation({
    mutationFn: createRSVP,
    onMutate: async (variables) => {
      // Optimistically update UI before server response
      queryClient.setQueryData(['participation', variables.eventId], (old) => ({
        ...old,
        rsvp: { exists: true, status: 'Active', createdAt: new Date() }
      }));
    }
  });
};
```

## 🚨 DISCOVERED CONSTRAINTS & SOLUTIONS

### 1. **Existing Wireframe Gaps**
- **Problem**: Social event RSVP functionality completely missing from existing wireframes
- **Solution**: Created new component specifications for social event RSVP flow
- **Implementation**: ParticipationCard component handles all event types with role-based logic

### 2. **Role Stacking Complexity**
- **Problem**: UI needs to handle users with multiple roles (Vetted + Teacher + Admin)
- **Solution**: Role checking functions use array.some() to check for any qualifying role
- **Implementation**: Highest privilege level displayed, all roles respected

### 3. **Dual Participation Concept**
- **Problem**: Users can have both RSVP (free) and Ticket (paid) for same event
- **Solution**: Separate status tracking with combined badge system
- **Implementation**: StatusBadge component handles all combinations

### 4. **$0 Transaction Handling**
- **Problem**: RSVPs are free but need confirmation flow
- **Solution**: Bypass payment processing, use email confirmation only
- **Implementation**: RSVP flow completely separate from checkout flow

### 5. **Mobile CTA Optimization**
- **Problem**: Participation cards need thumb-reach optimization
- **Solution**: Sticky bottom positioning on mobile with 48px+ touch targets
- **Implementation**: Responsive CSS with mobile-first approach

### 6. **Button Text Cutoff Issue (STAKEHOLDER CRITICAL)**
- **Problem**: Custom Mantine Button styling caused text cutoff
- **Solution**: Use standard CSS classes from `/apps/web/src/index.css`
- **Implementation**: Replace all custom Button styling with `.btn .btn-primary` classes

## ✅ VALIDATION CHECKLIST (MUST VERIFY)

### Design System v7 Compliance
- [ ] Exact WCR color palette used (#880124, #FFBF00, #B76D75, etc.)
- [ ] Floating label animation on ALL form inputs
- [ ] ✅ Button components use standard CSS classes (no custom styling)
- [ ] Typography hierarchy (Bodoni Moda, Montserrat, Source Sans 3)
- [ ] NO purple buttons (only amber primary, burgundy secondary)
- [ ] Navigation center-outward underline animation preserved

### Business Requirements Compliance (STAKEHOLDER CORRECTED)
- [ ] ✅ Single RSVP per user per event (no multi-person)
- [ ] ✅ Role stacking supported (Vetted + Teacher + Admin)
- [ ] ✅ Social events limited to vetted members
- [ ] ✅ Classes open to all active members
- [ ] ✅ Banned users completely blocked
- [ ] ✅ Optional ticket purchase for social events ALWAYS available
- [ ] ✅ Automatic RSVP creation when purchasing tickets
- [ ] ✅ Canceled participation retained with status
- [ ] ✅ ALL "register" terminology replaced with "RSVP"

### Technical Implementation
- [ ] Mantine v7 components used exclusively
- [ ] ✅ Standard CSS classes used for buttons (text cutoff prevention)
- [ ] TypeScript interfaces defined for all props
- [ ] React Query for data fetching and caching
- [ ] Lazy loading for admin components
- [ ] Optimistic updates for user actions
- [ ] Error boundaries for graceful failure handling

### Accessibility & Mobile
- [ ] WCAG 2.1 AA compliance (4.5:1 contrast, keyboard nav)
- [ ] Touch targets minimum 44px on mobile
- [ ] Screen reader support with proper ARIA
- [ ] Mobile responsive at 768px breakpoint
- [ ] Thumb-reach optimization for CTAs

## ⚠️ DO NOT IMPLEMENT (EXPLICITLY REMOVED)

- ❌ Multi-person RSVP functionality (future scope only)
- ❌ Role exclusivity (users can have multiple roles)
- ❌ Data deletion on cancellation (use status updates)
- ❌ Complex incident categorization (keep simple severity levels)
- ❌ SMS notifications (email only)
- ❌ Purple buttons (only amber/burgundy allowed in design system)
- ❌ Vertical button movement (translateY) - only corner morphing
- ❌ Manual TypeScript interfaces for API data (use NSwag generation)
- ❌ **Custom Mantine Button styling (causes text cutoff)**
- ❌ **"RSVP only" flow without ticket purchase option**

## 🔗 NEXT AGENT INSTRUCTIONS

### MANDATORY READING ORDER:
1. **FIRST**: Read complete UI specifications document (all components and patterns)
2. **SECOND**: Review existing wireframes to understand baseline patterns
3. **THIRD**: Study Mantine v7 documentation for component APIs
4. **FOURTH**: Check existing auth store patterns for role checking
5. **FIFTH**: Review PayPal webhook implementation for ticket purchases
6. **CRITICAL**: Review button implementation in `/apps/web/src/index.css`

### IMPLEMENTATION PRIORITIES:
1. **Week 1**: ParticipationCard component with all role-based states
2. **Week 2**: User dashboard with event sections
3. **Week 3**: Admin participant management (lower priority)

### CRITICAL SUCCESS FACTORS:
- **Role-Based Access**: Every UI element respects user permissions
- **Design System**: Perfect adherence to WCR visual standards
- **Button Implementation**: Use standard CSS classes to prevent text cutoff
- **Mobile First**: Thumb-reach optimization and touch targets
- **Performance**: Lazy loading and optimistic updates
- **Accessibility**: Screen reader and keyboard support
- **Terminology**: NEVER use "register" - always use "RSVP"

### TESTING REQUIREMENTS:
- **Role Testing**: Test with all role combinations (General, Vetted, Teacher, Admin, Banned)
- **Device Testing**: iOS Safari, Chrome Android, desktop browsers
- **Accessibility Testing**: Screen reader (VoiceOver/NVDA), keyboard-only
- **Performance Testing**: Bundle size, load times, mobile performance
- **Button Testing**: Verify no text cutoff on all button text lengths

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: Business Requirements Agent (Requirements APPROVED)
**Current Agent**: UI Designer Agent (Design APPROVED - STAKEHOLDER CORRECTIONS APPLIED)
**Phase Completed**: 2025-09-19 (UI Design & Specifications with Critical Corrections)
**Status**: ✅ **APPROVED - Ready for Phase 3**

**Next Agent Should Be**: React Developer for component implementation
**Next Phase**: Component Development with Mantine v7
**Estimated Effort**: 2-3 weeks for complete implementation

**Critical Handoff Items**:
- Complete component specifications with TypeScript interfaces
- Mantine v7 implementation patterns with WCR theme
- ✅ Button implementation using standard CSS classes
- Role-based access control patterns
- Mobile-first responsive design requirements
- Accessibility compliance checklist
- ✅ All stakeholder corrections applied and validated

---

## 🚀 INTEGRATION NOTES

**Existing Systems to Leverage:**
- Mantine v7 UI framework (ADR-004) with WCR theme customization
- ✅ Standard CSS classes in `/apps/web/src/index.css` for buttons
- Existing auth store with role stacking support
- PayPal webhook integration for ticket purchases
- React Router for navigation and protected routes
- React Query for state management and caching

**New Components to Build:**
- ParticipationCard (event RSVP interface)
- UserDashboard (event management interface)
- AdminEventParticipants (admin management interface)
- StatusBadge (participation status indicators)
- Access control guards and error states

**Design Patterns Established:**
- Role-based component rendering
- Optimistic UI updates for user actions
- Mobile-first responsive design
- WCR signature animations and styling
- Accessibility-first interaction patterns
- ✅ Standard CSS class usage for buttons

**🚨 CRITICAL STAKEHOLDER CORRECTIONS SUMMARY**:
1. ✅ **TERMINOLOGY**: All "register" → "RSVP"
2. ✅ **FLOW**: Social events ALWAYS show ticket purchase option
3. ✅ **BUTTONS**: Use standard CSS classes to prevent text cutoff
4. ✅ **UI DISTINCTION**: Different components for social events vs classes

This handoff provides complete UI design specifications for implementing the RSVP and Ticketing System with full business requirements compliance, stakeholder corrections, and design system adherence.