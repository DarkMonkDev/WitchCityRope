<!-- Last Updated: 2025-12-01 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Research Complete - Ready for Design Phase -->

# UI Analysis Handoff: Email Template Trigger Enhancements

## Phase: UI Research & Analysis
## Date: 2025-12-01
## Feature: Email Templates - Fixed Event and Time-Based Trigger Enhancement

---

## 🎯 CRITICAL CONTEXT FOR DESIGNERS & DEVELOPERS

This document provides UI/UX research findings for the email template trigger enhancement feature. The feature adds two new trigger types to the existing email templates admin interface:

1. **Fixed Event Triggers** - Templates fire when specific events occur (ticket purchase, cancellation, password reset)
2. **Time-Based Triggers** - Templates fire X days before/after session (negative numbers for post-event surveys)
3. **Recipient Group Selection** - Different recipient options per trigger type and tab

---

## 📋 CURRENT STATE ANALYSIS

### Existing Email Templates Feature (from PROGRESS.md)
- **Status**: Phase 1 - Requirements (Just Initialized as of 2025-11-09)
- **Scope**: Centralized admin UI for managing global email templates across all categories
- **Categories**: Vetting, Events, Admin, Incident, Ad Hoc
- **Features**: Template CRUD operations, variable substitution system, template preview, event-level customization overrides
- **Pattern Reference**: Similar architecture to CMS admin interface (in-place editing, card-based UI, admin-only dashboard)

### Technology Stack
- **Framework**: React + TypeScript + Vite
- **UI Library**: Mantine v7 (ADR-004) - AUTHORITATIVE for all UI component choices
- **Rich Text Editor**: @mantine/tiptap (migrated from TinyMCE on Oct 8, 2025) with variable insertion support
- **Design System**: Design System v7 (current authority) - Burgundy/plum gradient brand colors
- **Form Validation**: Mantine Form + React Hook Form + Zod
- **State Management**: React hooks + TanStack Query (React Query)

---

## 🏗️ RECOMMENDED UI ARCHITECTURE

### Overall Layout Pattern
Based on existing admin patterns, recommend **tabbed interface with progressive disclosure**:

```
┌─────────────────────────────────────────────────────────────┐
│  Admin Email Templates                                      │
│  Centrally manage templates across all categories            │
├─────────────────────────────────────────────────────────────┤
│  [ Events ]  [ Vetting ]  [ Admin ]  [ Incident ]  [ Ad Hoc ] │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Search & Filters:  [Search...]  [Trigger: Fixed Event ▼]  │
│                                                              │
│  ┌─────────────────────────────────────┐                   │
│  │ Template Name: "Ticket Purchased"   │                   │
│  │ Status: Published                   │                   │
│  │ Trigger: Fixed Event → Purchase     │ [Edit] [Delete]   │
│  │ Recipients: Registered Members      │                   │
│  └─────────────────────────────────────┘                   │
│                                                              │
│  ┌─────────────────────────────────────┐                   │
│  │ Template Name: "Session Reminder"   │                   │
│  │ Status: Published                   │                   │
│  │ Trigger: Time-Based → 3 days before │ [Edit] [Delete]   │
│  │ Recipients: Registered Attendees    │                   │
│  └─────────────────────────────────────┘                   │
│                                                              │
│  [+ Create Template]                                        │
└─────────────────────────────────────────────────────────────┘
```

### Tab-Specific Organization
Following lessons learned about progressive disclosure and visual hierarchy:

#### Events Tab
- **Trigger Types**: Fixed Event triggers + Time-Based triggers (session timing)
- **Fixed Event Options**:
  - Ticket Purchased
  - Ticket Cancelled
  - RSVP Confirmed
  - RSVP Cancelled
- **Time-Based Options**:
  - X days before session
  - X days after session
- **Recipient Groups**:
  - Registered Members
  - Session Attendees
  - Ticket Purchasers
  - RSVP Confirmedees

#### Vetting Tab
- **Trigger Types**: Fixed Event triggers (no time-based - vetting is status-driven)
- **Fixed Event Options**:
  - Application Submitted
  - Application Approved
  - Application Rejected
  - Application Reopened
  - Status Changed
- **Recipient Groups**:
  - Applicants
  - Approved Members
  - Admin Reviewers
  - Safety Coordinators

#### Admin Tab
- **Trigger Types**: Fixed Event triggers (system events)
- **Fixed Event Options**:
  - User Registered
  - Password Reset Requested
  - Member Deactivated
  - Email Verification Needed
  - Account Suspended
- **Recipient Groups**:
  - All Admin Users
  - Safety Coordinators
  - Specific User Segment

#### Incident Tab
- **Trigger Types**: Fixed Event triggers
- **Fixed Event Options**:
  - Incident Reported
  - Investigation Started
  - Resolution Documented
- **Recipient Groups**:
  - Safety Team
  - Community Leaders
  - Affected Parties

#### Ad Hoc Tab
- **Trigger Types**: None (manual send only)
- **Purpose**: One-time broadcast emails sent manually by admins
- **Recipient Groups**: Varies per send (filtered by segment/role)

---

## 🎨 TEMPLATE CARD ENHANCEMENT RECOMMENDATIONS

### Current Card Structure (Base)
- Template Name
- Subject line (preview)
- Status badge
- Edit/Delete actions

### Enhanced Card Structure (With Triggers)
Using **card header gradient pattern** from lessons learned (admin settings pattern):

```
┌────────────────────────────────────────────────────┐
│ 🎟️ Ticket Purchased                      Published │  ← Header gradient
├────────────────────────────────────────────────────┤
│ Subject: Thank you for purchasing!                │
│                                                    │
│ Trigger Type: Fixed Event                          │
│ Event: Ticket Purchased                            │
│ Recipients: Ticket Purchasers                      │
│                                                    │
│ [Preview]  [Edit]  [Delete]                       │
└────────────────────────────────────────────────────┘
```

### Mantine Components for Cards
- **Card Container**: `Mantine Card` component with shadow
- **Header**: Gradient Box with icon + title (burgundy/plum gradient from Design System v7)
- **Badge**: Status indicator (Published, Draft, Disabled)
- **Trigger Info**: `Group` + `Text` components showing trigger type and details
- **Actions**: `Group` with `ActionIcon` for edit/delete, `Button` for preview

### Card Variant: Time-Based Trigger
```
┌────────────────────────────────────────────────────┐
│ 📅 Session Reminder                        Published │
├────────────────────────────────────────────────────┤
│ Subject: Workshop starts in 3 days                 │
│                                                    │
│ Trigger Type: Time-Based                           │
│ Timing: 3 days before session                      │
│ Recipients: Session Attendees                      │
│                                                    │
│ [Preview]  [Edit]  [Delete]                       │
└────────────────────────────────────────────────────┘
```

---

## 📝 FORM ENHANCEMENTS FOR TEMPLATE CREATION/EDITING

### Current Form Elements (Baseline)
- Template name (TextInput)
- Subject line (TextInput)
- Template body (MantineTiptapEditor with variable insertion)
- Status (Select: Draft/Published/Disabled)
- Category/Tab (Select)

### NEW Form Elements Required

#### Trigger Type Selection (Radio or Select)
```tsx
// Radio group pattern (clearer for 2 options)
<Radio.Group
  label="Trigger Type"
  description="How should this template be sent?"
  required
>
  <Radio value="fixed-event" label="Fixed Event (triggered by specific events)" />
  <Radio value="time-based" label="Time-Based (triggered X days before/after session)" />
</Radio.Group>
```

#### Fixed Event Trigger Setup (Conditionally Visible)
**Shows when "Fixed Event" is selected**

```tsx
// Dropdown pattern with visual hierarchy
<Select
  label="Event Type"
  placeholder="Select the event that triggers this template"
  data={triggerEventOptions}  // Tab-specific options
  searchable
  clearable={false}
  required
/>
```

#### Time-Based Trigger Setup (Conditionally Visible)
**Shows when "Time-Based" is selected**

```tsx
// Offset configuration
<Group gap="md" align="flex-end">
  <NumberInput
    label="Days Before/After"
    description="Use negative numbers for post-event (e.g., -7 for 7 days after)"
    min={-365}
    max={365}
    required
    placeholder="0"
  />
  <Text size="sm" color="dimmed">days relative to session</Text>
</Group>

<Select
  label="Session Field"
  placeholder="Which session timing should trigger this?"
  data={[
    { value: 'start-time', label: 'Session Start Time' },
    { value: 'end-time', label: 'Session End Time' }
  ]}
  required
/>
```

#### Recipient Group Selection (Conditionally Visible)
**Options change based on tab context**

```tsx
// Select with grouped options (visual hierarchy)
<Select
  label="Send To"
  placeholder="Select recipient group"
  data={recipientGroupOptions}  // Tab-specific, grouped
  searchable
  clearable={false}
  required
/>

// Example for Events tab:
// [Registered Members, Session Attendees, Ticket Purchasers, RSVP Confirmedees]
```

---

## 🎯 INTERACTION PATTERNS & PROGRESSIVE DISCLOSURE

### Form Visibility Rules
Using **conditional form visibility pattern** from lessons learned:

1. **Initial State**: Trigger Type selection visible (radio group)
2. **After Selection**: Show relevant form fields below
3. **On Field Change**: Update available options (e.g., recipient groups change per tab)

### Implementation Pattern
```tsx
const [triggerType, setTriggerType] = useState<'fixed-event' | 'time-based' | null>(null);

// Render
{triggerType === 'fixed-event' && <FixedEventTriggerForm />}
{triggerType === 'time-based' && <TimeBasedTriggerForm />}
```

### Button State Management
- **Create Mode**: Single "Create Template" button (primary CTA)
- **Edit Mode**: "Delete Template" (secondary) + "Update Template" (primary) buttons
- **Follow pattern**: Destructive on left, save on right

---

## 📊 MANTINE COMPONENTS REFERENCE

| UI Element | Component | Configuration | Notes |
|-----------|-----------|-----------------|-------|
| Template Cards | `Card` + `Box` | Gradient header, shadow | Use burgundy/plum gradient from Design System v7 |
| Header Gradient | `Box` | `linear-gradient(135deg, var(--color-burgundy)...)` | From admin settings pattern |
| Tab Interface | `Tabs` | Default selected tab (Events) | Standard Mantine tabs component |
| Trigger Type | `Radio.Group` or `Select` | 2 options | Radio is clearer, Select is more compact |
| Event Trigger | `Select` | Tab-specific options, searchable | Use grouped options for visual hierarchy |
| Timing Offset | `NumberInput` | Range -365 to 365 | Allow decimals for hours? (future consideration) |
| Session Field | `Select` | 2-3 options max | Specific to session timing |
| Recipient Group | `Select` | 4-8 options per tab | Grouped for readability |
| Form Buttons | `Button` groups | `Group` layout responsive | Flex on desktop, stack on mobile (<768px) |
| Status Badge | `Badge` | Published/Draft/Disabled | Color-coded by status |
| Icons | Tabler Icons | 24px size | Use relevant icons per trigger type |
| Modals | `Modal` | Centered, medium size | For confirmations and previews |
| Notifications | `notifications` API | Top-right placement | Follow existing pattern (3s success, 5s error) |

---

## 📱 RESPONSIVE DESIGN CONSIDERATIONS

### Desktop (≥768px)
- Two-column card grid layout (if multiple templates shown)
- Full form with all fields visible (progressively)
- Horizontal button layout (right-aligned)
- Full table view for list view

### Mobile (<768px)
- Single column card layout
- Simplified form (same fields, but stacked)
- Full-width buttons, stacked vertically
- Card view preferred over table (less horizontal scroll)

### Touch Targets
Following mobile-first accessibility standards:
- Minimum 44×44px for buttons (Mantine provides this)
- Dropdown/select fields: 48px minimum height
- Form inputs: 44px minimum height

---

## 🎨 DESIGN SYSTEM INTEGRATION

### Color Palette Usage
From **Design System v7**:
- **Card Headers**: Burgundy/Plum gradient (`linear-gradient(135deg, #880124, #614B79)`)
- **Primary Buttons**: Gold/Amber gradient (class: `btn btn-primary`)
- **Secondary Buttons**: Burgundy outline (class: `btn btn-secondary`)
- **Status Badges**: Color-coded (success: #228B22, published: #4A4A4A)
- **Text**: Charcoal (#2B2B2B) for primary, Smoke (#4A4A4A) for secondary
- **Disabled State**: Stone (#8B8680)

### Typography
- **Card Titles**: Montserrat 700, 20px, uppercase, 1px letter-spacing
- **Labels**: Montserrat 600, 14px, uppercase, 0.5px letter-spacing
- **Body Text**: Source Sans 3 400, 16px, 1.7 line-height

### Spacing
Using Mantine spacing scale:
- **xs**: 8px (fine details)
- **sm**: 16px (component padding)
- **md**: 24px (related elements)
- **lg**: 32px (component spacing)
- **xl**: 40px (section spacing)

### Button Styling
All buttons use signature corner morphing animation:
- **Default**: `border-radius: 12px 6px 12px 6px`
- **Hover**: `border-radius: 6px 12px 6px 12px`
- **Transition**: `all 0.3s ease`

---

## 🚨 CRITICAL UX CONCERNS & OPPORTUNITIES

### 1. Trigger Event Naming Clarity
**Concern**: "Fixed Event Triggers" terminology might confuse users.
**Recommendation**: Use context-specific labels in UI:
- For Events tab: "Event Triggers" (Ticket Purchased, RSVP Confirmed, etc.)
- For Vetting tab: "Application Triggers" (Status Changed, Approved, etc.)
- For Admin tab: "System Event Triggers" (User Registered, etc.)

**Solution**: Label the trigger type section with tab-specific description:
```
"When should this template be sent?"
- ○ When a specific event occurs (Fixed Event)
- ○ X days before/after a session (Time-Based)
```

### 2. Time-Based Trigger Offset Clarity
**Concern**: Negative numbers for post-event timing might be confusing.
**Recommendation**: Provide visual explanation and validation feedback:
```
"Days Before/After Session"
"Positive = before (e.g., 3 = send 3 days before)"
"Negative = after (e.g., -7 = send 7 days after)"
```

### 3. Recipient Group Discoverability
**Concern**: Different tabs have different recipient group options - users may not realize this.
**Recommendation**: Add context to recipient group selector:
- Show icon or small label indicating "Available for this template type"
- Add helper text: "Recipients must have this role/status to receive the email"

### 4. Template Preview with Dynamic Timing
**Opportunity**: For time-based triggers, preview should show an example of when email would be sent:
```
"This template will send 3 days before the session"
"Example: If session starts Dec 15, email sends Dec 12"
```

### 5. Bulk Trigger Management
**Opportunity for future**: If many templates share same trigger:
- Ability to filter templates by trigger type
- Ability to enable/disable all templates of one trigger type
- Trigger management dashboard

---

## ✅ IMPLEMENTATION CHECKLIST FOR DESIGNERS

### Card Design Phase
- [ ] Create card mockup with gradient header
- [ ] Design trigger type badge/indicator
- [ ] Create card variants (Fixed Event, Time-Based, Ad Hoc)
- [ ] Design icon usage for different trigger types
- [ ] Verify color contrast (4.5:1 minimum, 7:1 target)

### Form Design Phase
- [ ] Design radio group for trigger type selection
- [ ] Create conditional form field layouts
- [ ] Design number input with helper text for timing offset
- [ ] Design select dropdowns with grouped options
- [ ] Design form validation error states
- [ ] Create mobile layout (stacked form)

### Interaction Design Phase
- [ ] Document progressive disclosure flow
- [ ] Create button state transitions
- [ ] Design loading states for preview/save
- [ ] Design error/success notifications
- [ ] Document keyboard navigation order

### Accessibility Phase
- [ ] Verify form label associations
- [ ] Check focus indicators on form fields
- [ ] Verify color contrast for all elements
- [ ] Test keyboard navigation (Tab order)
- [ ] Verify ARIA labels on custom components
- [ ] Test with screen reader

---

## 🔗 KEY DOCUMENTS FOR IMPLEMENTATION TEAM

| Document | Path | Critical Sections | Why Needed |
|----------|------|-------------------|-----------|
| Design System v7 | `/docs/design/current/design-system-v7.md` | Color palette, typography, button types | Authority for colors and styling |
| Button Style Guide | `/docs/design/current/button-style-guide.md` | All button types, corner animation | Implement signature button styles correctly |
| Mantine UI Standards | `/docs/standards-processes/frontend/mantine-ui-standards.md` | Component patterns, responsive context | Mantine v7 usage rules |
| Admin Settings Pattern | `/docs/lessons-learned/ui-designer-lessons-learned.md` (lines 118-566) | Card headers, conditional visibility, form patterns | Proven patterns for admin interfaces |
| Forms Standardization | `/docs/standards-processes/forms-standardization.md` | Form validation, error handling, input patterns | Form implementation standards |
| React Patterns | `/docs/standards-processes/frontend/react-patterns.md` | Hooks, component structure | React implementation standards |
| Design System Tokens | `/docs/design/current/design-system-v7.md` (lines 48-114) | CSS variables, color definitions | Exact colors to use |

---

## 📚 EXAMPLE REFERENCE IMPLEMENTATIONS

### Similar Admin Interfaces in Project
1. **Venue Management** (Lessons Learned, lines 200-282)
   - Dropdown with create/edit modes
   - Conditional form visibility
   - Two-column grid layout
   - Patterns directly applicable

2. **CMS Admin Interface** (Lessons Learned, lines 569-721)
   - Card-based layout
   - Always-visible action buttons
   - Edit modal with save/cancel
   - Patterns for content management

3. **Admin Settings Cards** (Lessons Learned, lines 118-166)
   - Gradient header pattern
   - Professional card styling
   - Consistent spacing
   - Directly inspirational for this feature

---

## 🎯 SUCCESS CRITERIA FOR UI DESIGN PHASE

Design phase is complete when:

1. ✅ Card mockups created for all trigger types
   - Fixed Event trigger card designed
   - Time-Based trigger card designed
   - Visual hierarchy clear and consistent

2. ✅ Form layouts designed for both desktop and mobile
   - Progressive disclosure documented
   - Conditional fields clearly marked
   - Form validation states shown

3. ✅ All Mantine components specified
   - Component list with configuration documented
   - CSS classes identified
   - Responsive behavior defined

4. ✅ Interaction patterns documented
   - User flows from create to save
   - Error scenarios handled
   - Loading/success states designed

5. ✅ Accessibility verified
   - WCAG 2.1 AA compliance checklist completed
   - Focus indicators designed
   - Color contrast verified

6. ✅ Design System integration complete
   - All colors match Design System v7
   - Typography follows standards
   - Spacing uses Mantine scale
   - Button styles match guide

---

## 🚀 HANDOFF TO DESIGN PHASE

### Design Phase Deliverables Expected
1. **Wireframes** - Low-fidelity layout sketches showing all form states
2. **High-Fidelity Mockups** - Pixel-perfect designs using Design System v7 colors
3. **Responsive Mockups** - Desktop (1440px) and Mobile (375px) layouts
4. **Interaction Documentation** - Click-through flows showing progressive disclosure
5. **Component Specification** - Exact Mantine component selection with config
6. **Design System Checklist** - Verification that all colors/typography match standards

### Design Phase Assumptions
- **Team knows Mantine v7** - Component-specific design patterns
- **Team has access to Design System v7** - Current colors/typography authority
- **Admin-only feature** - Desktop optimization acceptable, mobile optional
- **Similar to Venue Management** - Can use existing patterns as inspiration
- **Trigger options provided separately** - This analysis documents what options exist per tab

---

## 📞 QUESTIONS FOR PRODUCT/STAKEHOLDERS (If Design Phase Encounters Decisions)

1. **Time-Based Triggers**: Can offset be fractional (e.g., 1.5 days = 36 hours)?
2. **Recipient Filtering**: Should emails be filtered per-recipient within a group (e.g., only send to attendees aged 18+)?
3. **Template Variations**: Should time-based triggers support different emails for "near" vs "far" future sends?
4. **Preview Rendering**: Should preview show with example variable values or placeholders?
5. **Bulk Operations**: Future - Enable/disable multiple templates at once?
6. **Analytics**: Future - Track template sends per trigger type?

---

## 📝 NOTES FOR NEXT SESSION

### Discovered Patterns to Reuse
- **Venue Management pattern** (dropdown + conditional form): Directly applicable
- **Admin settings pattern** (gradient card headers): Proven effective
- **CMS pattern** (card-based layout with actions): Familiar to team
- **Form validation pattern** (inline errors + helper text): Consistent approach

### Design Decisions Made During Research
- **Card-based layout preferred**: Better visual hierarchy than table for mixed content
- **Progressive disclosure recommended**: Show only relevant fields per trigger type
- **Gradient headers**: Matches established admin UI pattern
- **Mantine Select for dropdowns**: Consistent with existing forms
- **Radio group for trigger type**: Clearer than Select for binary choice

### Assumptions Made in This Analysis
- **Tab interface**: Based on existing template categories (Vetting, Events, Admin, etc.)
- **Desktop-first design**: Admin-only interface, no mobile optimization required (per standards)
- **Existing variable insertion**: Already supported by MantineTiptapEditor
- **Status management**: Published/Draft/Disabled states continue existing pattern

---

**RESEARCH COMPLETE**: This analysis provides comprehensive UI/UX guidance. Design phase can now proceed with confidence about component choices, interaction patterns, and integration with existing admin interfaces.

**Next Step**: Design phase to create wireframes and high-fidelity mockups based on this analysis.
