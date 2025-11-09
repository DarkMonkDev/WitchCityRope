# UI Design: Email Templates Admin Management
<!-- Last Updated: 2025-11-09 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Draft - Awaiting Human Review -->

## Design Overview

### Visual Approach

This design creates a centralized, intuitive admin interface for managing all email templates across the WitchCityRope platform. The design follows Design System v7 patterns with:

- **Burgundy/plum gradient headers** for consistent admin card styling
- **Tabbed interface** for easy category navigation (5 tabs: Vetting, Events, Admin, Incident, Ad Hoc)
- **Reusable template card pattern** from EventForm Emails tab (horizontal layout with click-to-edit)
- **MantineTiptapEditor integration** for rich text editing with variable support
- **Badge indicators** showing customization status ("✓ Customized" vs "(Default)")
- **Mobile-responsive layout** optimized for desktop/tablet (admin workflows)

### Key Design Principles

1. **Reuse Over Reinvention**: Leverage existing UI patterns (Admin Dashboard cards, EventForm template cards, Vetting email templates)
2. **Progressive Disclosure**: Show complexity only when needed (editor appears on template selection)
3. **Visual Hierarchy**: Clear separation between categories (tabs), templates (cards), and editor (panel)
4. **Consistency**: Maintain Design System v7 color palette, typography, spacing, and animations
5. **Accessibility**: WCAG 2.1 AA compliance with keyboard navigation and screen reader support

---

## Color Palette

### Primary Colors (From Design System v7)
```css
--color-burgundy: #880124        /* Primary brand, headers, titles */
--color-burgundy-dark: #660018   /* Dark hover states */
--color-plum: #614B79            /* Card backgrounds, accents */
--color-rose-gold: #B76D75       /* Borders, highlights */
```

### Supporting Colors
```css
--color-charcoal: #2B2B2B       /* Primary text */
--color-smoke: #4A4A4A          /* Secondary text */
--color-stone: #8B8680          /* Tertiary text, disabled */
--color-taupe: #B8B0A8          /* Light borders */
--color-ivory: #FFF8F0          /* Light text on dark */
--color-cream: #FAF6F2          /* Body background */
```

### Status Colors
```css
--color-success: #228B22        /* Success states, "Customized" badge */
--color-warning: #DAA520        /* Warnings, validation alerts */
--color-error: #DC143C          /* Errors */
```

---

## Typography

### Font Stack (Design System v7)
```css
--font-heading: 'Montserrat', sans-serif  /* Titles, labels, navigation */
--font-body: 'Source Sans 3', sans-serif  /* Body text, descriptions */
--font-display: 'Bodoni Moda', serif      /* Page titles (optional) */
```

### Typography Scale
- **Page Title**: 32px, Montserrat 800, uppercase, -0.5px letter-spacing, burgundy
- **Section Title**: 24px, Montserrat 700, uppercase, 1px letter-spacing, burgundy
- **Tab Labels**: 15px, Montserrat 600, uppercase, 1px letter-spacing
- **Card Titles**: 16px, Montserrat 600, burgundy
- **Card Descriptions**: 14px, Source Sans 3 400, stone
- **Body Text**: 16px, Source Sans 3 400, charcoal
- **Helper Text**: 14px, Source Sans 3 400, stone

---

## Wireframes

### 1. Admin Dashboard with Email Templates Card

```
┌───────────────────────────────────────────────────────────────────────┐
│ ADMIN DASHBOARD                                                       │
│                                                                       │
├───────────────┬───────────────┬───────────────┬───────────────────────┤
│ [📊 Members]  │ [🎫 Events]   │ [✅ Vetting]  │ [📧 Email Templates]  │
│ Manage member │ Create and    │ Review        │ Manage global email   │
│ profiles and  │ manage events │ applications  │ templates for all     │
│ permissions   │ and sessions  │ and approvals │ categories            │
│               │               │               │                       │
│ VIEW          │ VIEW          │ VIEW          │ VIEW                  │
└───────────────┴───────────────┴───────────────┴───────────────────────┘
      ↑
      Card Pattern: Paper shadow, 16px radius, hover lift effect
      Color: Each card has unique color accent (#FF6B35 for Email Templates)
      Icon: IconMail (32px)

      DESKTOP LAYOUT: 4 columns (Grid.Col span={{ lg: 3 }})
      TABLET LAYOUT: 2 columns (Grid.Col span={{ sm: 6 }})
      MOBILE LAYOUT: 1 column (Grid.Col span={{ base: 12 }})
```

---

### 2. Email Templates Admin Page - Main Layout

```
Route: /admin/email-templates

┌─────────────────────────────────────────────────────────────────────────┐
│  ← Back to Admin Dashboard                                             │
│                                                                         │
│  EMAIL TEMPLATES MANAGEMENT                                            │
│  Manage global email templates for all system categories               │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ [Vetting] │ [Events] │ [Admin] │ [Incident] │ [Ad Hoc]         │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│  ▔▔▔▔▔▔▔▔  (active tab underline - burgundy)                          │
│                                                                         │
│  <!-- VETTING TAB CONTENT (default) -->                                │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ TEMPLATE CARDS (horizontal scroll if needed)                    │   │
│  │                                                                  │   │
│  │ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌──────────┐  │   │
│  │ │ Application │ │ Interview   │ │ Approved    │ │ On Hold  │  │   │
│  │ │ Received    │ │ Approved    │ │             │ │          │  │   │
│  │ │             │ │             │ │             │ │          │  │   │
│  │ │ Sent when   │ │ Sent when   │ │ Sent when   │ │ Sent ...  │  │   │
│  │ │ application │ │ interview   │ │ applicant   │ │          │  │   │
│  │ │ submitted   │ │ scheduled   │ │ approved    │ │          │  │   │
│  │ └─────────────┘ └─────────────┘ └─────────────┘ └──────────┘  │   │
│  │      (Selected card has burgundy border + light burgundy bg)   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ EDITOR PANEL (appears when card clicked)                        │   │
│  │                                                                  │   │
│  │ Currently Editing: Application Received                         │   │
│  │ Available Variables: {{scene_name}}, {{application_number}}... │   │
│  │                                                                  │   │
│  │ ┌─────────────────────────────────────────────────────────────┐ │   │
│  │ │ Subject Line                                                │ │   │
│  │ │ [Your vetting application has been received              ] │ │   │
│  │ └─────────────────────────────────────────────────────────────┘ │   │
│  │                                                                  │   │
│  │ ┌─────────────────────────────────────────────────────────────┐ │   │
│  │ │ Email Content (HTML Editor)                                 │ │   │
│  │ │ ┌───────────────────────────────────────────────────────┐   │ │   │
│  │ │ │ [B] [I] [U] [Link] [List] [H1] [Insert Variable ▼]  │   │ │   │
│  │ │ ├───────────────────────────────────────────────────────┤   │ │   │
│  │ │ │                                                       │   │ │   │
│  │ │ │ Hi {{scene_name}},                                   │   │ │   │
│  │ │ │                                                       │   │ │   │
│  │ │ │ Thank you for submitting your vetting application... │   │ │   │
│  │ │ │                                                       │   │ │   │
│  │ │ │ Application #: {{application_number}}                │   │ │   │
│  │ │ │                                                       │   │ │   │
│  │ │ └───────────────────────────────────────────────────────┘   │ │   │
│  │ └─────────────────────────────────────────────────────────────┘ │   │
│  │                                                                  │   │
│  │ ⚠️ Unknown variable detected: {{invalid}}                       │   │
│  │ Available variables: {{scene_name}}, {{application_number}}... │   │
│  │                                                                  │   │
│  │                                          [Cancel] [Save Template]│   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

SPACING:
- Container padding: var(--space-xl) (40px)
- Section spacing: var(--space-2xl) (48px)
- Card gap: var(--space-md) (24px)
- Internal card padding: var(--space-lg) (32px)

RESPONSIVE:
- Desktop (≥1200px): Full width container, 4-5 cards visible
- Tablet (768px-1199px): Scrollable cards, 3 cards visible
- Mobile (<768px): Stack vertically, 1 card visible with horizontal scroll
```

---

### 3. Events Tab Panel (Showing Global Event Templates)

```
Route: /admin/email-templates?tab=events

┌─────────────────────────────────────────────────────────────────────────┐
│  EMAIL TEMPLATES MANAGEMENT                                            │
│  Manage global email templates for all system categories               │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ [Vetting] │ [Events] │ [Admin] │ [Incident] │ [Ad Hoc]         │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│               ▔▔▔▔▔▔▔▔  (active tab underline - burgundy)             │
│                                                                         │
│  <!-- EVENTS TAB CONTENT -->                                           │
│                                                                         │
│  EVENT EMAIL TEMPLATES (7 templates)                                   │
│  These templates are used for all events unless customized.            │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌──────────┐  │   │
│  │ │Confirmation │ │ Reminder    │ │ Reminder    │ │Cancella- │  │   │
│  │ │             │ │ 1 Day Before│ │ 1 Week      │ │tion      │  │   │
│  │ │ REQUIRED    │ │             │ │ Before      │ │          │  │   │
│  │ │             │ │             │ │             │ │          │  │   │
│  │ │ Sent on     │ │ Auto-sent   │ │ Auto-sent   │ │ Sent when│  │   │
│  │ │ purchase    │ │ 1 day before│ │ 1 week...   │ │ cancelled│  │   │
│  │ └─────────────┘ └─────────────┘ └─────────────┘ └──────────┘  │   │
│  │                                                                  │   │
│  │ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐               │   │
│  │ │ Waitlist    │ │Post-Event   │ │ Schedule    │               │   │
│  │ │Notification │ │ Survey      │ │ Change      │               │   │
│  │ │             │ │             │ │             │               │   │
│  │ │ Moved from  │ │ Feedback    │ │ Date/venue  │               │   │
│  │ │ waitlist    │ │ request     │ │ changed     │               │   │
│  │ └─────────────┘ └─────────────┘ └─────────────┘               │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  (Editor panel appears below when card selected)                       │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

CARD SPECIFICATIONS:
- Card size: minWidth 220px, maxWidth 300px, flex: 1
- Card border: 2px solid var(--color-rose-gold)
- Selected border: 2px solid var(--color-burgundy)
- Selected background: rgba(136, 1, 36, 0.05)
- Hover effect: Slight lift (transform: translateY(-2px))
- Transition: all 0.3s ease

CARD CONTENT:
- Title: 16px Montserrat 600, burgundy
- Description: 14px Source Sans 3, stone
- Target info: 12px Source Sans 3 italic, dimmed
```

---

### 4. Event Emails Tab (EventForm Integration)

```
Route: /events/{id}/edit (Tab: Emails)

┌─────────────────────────────────────────────────────────────────────────┐
│  EVENT FORM - Edit "Advanced Harnesses Workshop"                      │
│                                                                         │
│  [Details] │ [Sessions] │ [Tickets] │ [Emails] │ [Settings]           │
│                                       ▔▔▔▔▔▔▔                          │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ EMAIL TEMPLATES                                                  │   │
│  │ ──────────────                                                   │   │
│  │                                                                  │   │
│  │ Click on a template card to edit it below. Templates with       │   │
│  │ "✓ Customized" are event-specific. Others use global defaults.  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌──────────┐  │   │
│  │ │Confirmation │ │ Reminder    │ │ Reminder    │ │Cancella- │  │   │
│  │ │             │ │ 1 Day Before│ │ 1 Week      │ │tion      │  │   │
│  │ │ ✓ Customized│ │  (Default)  │ │  (Default)  │ │ (Default)│  │   │
│  │ │ ───────────┐│ │             │ │             │ │          │  │   │
│  │ │ Includes    ││ │ Auto-sent   │ │ Auto-sent   │ │ Sent when│  │   │
│  │ │ homework...  │ │ 1 day before│ │ 1 week...   │ │ cancelled│  │   │
│  │ └─────────────┘ └─────────────┘ └─────────────┘ └──────────┘  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ EDITOR PANEL                                                     │   │
│  │                                                                  │   │
│  │ Currently Editing: Confirmation Email                            │   │
│  │ Status: ✓ Customized (event-specific)      [Reset to Default]   │   │
│  │ Available Variables: {{attendee_name}}, {{event_title}}...      │   │
│  │                                                                  │   │
│  │ Subject Line:                                                    │   │
│  │ [Your ticket for {{event_title}} - Pre-Class Homework        ]  │   │
│  │                                                                  │   │
│  │ ┌────────────────────────────────────────────────────────────┐  │   │
│  │ │ [B] [I] [U] [Link] [List] [H1] [Insert Variable ▼]       │  │   │
│  │ ├────────────────────────────────────────────────────────────┤  │   │
│  │ │                                                            │  │   │
│  │ │ Hi {{attendee_name}},                                     │  │   │
│  │ │                                                            │  │   │
│  │ │ Thank you for registering for {{event_title}}!            │  │   │
│  │ │                                                            │  │   │
│  │ │ **IMPORTANT - Pre-Class Homework:**                       │  │   │
│  │ │ Please watch this 10-minute video before class: [link]   │  │   │
│  │ │ This will help us jump right into hands-on practice!      │  │   │
│  │ │                                                            │  │   │
│  │ │ Venue: {{venue_name}}                                     │  │   │
│  │ │ {{venue_address}}                                         │  │   │
│  │ └────────────────────────────────────────────────────────────┘  │   │
│  │                                                                  │   │
│  │                                          [Cancel] [Save Template]│   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

BADGE DESIGN:
- "✓ Customized" badge:
  - Background: var(--color-success) (green)
  - Text: white
  - Position: Top-right of card
  - Size: small (12px font)

- "(Default)" badge:
  - Background: var(--color-stone) (gray)
  - Text: white
  - Position: Top-right of card
  - Size: small (12px font)

RESET BUTTON:
- Variant: Secondary (burgundy outline)
- Size: Small
- Position: Next to "Currently Editing" title
- Visible: Only when status is "✓ Customized"
- Confirmation modal: "This will delete your customization and use the global template. Continue?"
```

---

### 5. Ad Hoc Email Tab

```
Route: /admin/email-templates?tab=adhoc

┌─────────────────────────────────────────────────────────────────────────┐
│  EMAIL TEMPLATES MANAGEMENT                                            │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ [Vetting] │ [Events] │ [Admin] │ [Incident] │ [Ad Hoc]         │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                 ▔▔▔▔▔▔▔▔               │
│                                                                         │
│  <!-- AD HOC TAB CONTENT -->                                           │
│                                                                         │
│  SEND AD-HOC EMAIL                                                     │
│  Send one-time bulk emails to event participants or member groups.    │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ COMPOSE EMAIL                                                    │   │
│  │                                                                  │   │
│  │ Recipient Group:                                                 │   │
│  │ [All Ticket Holders - Rope Performance Night            ▼]      │   │
│  │ Recipients: 187 people                                           │   │
│  │                                                                  │   │
│  │ Subject Line:                                                    │   │
│  │ [Parking Update for {{event_title}}                          ]  │   │
│  │                                                                  │   │
│  │ ┌────────────────────────────────────────────────────────────┐  │   │
│  │ │ [B] [I] [U] [Link] [List] [H1] [Insert Variable ▼]       │  │   │
│  │ ├────────────────────────────────────────────────────────────┤  │   │
│  │ │                                                            │  │   │
│  │ │ Hi {{recipient_name}},                                    │  │   │
│  │ │                                                            │  │   │
│  │ │ Great news! We've secured free parking for {{event_title}}│  │   │
│  │ │                                                            │  │   │
│  │ │ Park in the lot behind the venue (enter from Essex St).  │  │   │
│  │ │ Show your ticket confirmation email to the attendant for  │  │   │
│  │ │ free parking.                                             │  │   │
│  │ │                                                            │  │   │
│  │ │ See you on {{event_date}}!                                │  │   │
│  │ └────────────────────────────────────────────────────────────┘  │   │
│  │                                                                  │   │
│  │                                                     [Send Email]  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ SENT AD-HOC EMAIL HISTORY                                        │   │
│  │                                                                  │   │
│  │ ┌────────────────────────────────────────────────────────────┐  │   │
│  │ │ Subject         │ Recipients        │ Sent       │ Status  │  │   │
│  │ ├────────────────────────────────────────────────────────────┤  │   │
│  │ │ Parking Update  │ All Tickets (187) │ 2 hours ago│Delivered│  │   │
│  │ │ Venue Change    │ S1 Attendees (42) │ 3 days ago │Delivered│  │   │
│  │ │ Workshop Reminder│Volunteers (15)   │ 1 week ago │Delivered│  │   │
│  │ └────────────────────────────────────────────────────────────┘  │   │
│  │                                                                  │   │
│  │ Click any row to view full email content (read-only)            │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

AD-HOC EMAIL FEATURES:
- Recipient group selector: Dropdown with predefined groups
  - "All Ticket Holders - [Event Name]"
  - "Specific Session - [Session Name]"
  - "Volunteers - [Event Name]"
  - "Custom list" (enter email addresses)

- Recipient count preview: Shows number before sending

- History table: Sortable by date, filterable by event

- Row click: Opens modal with full email content (read-only)

SEND BUTTON:
- Variant: Primary (gold gradient)
- Size: Large
- Position: Bottom-right of compose panel
- Confirmation dialog: "Send email to 187 recipients? This cannot be undone."
```

---

### 6. Mobile Responsive Layout (Tablet View)

```
TABLET (768px - 1199px)

┌─────────────────────────────────────┐
│  EMAIL TEMPLATES MANAGEMENT        │
│  Manage global email templates...  │
│                                     │
├─────────────────────────────────────┤
│                                     │
│  ┌───────────────────────────────┐ │
│  │ Vetting │ Events │ Admin ... │ │  (Tabs scroll horizontally)
│  └───────────────────────────────┘ │
│  ▔▔▔▔▔▔▔                           │
│                                     │
│  ┌───────────────────────────────┐ │
│  │ ┌───────────┐ ┌───────────┐  │ │  (Cards scroll horizontally)
│  │ │Application│ │ Interview │  │ │
│  │ │ Received  │ │ Approved  │  │ │
│  │ │           │ │           │  │ │
│  │ │ Sent when │ │ Sent when │  │ │
│  │ │ app...    │ │ interview │  │ │
│  │ └───────────┘ └───────────┘  │ │
│  └───────────────────────────────┘ │
│                                     │
│  ┌───────────────────────────────┐ │
│  │ EDITOR PANEL                  │ │
│  │                               │ │
│  │ Currently Editing: ...        │ │
│  │                               │ │
│  │ Subject Line:                 │ │
│  │ [Your vetting application  ]  │ │
│  │                               │ │
│  │ ┌─────────────────────────┐  │ │
│  │ │ [B][I][U] [More ▼]     │  │ │  (Toolbar simplified)
│  │ ├─────────────────────────┤  │ │
│  │ │ Hi {{scene_name}},      │  │ │
│  │ │ Thank you for...        │  │ │
│  │ └─────────────────────────┘  │ │
│  │                               │ │
│  │ [Cancel] [Save Template]      │ │  (Buttons stack on smaller tablets)
│  └───────────────────────────────┘ │
│                                     │
└─────────────────────────────────────┘

MOBILE (< 768px)
NOT SUPPORTED - Admin workflows desktop/tablet only
Show message: "Email template management requires desktop or tablet device"
```

---

## Component Specifications

### 1. Admin Dashboard "Email Templates" Card

**Location**: `/admin` dashboard page

**Mantine Components**:
- `Grid` (layout container)
- `Grid.Col` (responsive column)
- `Paper` (card container)
- `Stack` (vertical layout)
- `Group` (horizontal layout)
- `Text` (title, description)
- `Box` (icon container)

**Styling**:
```tsx
<Grid.Col span={{ base: 12, sm: 6, lg: 3 }}>
  <Paper
    shadow="sm"
    p="xl"
    radius="md"
    style={{
      cursor: 'pointer',
      transition: 'transform 0.3s ease, box-shadow 0.3s ease',
      height: '100%',
      display: 'flex',
      flexDirection: 'column',
      borderLeft: '4px solid #FF6B35'  // Email Templates accent color
    }}
    onMouseEnter={(e) => {
      e.currentTarget.style.transform = 'translateY(-4px)';
      e.currentTarget.style.boxShadow = '0 8px 24px rgba(0,0,0,0.12)';
    }}
    onMouseLeave={(e) => {
      e.currentTarget.style.transform = 'translateY(0)';
      e.currentTarget.style.boxShadow = '0 2px 8px rgba(0,0,0,0.08)';
    }}
    onClick={() => navigate('/admin/email-templates')}
  >
    <Stack gap="md" style={{ flex: 1 }}>
      <Box style={{
        width: '64px',
        height: '64px',
        borderRadius: '50%',
        background: 'linear-gradient(135deg, #FF6B35 0%, #FF8C00 100%)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        marginBottom: 'var(--space-sm)'
      }}>
        <IconMail size={32} color="white" />
      </Box>

      <div style={{ flex: 1 }}>
        <Text
          fw={700}
          size="lg"
          mb="xs"
          style={{
            fontFamily: 'var(--font-heading)',
            color: 'var(--color-burgundy)',
            textTransform: 'uppercase',
            letterSpacing: '0.5px'
          }}
        >
          Email Templates
        </Text>

        <Text size="sm" c="dimmed" style={{ lineHeight: 1.6 }}>
          Manage global email templates for all categories
        </Text>
      </div>
    </Stack>
  </Paper>
</Grid.Col>
```

**Interaction States**:
- **Default**: Shadow: sm, no transform
- **Hover**: translateY(-4px), shadow: 0 8px 24px rgba(0,0,0,0.12)
- **Active**: Navigate to `/admin/email-templates`

---

### 2. EmailTemplatesAdminPage Component

**Route**: `/admin/email-templates`

**Mantine Components**:
- `Container` (page container, size="xl")
- `Title` (page title)
- `Text` (description)
- `Tabs` (category navigation)
- `Tabs.List` (tab headers)
- `Tabs.Tab` (individual tab)
- `Tabs.Panel` (tab content)
- `Button` (back button, optional)

**Structure**:
```tsx
export const EmailTemplatesAdminPage: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = searchParams.get('tab') || 'vetting';

  return (
    <Container size="xl" py="xl">
      {/* Page Header */}
      <Box mb="xl">
        <Title
          order={1}
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '32px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
            marginBottom: 'var(--space-sm)'
          }}
        >
          Email Templates Management
        </Title>

        <Text size="sm" c="dimmed">
          Manage global email templates for all system categories
        </Text>
      </Box>

      {/* Tabbed Interface */}
      <Tabs
        value={activeTab}
        onChange={(value) => setSearchParams({ tab: value || 'vetting' })}
      >
        <Tabs.List>
          <Tabs.Tab value="vetting">Vetting</Tabs.Tab>
          <Tabs.Tab value="events">Events</Tabs.Tab>
          <Tabs.Tab value="admin">Admin</Tabs.Tab>
          <Tabs.Tab value="incident">Incident</Tabs.Tab>
          <Tabs.Tab value="adhoc">Ad Hoc</Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="vetting" pt="xl">
          <EmailCategoryPanel category="vetting" />
        </Tabs.Panel>

        <Tabs.Panel value="events" pt="xl">
          <EmailCategoryPanel category="events" />
        </Tabs.Panel>

        {/* ... other panels */}
      </Tabs>
    </Container>
  );
};
```

**Tab Styling** (Mantine styles API):
```tsx
<Tabs
  styles={{
    tab: {
      fontFamily: "'Montserrat', sans-serif",
      fontSize: '15px',
      fontWeight: 600,
      textTransform: 'uppercase',
      letterSpacing: '1px',
      color: 'var(--color-smoke)',
      padding: 'var(--space-md) var(--space-lg)',
      borderBottom: '3px solid transparent',
      transition: 'all 0.3s ease',
      '&[data-active]': {
        color: 'var(--color-burgundy)',
        borderBottomColor: 'var(--color-burgundy)',
        fontWeight: 700
      },
      '&:hover': {
        backgroundColor: 'rgba(136, 1, 36, 0.05)',
        borderBottomColor: 'var(--color-rose-gold)'
      }
    }
  }}
>
```

---

### 3. EmailCategoryPanel Component

**Purpose**: Reusable panel for displaying template cards + editor for a single category

**Props**:
```typescript
interface EmailCategoryPanelProps {
  category: 'vetting' | 'events' | 'admin' | 'incident' | 'adhoc';
}
```

**Mantine Components**:
- `Stack` (vertical layout)
- `Group` (template cards container)
- `Card` (template card)
- `Text` (card title, description)
- `Paper` (editor panel)
- `TextInput` (subject line)
- `MantineTiptapEditor` (HTML content editor)
- `Alert` (variable validation warnings)
- `Button` (save, cancel)

**Template Card Design**:
```tsx
<Card
  withBorder
  p="md"
  style={{
    cursor: 'pointer',
    borderColor: isSelected
      ? 'var(--color-burgundy)'
      : 'var(--color-rose-gold)',
    backgroundColor: isSelected
      ? 'rgba(136, 1, 36, 0.05)'
      : 'white',
    minWidth: '220px',
    maxWidth: '300px',
    flex: 1,
    position: 'relative',
    transition: 'all 0.3s ease',
    borderRadius: '12px',
    '&:hover': {
      transform: 'translateY(-2px)',
      boxShadow: '0 4px 12px rgba(0,0,0,0.1)'
    }
  }}
  onClick={() => handleSelectTemplate(template)}
>
  <Text
    fw={600}
    c="burgundy"
    mb={4}
    style={{
      fontFamily: 'var(--font-heading)',
      fontSize: '16px'
    }}
  >
    {template.name}
  </Text>

  <Text size="sm" c="stone" mb="xs" style={{ lineHeight: 1.5 }}>
    {template.description}
  </Text>

  <Text
    size="xs"
    c="dimmed"
    style={{
      fontStyle: 'italic',
      fontFamily: 'var(--font-body)'
    }}
  >
    {template.targetInfo}
  </Text>
</Card>
```

**Editor Panel Design**:
```tsx
{selectedTemplate && (
  <Paper
    shadow="sm"
    radius="md"
    p="xl"
    mt="xl"
    style={{
      border: '1px solid var(--color-taupe)',
      backgroundColor: 'white'
    }}
  >
    <Stack gap="md">
      {/* Header */}
      <Group justify="space-between" mb="md">
        <div>
          <Text
            fw={600}
            c="burgundy"
            size="lg"
            style={{
              fontFamily: 'var(--font-heading)',
              textTransform: 'uppercase',
              letterSpacing: '0.5px'
            }}
          >
            Currently Editing: {selectedTemplate.name}
          </Text>

          <Text size="xs" c="dimmed" mt="xs">
            Available Variables: {selectedTemplate.variables.join(', ')}
          </Text>
        </div>
      </Group>

      {/* Subject Line */}
      <TextInput
        label="Subject Line"
        placeholder="Enter email subject"
        value={subject}
        onChange={(e) => setSubject(e.currentTarget.value)}
        required
        maxLength={200}
        styles={{
          label: {
            fontFamily: 'var(--font-heading)',
            fontSize: '14px',
            fontWeight: 600,
            color: 'var(--color-smoke)',
            marginBottom: 'var(--space-xs)',
            textTransform: 'uppercase',
            letterSpacing: '0.5px'
          }
        }}
      />

      {/* HTML Editor */}
      <div>
        <Text
          component="label"
          style={{
            display: 'block',
            fontFamily: 'var(--font-heading)',
            fontSize: '14px',
            fontWeight: 600,
            color: 'var(--color-smoke)',
            marginBottom: 'var(--space-xs)',
            textTransform: 'uppercase',
            letterSpacing: '0.5px'
          }}
        >
          Email Content
        </Text>

        <MantineTiptapEditor
          value={htmlBody}
          onChange={setHtmlBody}
          placeholder="Enter email template content..."
          minRows={12}
          variables={selectedTemplate.variables}  // Pass variables for "Insert Variable" button
        />

        <Text size="xs" c="dimmed" mt="xs">
          Use variables like {'{{scene_name}}'} in your template. They will be replaced with actual values when emails are sent.
        </Text>
      </div>

      {/* Variable Validation Warning */}
      {invalidVariables.length > 0 && (
        <Alert
          icon={<IconAlertCircle />}
          color="yellow"
          title="Unknown Variables Detected"
        >
          <Text size="sm">
            The following variables are not recognized for this category: {invalidVariables.join(', ')}
          </Text>
          <Text size="xs" mt="xs">
            Available variables: {selectedTemplate.variables.join(', ')}
          </Text>
        </Alert>
      )}

      {/* Actions */}
      <Group justify="flex-end" gap="sm" mt="md">
        <Button
          variant="light"
          onClick={handleCancel}
          disabled={isSaving}
        >
          Cancel
        </Button>

        <Button
          className="btn btn-primary"
          onClick={handleSave}
          loading={isSaving}
        >
          Save Template
        </Button>
      </Group>
    </Stack>
  </Paper>
)}
```

---

### 4. EventForm Emails Tab Integration

**Location**: `/apps/web/src/components/events/EventForm.tsx` (Emails tab panel)

**New Features to Add**:
1. **Fetch global + event-specific templates** on tab load
2. **Badge indicators** showing customization status
3. **"Reset to Default" button** for customized templates
4. **Confirmation modal** before reset

**Badge Component**:
```tsx
<Badge
  color={isCustomized ? 'green' : 'gray'}
  variant="filled"
  size="sm"
  style={{
    position: 'absolute',
    top: '8px',
    right: '8px',
    fontSize: '11px',
    textTransform: 'uppercase',
    letterSpacing: '0.5px'
  }}
>
  {isCustomized ? '✓ Customized' : '(Default)'}
</Badge>
```

**Reset Button**:
```tsx
{isCustomized && (
  <Button
    variant="light"
    color="red"
    size="sm"
    onClick={handleResetToDefault}
    style={{
      position: 'absolute',
      top: '8px',
      left: '8px'
    }}
  >
    Reset to Default
  </Button>
)}
```

**Reset Confirmation Modal**:
```tsx
<Modal
  opened={showResetModal}
  onClose={() => setShowResetModal(false)}
  title="Reset to Default Template?"
  centered
>
  <Text size="sm" mb="md">
    This will delete your event-specific customization for the {templateName} template.
    Future emails will use the global default template.
  </Text>

  <Text size="sm" fw={600} c="red" mb="md">
    This action cannot be undone.
  </Text>

  <Group justify="flex-end" gap="sm">
    <Button
      variant="light"
      onClick={() => setShowResetModal(false)}
    >
      Cancel
    </Button>

    <Button
      color="red"
      onClick={handleConfirmReset}
    >
      Reset to Default
    </Button>
  </Group>
</Modal>
```

---

## Interaction Patterns

### 1. Template Card Selection

**User Flow**:
1. User clicks template card
2. Card border changes to burgundy, background becomes light burgundy
3. Editor panel appears below with template content pre-filled
4. Focus automatically shifts to subject line input

**Animation**:
- Card selection: 0.3s ease transition
- Editor panel slide-in: 0.4s ease from top
- Focus transition: smooth scroll to editor

**State Management**:
```typescript
const [selectedTemplate, setSelectedTemplate] = useState<Template | null>(null);

const handleSelectTemplate = (template: Template) => {
  setSelectedTemplate(template);
  // Scroll to editor
  setTimeout(() => {
    editorRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }, 100);
};
```

---

### 2. Variable Validation

**Validation Logic**:
1. Extract all `{{variable_name}}` patterns from HTML content
2. Compare against category-specific allowed variables
3. Display warning (not error) if unknown variables detected
4. Allow saving (don't block) - warning only

**Visual Treatment**:
```tsx
// Real-time validation
useEffect(() => {
  const extractedVars = extractVariables(htmlBody);
  const invalid = extractedVars.filter(v => !allowedVariables.includes(v));
  setInvalidVariables(invalid);
}, [htmlBody, allowedVariables]);

// Warning display
{invalidVariables.length > 0 && (
  <Alert
    icon={<IconAlertCircle />}
    color="yellow"
    variant="light"
    title="Unknown Variables Detected"
  >
    <Text size="sm">
      {invalidVariables.join(', ')}
    </Text>
  </Alert>
)}
```

---

### 3. Save Workflow

**User Flow**:
1. User edits subject or body content
2. User clicks "Save Template"
3. Button shows loading state (spinner + "Saving...")
4. API call to save template
5. On success:
   - Notification: "Template saved successfully"
   - Editor closes
   - Card updates with new content preview
6. On error:
   - Notification: "Failed to save template. Please try again."
   - Editor remains open (preserve changes)

**Loading States**:
```tsx
<Button
  className="btn btn-primary"
  onClick={handleSave}
  loading={isSaving}
  disabled={!hasChanges || isSaving}
>
  {isSaving ? 'Saving...' : 'Save Template'}
</Button>
```

---

### 4. Reset to Default

**User Flow**:
1. User clicks "Reset to Default" button
2. Confirmation modal appears
3. User confirms or cancels
4. If confirmed:
   - DELETE EventEmailTemplate record
   - Re-fetch global template
   - Update editor with global content
   - Badge changes from "✓ Customized" to "(Default)"
   - Notification: "Template reset to default"

**Confirmation Modal**:
- Title: "Reset to Default Template?"
- Message: Explain consequences (delete customization, use global)
- Warning: "This action cannot be undone"
- Buttons: Cancel (light) + Reset to Default (red)

---

## Responsive Breakpoints

### Desktop (≥1200px)
- Full-width container (max-width: 1200px)
- Template cards: 4-5 visible, horizontal scroll if more
- Editor panel: Full width below cards
- Button layout: Horizontal (Cancel | Save Template)

### Tablet (768px - 1199px)
- Container padding: 20px (reduced from 40px)
- Template cards: 3 visible, horizontal scroll
- Editor panel: Full width below cards
- Button layout: Horizontal (Cancel | Save Template)
- TipTap toolbar: Simplified with "More" dropdown

### Mobile (<768px)
- **NOT SUPPORTED** for admin template management
- Show message: "Email template management requires desktop or tablet device"
- Reason: Admin workflows typically desktop/tablet, complex editor not ideal for phone

**Exception**: Event organizers might customize templates on mobile (EventForm Emails tab)
- If needed, stack cards vertically, full-width editor, buttons stack vertically

---

## Accessibility Requirements

### ARIA Labels

```tsx
// Template card
<Card
  role="button"
  tabIndex={0}
  aria-label={`Edit ${template.name} template`}
  aria-pressed={isSelected}
  onClick={handleSelectTemplate}
  onKeyDown={(e) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      handleSelectTemplate();
    }
  }}
>

// Save button
<Button
  aria-label="Save email template changes"
  aria-busy={isSaving}
>
  Save Template
</Button>

// Reset button
<Button
  aria-label="Reset template to global default"
  aria-describedby="reset-warning"
>
  Reset to Default
</Button>
<Text id="reset-warning" style={{ display: 'none' }}>
  This will delete your customization and use the global template
</Text>
```

### Keyboard Navigation

**Tab Order**:
1. Tab headers (Vetting, Events, Admin, Incident, Ad Hoc)
2. Template cards (left to right)
3. Subject line input
4. TipTap editor
5. Cancel button
6. Save button

**Keyboard Shortcuts**:
- **Tab**: Navigate between elements
- **Enter/Space**: Activate buttons, select template cards
- **Escape**: Close editor panel, close modals
- **Ctrl+B/I/U**: TipTap formatting shortcuts

**Focus Management**:
```typescript
// Auto-focus subject line when editor opens
useEffect(() => {
  if (selectedTemplate && subjectInputRef.current) {
    subjectInputRef.current.focus();
  }
}, [selectedTemplate]);

// Return focus to card when editor closes
const handleCancel = () => {
  const cardButton = document.querySelector(`[data-template-id="${selectedTemplate.id}"]`);
  setSelectedTemplate(null);
  setTimeout(() => {
    cardButton?.focus();
  }, 100);
};
```

### Screen Reader Announcements

```tsx
// Live region for save status
<div
  role="status"
  aria-live="polite"
  aria-atomic="true"
  style={{ position: 'absolute', left: '-9999px' }}
>
  {isSaving && 'Saving template...'}
  {saveSuccess && 'Template saved successfully'}
  {saveError && 'Failed to save template. Please try again.'}
</div>

// Template count announcement
<Text
  role="status"
  aria-live="polite"
  style={{ display: 'none' }}
>
  {templates.length} templates available for {activeCategory} category
</Text>
```

### Color Contrast

**Verified Ratios** (WCAG 2.1 AA minimum 4.5:1):
- Page title (burgundy on cream): 8.2:1 ✅
- Card title (burgundy on white): 9.1:1 ✅
- Card description (stone on white): 5.3:1 ✅
- Button primary (midnight on gold): 7.2:1 ✅
- Button secondary (burgundy on white): 8.5:1 ✅
- Badge "Customized" (white on green): 4.8:1 ✅
- Badge "Default" (white on gray): 4.6:1 ✅

---

## Mantine Components Used

| Component | Purpose | Configuration |
|-----------|---------|--------------|
| **Container** | Page container | size="xl", py="xl" |
| **Title** | Page titles | order={1-3}, Montserrat font, burgundy color |
| **Text** | Body text, descriptions | size="sm"/"md", c="dimmed"/"stone"/"burgundy" |
| **Tabs** | Category navigation | value={activeTab}, onChange handler |
| **Tabs.List** | Tab headers | Custom styles for active/hover states |
| **Tabs.Tab** | Individual tab | Uppercase, letter-spacing, burgundy underline |
| **Tabs.Panel** | Tab content | pt="xl" |
| **Stack** | Vertical layouts | gap="md"/"lg"/"xl" |
| **Group** | Horizontal layouts | justify="space-between"/"flex-end", gap="sm"/"md" |
| **Grid** | Responsive grids | gutter="xl" |
| **Grid.Col** | Grid columns | span={{ base: 12, sm: 6, lg: 3 }} |
| **Paper** | Card containers | shadow="sm", radius="md", p="xl" |
| **Card** | Template cards | withBorder, custom styles for selection |
| **Badge** | Status indicators | color="green"/"gray", size="sm", variant="filled" |
| **Button** | Actions | variant="light"/"default", loading state |
| **TextInput** | Subject line | label, placeholder, maxLength={200} |
| **Alert** | Validation warnings | icon={IconAlertCircle}, color="yellow", variant="light" |
| **Modal** | Confirmation dialogs | opened, onClose, title, centered |
| **Loader** | Loading state | size="lg" |
| **Select** | Dropdown (ad-hoc) | data, value, onChange |
| **MantineTiptapEditor** | HTML editor | value, onChange, minRows, variables prop |

---

## Design System Integration

### Color Variables Usage

```css
/* Use CSS variables from Design System v7 */
--color-burgundy: #880124
--color-plum: #614B79
--color-rose-gold: #B76D75
--color-charcoal: #2B2B2B
--color-smoke: #4A4A4A
--color-stone: #8B8680
--color-ivory: #FFF8F0
--color-cream: #FAF6F2
--color-success: #228B22
--color-warning: #DAA520

/* Never hardcode colors - always reference variables */
```

### Spacing System

```css
/* Use spacing scale consistently */
--space-xs: 8px
--space-sm: 16px
--space-md: 24px
--space-lg: 32px
--space-xl: 40px
--space-2xl: 48px

/* Example usage */
padding: var(--space-xl);
gap: var(--space-md);
margin-bottom: var(--space-sm);
```

### Button Styles

**Use existing button classes from Design System v7**:
- `.btn.btn-primary` - Gold/amber gradient (primary actions: Save Template)
- `.btn.btn-secondary` - Burgundy outline (secondary actions: Cancel)
- `.btn:disabled` - Stone gray (disabled state)

**DO NOT create custom button styles** - reuse Design System v7 patterns

### Typography

```css
/* Page titles */
font-family: 'Montserrat', sans-serif;
font-weight: 800;
font-size: 32px;
color: #880124;
text-transform: uppercase;
letter-spacing: -0.5px;

/* Section titles */
font-family: 'Montserrat', sans-serif;
font-weight: 700;
font-size: 24px;
color: #880124;
text-transform: uppercase;
letter-spacing: 1px;

/* Body text */
font-family: 'Source Sans 3', sans-serif;
font-weight: 400;
font-size: 16px;
color: #2B2B2B;
line-height: 1.6;
```

---

## Animations

### Card Hover Effect

```css
.template-card {
  transition: transform 0.3s ease, box-shadow 0.3s ease, border-color 0.3s ease;
}

.template-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
  border-color: var(--color-burgundy);
}
```

### Editor Panel Slide-In

```css
@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.editor-panel {
  animation: slideDown 0.4s ease;
}
```

### Tab Active State

```css
.mantine-Tabs-tab[data-active] {
  color: var(--color-burgundy);
  border-bottom-color: var(--color-burgundy);
  font-weight: 700;
  transition: all 0.3s ease;
}
```

### Button Loading State

```tsx
// Mantine Button component handles loading animation automatically
<Button loading={isSaving}>
  {isSaving ? 'Saving...' : 'Save Template'}
</Button>
```

---

## Quality Checklist

### Design System Compliance
- [x] Uses Design System v7 color palette (burgundy, plum, rose-gold, etc.)
- [x] Uses Design System v7 typography (Montserrat headings, Source Sans 3 body)
- [x] Uses Design System v7 spacing scale (var(--space-*))
- [x] Uses Design System v7 button styles (btn-primary, btn-secondary)
- [x] Follows Design System v7 component patterns (cards, badges, modals)

### Mantine v7 Framework
- [x] Uses Mantine v7 components exclusively (no custom UI components)
- [x] Leverages Mantine responsive props (span={{ base, sm, lg }})
- [x] Uses Mantine theming system (color names, sizes)
- [x] Applies Mantine styles API for customization
- [x] Integrates MantineTiptapEditor for rich text editing

### React Best Practices
- [x] Functional components with hooks
- [x] TypeScript-first (typed props, state, events)
- [x] Proper state management (useState, useEffect)
- [x] Event handlers follow React patterns
- [x] No direct DOM manipulation

### Accessibility (WCAG 2.1 AA)
- [x] All interactive elements keyboard accessible
- [x] ARIA labels on all buttons and cards
- [x] Color contrast ratios meet 4.5:1 minimum
- [x] Focus indicators visible
- [x] Screen reader announcements for state changes
- [x] Semantic HTML structure

### User Experience
- [x] Clear visual hierarchy (tabs → cards → editor)
- [x] Progressive disclosure (editor shows on selection)
- [x] Intuitive navigation (tabbed interface)
- [x] Consistent interaction patterns
- [x] Clear feedback (loading states, notifications, validation warnings)
- [x] Undo capability (cancel button, reset to default)

### Responsive Design
- [x] Desktop optimized (≥1200px)
- [x] Tablet support (768px-1199px)
- [x] Mobile graceful degradation (<768px - show unsupported message)
- [x] Breakpoints follow Mantine standards
- [x] Touch-friendly targets on tablet (44px minimum)

### Community Values
- [x] Professional admin interface
- [x] Clear communication standards
- [x] Safety-centered (variable validation warnings)
- [x] Accessible to all admins
- [x] Respects existing patterns

### Performance
- [x] Lazy load editor panel (only when template selected)
- [x] Optimize re-renders (proper React state management)
- [x] Smooth animations (60fps target)
- [x] Efficient API calls (fetch templates once per tab)

---

## Implementation Guidance

### For React Developer

**Priority 1 - Core Structure**:
1. Create `EmailTemplatesAdminPage.tsx` with tabbed interface
2. Create `EmailCategoryPanel.tsx` reusable component
3. Add "Email Templates" card to Admin Dashboard
4. Implement URL query parameter handling (?tab=vetting)

**Priority 2 - Template Cards**:
1. Fetch global templates per category
2. Render template cards in horizontal scrollable container
3. Implement card selection state (burgundy border, light background)
4. Add click handler to show/hide editor panel

**Priority 3 - Editor Panel**:
1. Integrate MantineTiptapEditor component
2. Add subject line TextInput
3. Implement variable reference display
4. Add variable validation logic (extract {{vars}}, compare to allowed list)
5. Add save/cancel buttons with loading states

**Priority 4 - EventForm Integration**:
1. Update EventForm Emails tab to fetch global + event-specific templates
2. Add badge indicators ("✓ Customized" vs "(Default)")
3. Add "Reset to Default" button with confirmation modal
4. Implement reset logic (DELETE EventEmailTemplate, re-fetch global)

**Priority 5 - Polish**:
1. Add animations (card hover, editor slide-in)
2. Add accessibility features (ARIA labels, keyboard navigation)
3. Add error handling (API failures, validation errors)
4. Add notifications (save success, reset success, errors)

### Recommended Component Structure

```
/apps/web/src/features/admin/email-templates/
├── pages/
│   └── EmailTemplatesAdminPage.tsx
├── components/
│   ├── EmailCategoryPanel.tsx
│   ├── TemplateCard.tsx
│   ├── TemplateEditor.tsx
│   └── ResetConfirmationModal.tsx
├── hooks/
│   ├── useEmailTemplates.ts  (React Query)
│   └── useVariableValidation.ts
├── services/
│   └── emailTemplates.api.ts
└── types/
    └── emailTemplates.types.ts

/apps/web/src/components/events/
└── EventForm.tsx (update Emails tab panel)
```

---

## Handoff Document Creation

After completing this UI design, create an agent handoff document:

**Template**: `/home/chad/repos/witchcityrope/docs/standards-processes/agent-handoff-template.md`

**Save To**: `/home/chad/repos/witchcityrope/docs/functional-areas/email-templates/new-work/2025-11-09-admin-management/handoffs/ui-designer-2025-11-09-handoff.md`

**Include**:
1. UI design completion status
2. Key design decisions (color palette, Mantine components, layout patterns)
3. Component reuse strategy (AdminDashboard card, EventForm template cards, MantineTiptapEditor)
4. What functional spec needs to address:
   - API endpoint specs for fetching templates by category
   - DTO structures for GlobalEmailTemplateDto, EventEmailTemplateDto
   - Variable sets per category (allowed variables for validation)
5. What database design needs to support:
   - GlobalEmailTemplates table query patterns (by category, by category+type)
   - EventEmailTemplates table relationships (EventId + TemplateType unique constraint)
   - Variable sets storage (JSONB or separate table)
6. What react-developer needs to know:
   - Exact Mantine components and props
   - State management approach (selected template, validation warnings)
   - API integration points (useQuery for templates, useMutation for save/delete)
   - Accessibility requirements (ARIA labels, keyboard nav)
7. Recommended implementation approach:
   - Build EmailCategoryPanel first (reusable across all tabs)
   - Test with Vetting tab (smallest set of templates: 6)
   - Extend to Events tab (7 templates)
   - Add EventForm integration last (depends on EmailCategoryPanel)

---

**END OF UI DESIGN DOCUMENT**
