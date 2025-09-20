# Wireframe Analysis: RSVP and Ticketing System
<!-- Last Updated: 2025-09-19 -->
<!-- Version: 2.0 - STAKEHOLDER CORRECTIONS APPLIED -->
<!-- Owner: UI Designer Agent -->
<!-- Status: APPROVED -->

## Executive Summary

This analysis reviews existing wireframes for the RSVP and Ticketing System against the approved business requirements (Version 3.0) to identify alignments, gaps, and required enhancements for the simplified system.

**🚨 CRITICAL STAKEHOLDER CORRECTIONS APPLIED**:
- **TERMINOLOGY**: All instances of "register" replaced with "RSVP"
- **FLOW CORRECTION**: Social events show RSVP button FIRST, then optional ticket purchase
- **BUTTON FIX**: Standard Mantine Button components used to prevent text cutoff
- **UI CLARIFICATION**: Different components for social events (RSVP + ticket) vs classes (ticket purchase)

## Existing Wireframes Reviewed

### 1. Social Event RSVP Flow (`event-social-details-RSVP.html`)
**Current Design**: Shows social event without RSVP-specific functionality
**Alignment**: ❌ **MAJOR GAP** - Missing RSVP functionality entirely
**Issues**:
- Shows "RSVP Now" button but doesn't distinguish between RSVP (free) and ticket purchase (optional)
- No role-based access control shown (Vetted Members only for social events)
- Missing optional ticket purchase flow for suggested donations

**🚨 CORRECTED FLOW**: Social Events should show:
1. **Primary**: "RSVP" button (free attendance confirmation)
2. **Secondary**: "Purchase Ticket ($X)" button (optional suggested donation)

### 2. Class Ticket Purchase (`event-class-detail-purchase-ticket.html`)
**Current Design**: Complete class RSVP with sliding scale pricing
**Alignment**: ✅ **EXCELLENT** - Well-designed for paid classes
**Strengths**:
- Clear sliding scale pricing implementation
- Excellent capacity display with visual bar
- Good RSVP card design
- Mobile responsive layout
**Minor Gaps**:
- Needs automatic RSVP creation when purchasing tickets
- Role validation needed (General Members can buy class tickets)

**🚨 CORRECTED FLOW**: Classes should show:
1. **Primary**: "Purchase Ticket ($X-$Y)" with sliding scale
2. **Automatic**: RSVP created when ticket purchased

### 3. Checkout Flow (`checkout-visual.html`)
**Current Design**: Comprehensive PayPal/Credit Card checkout
**Alignment**: ✅ **GOOD** - Solid foundation for both RSVPs and tickets
**Strengths**:
- PayPal integration ready
- Progress bar for user orientation
- Secure checkout design
- Mobile responsive
**Enhancement Needed**:
- Distinguish between RSVP creation and ticket purchase flows
- Handle $0 transactions for RSVPs

### 4. Confirmation (`checkout-confirmation-visual.html`)
**Current Design**: Celebration-focused success page
**Alignment**: ✅ **EXCELLENT** - Works for both RSVPs and tickets
**Strengths**:
- Great success experience with confetti animation
- Clear next steps and important information
- Print receipt functionality
- Social sharing capabilities

## Key Business Requirements Alignment Analysis

### ✅ **WELL COVERED** in Existing Wireframes

1. **Sliding Scale Pricing**: Perfectly implemented in class detail page
2. **PayPal Integration**: Checkout flow ready for existing webhook system
3. **Role Stacking**: Navigation can handle multiple roles per user
4. **Mobile Responsive**: All wireframes are mobile-optimized
5. **Design System v7**: All wireframes follow approved color scheme and typography
6. **Capacity Display**: Visual progress bars show availability clearly

### ❌ **MAJOR GAPS** Requiring New Wireframes

1. **Social Event RSVP Flow**: No RSVP-specific interface exists
2. **Role-Based Event Access**: No UI for blocking General Members from social events
3. **Optional Ticket Purchase**: No interface for purchasing tickets after RSVP
4. **Banned User Blocking**: No error states for banned users
5. **Dual Participation Tracking**: No distinction between RSVP and ticket in UI
6. **User Dashboard Integration**: No "Upcoming Events" and "My Tickets" sections

### ⚠️ **MINOR ENHANCEMENTS** Needed

1. **Automatic RSVP Creation**: UI should indicate when ticket purchase includes RSVP
2. **Canceled Participation Display**: Admin views need to show canceled RSVPs/tickets
3. **Terminology Consistency**: Ensure "Purchase Ticket" is used everywhere
4. **$0 Transaction Handling**: Checkout flow needs RSVP-only path

## Missing Wireframes Required

### 1. **Social Event Detail Page with RSVP**
- **Purpose**: Show social events with RSVP button (free) and optional ticket purchase
- **Role Logic**:
  - Vetted Members: See "RSVP Now" + "Purchase Optional Ticket ($X suggested donation)"
  - General Members: See "Vetting Required" message
  - Banned Users: See "Access Denied" message
- **Components Needed**: Role-based CTAs, optional ticket section, vetting requirements

### 2. **User Dashboard: My Events**
- **Purpose**: Show user's RSVPs and tickets separately
- **Sections**:
  - Upcoming Events (RSVPs)
  - My Tickets (Paid participation)
  - Past Events (with canceled items shown with status)
- **Components Needed**: Event cards, status badges, cancel buttons

### 3. **Admin Event Management: Participants Tab**
- **Purpose**: Show all RSVPs and tickets for an event
- **Features**:
  - Filter: Active/Canceled participation
  - Export functionality
  - Participant details with role indicators
- **Components Needed**: Data table, filters, export controls, status indicators

### 4. **RSVP Confirmation Flow**
**🚨 CORRECTED: Social events have TWO separate flows**
- **Flow 1 - RSVP Only**: RSVP → Email confirmation → Success (bypass payment)
- **Flow 2 - Optional Ticket**: RSVP → Purchase Ticket ($X) → Payment → Success
- **Components Needed**: Simplified RSVP flow, optional upgrade to ticket purchase

## Enhanced Component Requirements

### Event Details Page Enhancement

#### Role-Based Call-to-Action Logic
```typescript
interface EventCTAProps {
  eventType: 'SocialEvent' | 'Class' | 'Workshop';
  userRoles: string[];
  userStatus: 'Active' | 'Banned';
  currentParticipation: {
    hasRSVP: boolean;
    hasTicket: boolean;
    status: 'Active' | 'Canceled';
  };
}

// CTA Display Logic:
// Social Events:
//   - Banned: "Access Denied"
//   - General Member: "Vetting Required - Learn More"
//   - Vetted Member (no RSVP): "RSVP Now (Free)" + "Purchase Ticket ($X)"
//   - Vetted Member (has RSVP): "Purchase Ticket ($X)" + "Cancel RSVP"
//   - Vetted Member (has Ticket): "RSVPed ✓" + "Cancel Ticket"

// Classes:
//   - Banned: "Access Denied"
//   - Any Active Member: "Purchase Ticket ($X-$Y)"
//   - Has Ticket: "RSVPed ✓" + "Cancel Ticket"
```

#### Participation Status Indicators
- **RSVP Status**: Green badge "RSVP Confirmed"
- **Ticket Status**: Blue badge "Ticket Purchased"
- **Combined Status**: Purple badge "Fully RSVPed" (when both)
- **Canceled Status**: Gray badge "Canceled" (visible in admin views)

### User Dashboard Enhancement

#### Event Card Component
```jsx
<EventCard>
  <EventImage />
  <EventInfo>
    <EventTitle />
    <EventDate />
    <EventLocation />
  </EventInfo>
  <ParticipationStatus>
    <StatusBadge type="rsvp|ticket|both" status="active|canceled" />
    <CancelButton if="cancellable" />
  </ParticipationStatus>
</EventCard>
```

#### Dashboard Sections
- **Upcoming Events**: Shows events with active RSVPs or tickets
- **Past Events**: Shows completed events with participation history
- **Canceled**: Shows canceled RSVPs/tickets for reference

### Admin Participant Management

#### Participant Table Columns
1. **Name** (with role indicators)
2. **Participation Type** (RSVP/Ticket/Both)
3. **Payment Amount** (if applicable)
4. **Status** (Active/Canceled)
5. **RSVP Date**
6. **Actions** (View Details/Contact)

#### Filter Controls
- **Participation Type**: All, RSVP Only, Ticket Only, Both
- **Status**: Active, Canceled, All
- **Role**: All Roles, Vetted Only, Teachers Only

## Design System Integration

### Color Coding for Participation Types
- **RSVP**: `var(--color-success)` (#228B22) - Green for free participation
- **Ticket**: `var(--color-burgundy)` (#880124) - Burgundy for paid participation
- **Both**: `var(--color-electric)` (#9D4EDD) - Purple for complete RSVP
- **Canceled**: `var(--color-stone)` (#8B8680) - Gray for canceled items

### Typography Hierarchy
- **Event Titles**: `var(--font-heading)` Montserrat, 24px, 700 weight
- **Participation Status**: `var(--font-heading)` Montserrat, 14px, 600 weight, uppercase
- **Event Details**: `var(--font-body)` Source Sans 3, 16px, 400 weight
- **Status Messages**: `var(--font-body)` Source Sans 3, 14px, 500 weight

### Button Patterns (🚨 CRITICAL FIX: Use Standard Mantine Components)
**Reference**: `/docs/lessons-learned/react-developer-lessons-learned.md` - Button text cutoff prevention

```jsx
// ✅ CORRECT - Use standard CSS classes to prevent text cutoff
<button className="btn btn-primary">RSVP Now (Free)</button>
<button className="btn btn-secondary">Purchase Ticket ($X)</button>
<button className="btn btn-secondary">Cancel RSVP</button>

// ❌ WRONG - Inline styles cause text cutoff issues
<Button style={{...customStyles}}>Text gets cut off</Button>
```

- **Primary RSVP**: Use `.btn .btn-primary` CSS classes (amber gradient)
- **Secondary Ticket Purchase**: Use `.btn .btn-secondary` CSS classes (burgundy outline)
- **Cancel Actions**: Use `.btn .btn-secondary` with rose-gold styling
- **Admin Actions**: Use `.btn .btn-primary` with burgundy solid

## Accessibility Considerations

### Role-Based Access
- **Screen Reader Support**: Clear announcement of access level and available actions
- **Keyboard Navigation**: All CTAs accessible via tab navigation
- **Error States**: Clear, descriptive messages for access restrictions

### Status Indicators
- **Color + Text**: Never rely on color alone for status
- **Icon Support**: Use iconography alongside color coding
- **High Contrast**: Ensure 4.5:1 contrast ratio for all status elements

## Mobile Optimization Requirements

### Event Detail Page
- **Responsive CTA Buttons**: Full width on mobile, maintain touch targets 44px+
- **Collapsed Info Sections**: Use accordions for event details on small screens
- **Sticky RSVP Card**: Bottom-fixed CTA on mobile for easy access

### Dashboard
- **Card Layout**: Single column stack on mobile
- **Swipe Actions**: Enable swipe-to-cancel on mobile event cards
- **Filter Drawer**: Collapsible filter sidebar for mobile

### Admin Tables
- **Responsive Tables**: Horizontal scroll with sticky first column
- **Mobile Actions**: Long-press or dedicated action column for mobile
- **Bulk Actions**: Checkbox selection with bottom action bar

## Implementation Priority

### Phase 1: Critical Missing Components (Week 1)
1. Social Event RSVP interface with role-based CTAs
2. $0 RSVP confirmation flow
3. Basic user dashboard with event sections

### Phase 2: Enhanced Functionality (Week 2)
1. Optional ticket purchase after RSVP
2. Admin participant management tab
3. Cancel RSVP/ticket functionality

### Phase 3: Polish & Enhancement (Week 3)
1. Advanced filtering and export
2. Mobile gesture support
3. Enhanced accessibility features

## Conclusion

The existing wireframes provide an excellent foundation for the ticketing system, particularly for paid class RSVP. However, significant gaps exist for the social event RSVP functionality and role-based access control. The simplified business requirements (single RSVP per user, role stacking, optional tickets) actually reduce complexity compared to the original sophisticated designs.

**🚨 CRITICAL STAKEHOLDER CORRECTIONS APPLIED**:
- ✅ All "register" terminology replaced with "RSVP"
- ✅ Social event flow shows RSVP button first, then optional ticket purchase
- ✅ Button implementation uses standard CSS classes to prevent text cutoff
- ✅ Clear distinction between social event UI (RSVP + ticket) vs class UI (ticket purchase)

**Next Steps**: Create the missing wireframes focusing on social event RSVP flow and user dashboard integration, while leveraging the strong existing patterns for the checkout and confirmation processes.