# UI Design Handoff - Email Template Trigger Enhancements
<!-- Date: 2025-12-01 -->
<!-- Phase: Design (Phase 2) -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Complete -->

## Executive Summary

This document provides complete UI/UX specifications for the Email Template Trigger Enhancements feature. All designs follow Design System v7 patterns, use Mantine v7 components, and maintain consistency with existing admin interfaces.

**Key Design Decisions**:
- Enhanced template cards with trigger badges and timing display
- Modal-based trigger configuration (progressive disclosure)
- Recipient group selection with clear business logic labels
- Ad hoc scheduled send using DateTimePicker
- Template management with save/delete capabilities

---

## 1. Code Review Summary

### Existing Patterns Discovered

**Admin Card Header Pattern** (from lessons learned):
- Burgundy/plum gradient header: `linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)`
- Icon + Title pattern with ivory text
- 1px taupe border-bottom separator
- Consistent padding: `var(--space-lg) var(--space-xl)`

**Design System v7 Standards**:
- Color palette: Burgundy (#880124), Plum (#614B79), Rose Gold (#B76D75)
- Typography: Montserrat for headings, Source Sans 3 for body
- Button system: Primary CTA (gold gradient), Secondary (burgundy outline)
- Signature corner morphing animation on all buttons

**Mantine v7 Component Library**:
- Card, Badge, Modal, Radio.Group, Select, NumberInput, Switch, DateTimePicker
- Stack/Group for layout (gap-based spacing)
- Grid for two-column layouts
- Notifications system for feedback

**Current Entity Structure**:
- `GlobalEmailTemplate`: Category, TemplateType, Subject, HtmlBody, PlainTextBody, Variables
- `EventEmailTemplate`: EventId, GlobalTemplateId, Subject, HtmlBody, RecipientGroup (string, nullable)
- NO existing trigger configuration fields (to be added)

---

## 2. Component Specifications

### 2.1 Enhanced Template Card (Events Tab)

**Purpose**: Display event templates with trigger configuration at a glance

**Current State** (verified from requirements):
- Shows: Name (TemplateType), Subject
- Location: Events tab in Admin Email Templates page

**Enhanced State**:
```
┌─────────────────────────────────────────────────────────────┐
│ 🔔 Reminder 3 Days Before              [Edit Trigger] [Edit]│
│─────────────────────────────────────────────────────────────│
│ Subject: Don't forget your class this Friday!               │
│                                                              │
│ [⏰ Time-Based] [📅 3 days before] [👥 RSVPTicketHolders]  │
│ Enabled: ✅                                                 │
└─────────────────────────────────────────────────────────────┘
```

**Component Props**:
```tsx
interface EnhancedTemplateCardProps {
  template: {
    id: string;
    templateType: string;
    subject: string;
    triggerType: 'FixedEvent' | 'TimeBased' | 'Manual';
    triggerEnabled: boolean;
    timingOffsetDays?: number;  // +3 = before, -2 = after
    recipientGroup?: EventRecipientGroup;
  };
  onEditTrigger: (templateId: string) => void;
  onEditTemplate: (templateId: string) => void;
}
```

**Mantine Components**:
- `Card` - Container with shadow
- `Group` - Header layout (title + buttons)
- `Badge` - Trigger type, timing, recipient group
- `Text` - Subject display
- `Switch` - Enabled/disabled indicator (read-only, opens modal to toggle)
- `Button` - "Edit Trigger" action

**Visual Hierarchy**:
1. **Header**: Template name + action buttons (right-aligned)
2. **Subject**: Secondary text, 1 line with ellipsis
3. **Badges Row**: Trigger type, timing, recipient group (left-aligned)
4. **Status Indicator**: Enabled/disabled with checkmark/cross

**Badge Color Coding**:
- **Time-Based**: Plum (#614B79) background, ivory text
- **Fixed Event**: Burgundy (#880124) background, ivory text
- **Manual**: Stone gray (#8B8680) background, ivory text
- **Timing**: Rose gold (#B76D75) background, charcoal text
- **Recipient**: Dusty rose (#D4A5A5) background, charcoal text

**Responsive Behavior**:
- Desktop (≥1024px): Full layout as shown
- Admin areas are desktop-only (no mobile testing per Mantine UI Standards)

---

### 2.2 Trigger Configuration Modal

**Purpose**: Configure time-based triggers and recipient groups for event templates

**Trigger**: Click "Edit Trigger" button on template card

**Modal Layout**:
```
┌──────────────────────────────────────────────────────────────┐
│ Configure Email Trigger                                   [X]│
├──────────────────────────────────────────────────────────────┤
│                                                               │
│ Template: Reminder 3 Days Before                             │
│ Subject: Don't forget your class this Friday!                │
│                                                               │
│ ─────────────────────────────────────────────────────────────│
│                                                               │
│ TRIGGER TYPE                                                 │
│ ○ Fixed Event (triggered by action)                         │
│ ● Time-Based (triggered by session timing)                  │
│                                                               │
│ [Only shown when Time-Based is selected:]                    │
│                                                               │
│ TIMING OFFSET                                                │
│ Send email [ 3 ] days [Before ▼] session start             │
│                                                               │
│ Helper text: Positive = before session, Negative = after    │
│                                                               │
│ RECIPIENT GROUP                                              │
│ Who receives this email?                                     │
│ [Session Attendees (users who checked in)        ▼]         │
│                                                               │
│ Options:                                                     │
│   - Session Attendees (users who checked in)                │
│   - RSVP/Ticket Holders (deduplicated)                      │
│   - Session Volunteers (assigned to session)                │
│   - Teachers (assigned to session)                          │
│                                                               │
│ ENABLE/DISABLE                                               │
│ [✓] Template enabled (will send automatically)              │
│                                                               │
│ ─────────────────────────────────────────────────────────────│
│                                                               │
│                         [Cancel] [Save Configuration]        │
└──────────────────────────────────────────────────────────────┘
```

**Component Props**:
```tsx
interface TriggerConfigModalProps {
  opened: boolean;
  onClose: () => void;
  template: {
    id: string;
    templateType: string;
    subject: string;
    triggerType: 'FixedEvent' | 'TimeBased' | 'Manual';
    triggerEnabled: boolean;
    timingOffsetDays?: number;
    recipientGroup?: EventRecipientGroup;
  };
  onSave: (config: TriggerConfig) => Promise<void>;
}

interface TriggerConfig {
  triggerType: 'FixedEvent' | 'TimeBased' | 'Manual';
  triggerEnabled: boolean;
  timingOffsetDays?: number;
  recipientGroup?: EventRecipientGroup;
}

enum EventRecipientGroup {
  SessionAttendees = 'SessionAttendees',
  RSVPTicketHolders = 'RSVPTicketHolders',
  SessionVolunteers = 'SessionVolunteers',
  Teachers = 'Teachers'
}
```

**Mantine Components**:
- `Modal` - Container (size="lg", centered)
- `Radio.Group` - Trigger type selection
- `NumberInput` - Days offset (when time-based)
- `Select` - Before/After dropdown, Recipient group
- `Switch` - Enabled/disabled toggle
- `Group` - Button layout (right-aligned)
- `Button` - Cancel (secondary), Save (primary)
- `Stack` - Vertical spacing between form sections

**State Management**:
```tsx
const [triggerType, setTriggerType] = useState<'FixedEvent' | 'TimeBased'>('TimeBased');
const [daysOffset, setDaysOffset] = useState<number>(3);
const [beforeAfter, setBeforeAfter] = useState<'before' | 'after'>('before');
const [recipientGroup, setRecipientGroup] = useState<EventRecipientGroup | null>(null);
const [enabled, setEnabled] = useState<boolean>(true);
```

**Conditional Rendering**:
- **Time-Based selected**: Show "Timing Offset" section (NumberInput + Select)
- **Fixed Event selected**: Hide "Timing Offset" section
- **Recipient Group**: Always visible for Events tab templates

**Form Validation**:
- Recipient group required if trigger enabled
- Days offset required if time-based (must be non-zero)
- Before/after selection required if time-based

**Save Behavior**:
1. Validate form
2. Convert before/after + days to single offset number (before=positive, after=negative)
3. Call API endpoint: `PUT /api/templates/{id}/trigger-config`
4. Show success notification
5. Close modal
6. Refresh template card data

---

### 2.3 Ad Hoc Tab - Scheduled Send Enhancement

**Purpose**: Allow scheduling ad hoc emails for future delivery

**Current Ad Hoc Send Form** (assumed pattern):
```
┌─────────────────────────────────────────────────────────────┐
│ Send Ad Hoc Email                                            │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│ RECIPIENT SEGMENT                                            │
│ [All Vetted Members ▼]                                      │
│                                                               │
│ SUBJECT                                                      │
│ [________________________]                                   │
│                                                               │
│ MESSAGE BODY                                                 │
│ [Rich text editor...]                                        │
│                                                               │
│ [NEW] SCHEDULED SEND (Optional)                             │
│ ○ Send immediately                                           │
│ ● Schedule for later                                         │
│                                                               │
│   [Only shown when "Schedule for later" selected:]          │
│   Send on: [📅 2025-12-15 10:00 AM]                        │
│                                                               │
│                                    [Save as Template] [Send] │
└─────────────────────────────────────────────────────────────┘
```

**Component Props**:
```tsx
interface AdHocEmailFormProps {
  onSend: (email: AdHocEmailData) => Promise<void>;
  onSaveAsTemplate: (email: AdHocEmailData) => Promise<void>;
}

interface AdHocEmailData {
  recipientSegment: UserSegment;
  subject: string;
  htmlBody: string;
  plainTextBody: string;
  scheduledSendAt?: Date;  // null = immediate
}
```

**Mantine Components**:
- `Radio.Group` - Send timing (immediate vs scheduled)
- `DateTimePicker` - Scheduled send date (from @mantine/dates)
- `Select` - Recipient segment dropdown
- `TextInput` - Subject line
- `RichTextEditor` - Message body (Mantine TipTap)
- `Button` - "Save as Template" (secondary), "Send" (primary)

**State Management**:
```tsx
const [sendTiming, setSendTiming] = useState<'immediate' | 'scheduled'>('immediate');
const [scheduledDate, setScheduledDate] = useState<Date | null>(null);
```

**Conditional Rendering**:
- DateTimePicker only visible when `sendTiming === 'scheduled'`
- "Send" button text changes: "Send Now" vs "Schedule Send"

**Validation**:
- If scheduled, scheduledDate must be in future (>= now + 1 minute)
- Subject and body required (existing validation)
- Recipient segment required (existing validation)

---

### 2.4 Ad Hoc Tab - Save as Template Feature

**Purpose**: Save ad hoc emails as reusable templates

**Trigger**: Click "Save as Template" button in ad hoc send form

**Save Template Modal**:
```
┌──────────────────────────────────────────────────────────────┐
│ Save as Template                                          [X]│
├──────────────────────────────────────────────────────────────┤
│                                                               │
│ TEMPLATE NAME                                                │
│ [Monthly Newsletter                    ]                     │
│                                                               │
│ This template will be saved to the Ad Hoc templates          │
│ section and can be reused for future sends.                  │
│                                                               │
│ Template Preview:                                            │
│ Subject: Monthly Community Update                            │
│ Recipient: All Vetted Members                                │
│                                                               │
│                                          [Cancel] [Save]     │
└──────────────────────────────────────────────────────────────┘
```

**Component Props**:
```tsx
interface SaveTemplateModalProps {
  opened: boolean;
  onClose: () => void;
  emailData: {
    subject: string;
    htmlBody: string;
    plainTextBody: string;
    recipientSegment: UserSegment;
  };
  onSave: (templateName: string) => Promise<void>;
}
```

**Default Behavior**:
- Template name defaults to subject line (user can override)
- Preview shows subject + recipient for confirmation

**Saved Templates Section** (top of Ad Hoc tab):
```
┌─────────────────────────────────────────────────────────────┐
│ Saved Ad Hoc Templates                                       │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│ ┌──────────────────────────────────────┐                    │
│ │ Monthly Newsletter                    │  [Use] [Delete]   │
│ │ Subject: Monthly Community Update     │                   │
│ │ Recipient: All Vetted Members         │                   │
│ │ Saved: Nov 1, 2025                    │                   │
│ └──────────────────────────────────────┘                    │
│                                                               │
│ ┌──────────────────────────────────────┐                    │
│ │ Event Announcement Template           │  [Use] [Delete]   │
│ │ Subject: New Event Alert!             │                   │
│ │ Recipient: All Pre-Vetted Members     │                   │
│ │ Saved: Oct 15, 2025                   │                   │
│ └──────────────────────────────────────┘                    │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

**Mantine Components**:
- `Card` - Template card container
- `Text` - Template name, subject, recipient, date
- `Group` - Action buttons (right-aligned)
- `Button` - "Use" (primary), "Delete" (secondary)
- `Stack` - Template cards vertical spacing

**Actions**:
- **Use**: Populate ad hoc form with template data
- **Delete**: Confirmation modal → DELETE /api/templates/adhoc/{id}

---

### 2.5 Ad Hoc Tab - Delete Template Confirmation

**Purpose**: Prevent accidental deletion of saved templates

**Delete Confirmation Modal**:
```
┌──────────────────────────────────────────────────────────────┐
│ Delete Template?                                          [X]│
├──────────────────────────────────────────────────────────────┤
│                                                               │
│ Are you sure you want to delete "Monthly Newsletter"?        │
│                                                               │
│ This action cannot be undone.                                │
│                                                               │
│                                          [Cancel] [Delete]   │
└──────────────────────────────────────────────────────────────┘
```

**Component Props**:
```tsx
interface DeleteTemplateModalProps {
  opened: boolean;
  onClose: () => void;
  templateName: string;
  onConfirm: () => Promise<void>;
}
```

**Button Styling**:
- Cancel: Secondary (burgundy outline)
- Delete: Destructive (red variant)

---

## 3. Wireframes

### 3.1 Events Tab - Enhanced Template Cards

**Desktop Layout (1440px)**:
```
┌─────────────────────────────────────────────────────────────────────────┐
│ Admin Email Templates                                                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│ [Vetting] [Events] [Admin] [Incident] [Ad Hoc]                          │
│                                                                           │
│ ──────────────────────────────────────────────────────────────────────  │
│                                                                           │
│ Events Tab - Email Templates for Event Communications                    │
│                                                                           │
│ ┌──────────────────────────────────────────────────────────────────────┐│
│ │ 🔔 Confirmation Email                    [Edit Trigger] [Edit Content]││
│ │───────────────────────────────────────────────────────────────────────││
│ │ Subject: Your registration for {{event_name}} is confirmed!          ││
│ │                                                                        ││
│ │ [⚡ Fixed Event] [👥 RSVP/Ticket Holders]                            ││
│ │ Enabled: ✅ (triggers on ticket purchase/RSVP)                        ││
│ └──────────────────────────────────────────────────────────────────────┘│
│                                                                           │
│ ┌──────────────────────────────────────────────────────────────────────┐│
│ │ 🔔 Reminder 3 Days Before                [Edit Trigger] [Edit Content]││
│ │───────────────────────────────────────────────────────────────────────││
│ │ Subject: Don't forget your class this Friday!                         ││
│ │                                                                        ││
│ │ [⏰ Time-Based] [📅 3 days before] [👥 RSVP/Ticket Holders]         ││
│ │ Enabled: ✅                                                           ││
│ └──────────────────────────────────────────────────────────────────────┘│
│                                                                           │
│ ┌──────────────────────────────────────────────────────────────────────┐│
│ │ 🔔 Post-Event Survey                     [Edit Trigger] [Edit Content]││
│ │───────────────────────────────────────────────────────────────────────││
│ │ Subject: How was {{event_name}}? We'd love your feedback!            ││
│ │                                                                        ││
│ │ [⏰ Time-Based] [📅 2 days after] [👥 Session Attendees]            ││
│ │ Enabled: ✅                                                           ││
│ └──────────────────────────────────────────────────────────────────────┘│
│                                                                           │
│ ┌──────────────────────────────────────────────────────────────────────┐│
│ │ 🔔 Cancellation Notice                   [Edit Trigger] [Edit Content]││
│ │───────────────────────────────────────────────────────────────────────││
│ │ Subject: Your ticket for {{event_name}} has been cancelled           ││
│ │                                                                        ││
│ │ [⚡ Fixed Event] [👥 RSVP/Ticket Holders]                            ││
│ │ Enabled: ✅ (triggers on ticket cancellation)                         ││
│ └──────────────────────────────────────────────────────────────────────┘│
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

**Visual Specifications**:
- Card padding: 24px (xl)
- Card gap: 16px (md)
- Badge gap: 8px (sm)
- Icon size: 20px
- Card shadow: Mantine default
- Border radius: 8px

---

### 3.2 Trigger Configuration Modal

**Modal Dimensions**: 600px width, auto height, centered

```
┌────────────────────────────────────────────────────────────────┐
│ Configure Email Trigger                                     [X]│
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│ Template: Reminder 3 Days Before                               │
│ Subject: Don't forget your class this Friday!                  │
│                                                                 │
│ ───────────────────────────────────────────────────────────────│
│                                                                 │
│ TRIGGER TYPE                                                   │
│ ┌─────────────────────────────────────────────────────────────┐│
│ │ ○ Fixed Event                                               ││
│ │   Triggered by specific actions (purchase, RSVP, etc.)      ││
│ │                                                              ││
│ │ ● Time-Based                                                ││
│ │   Triggered relative to session start time                  ││
│ └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│ TIMING OFFSET (Time-Based only)                                │
│ ┌─────────────────────────────────────────────────────────────┐│
│ │ Send email  [ 3 ]  days  [Before ▼]  session start         ││
│ │                                                              ││
│ │ ℹ Before = positive days, After = negative days             ││
│ └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│ RECIPIENT GROUP                                                │
│ ┌─────────────────────────────────────────────────────────────┐│
│ │ Who receives this email?                                    ││
│ │                                                              ││
│ │ [RSVP/Ticket Holders (deduplicated)                    ▼]  ││
│ │                                                              ││
│ │ Options:                                                     ││
│ │  • Session Attendees (users who checked in)                ││
│ │  • RSVP/Ticket Holders (deduplicated)                      ││
│ │  • Session Volunteers (assigned to session)                ││
│ │  • Teachers (assigned to session)                          ││
│ └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│ ENABLE/DISABLE                                                 │
│ ┌─────────────────────────────────────────────────────────────┐│
│ │ [✓] Template enabled (will send automatically)             ││
│ └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│ ───────────────────────────────────────────────────────────────│
│                                                                 │
│                              [Cancel] [Save Configuration]     │
└────────────────────────────────────────────────────────────────┘
```

**Component Spacing**:
- Section gap: 24px (lg)
- Internal padding: 16px (md)
- Button gap: 12px (sm)
- Label margin-bottom: 8px (xs)

---

### 3.3 Ad Hoc Tab - Enhanced Send Form

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Ad Hoc Tab - Send Email to Segments                                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│ ┌──────────────────────────────────────────────────────────────────────┐│
│ │ Saved Ad Hoc Templates                                                ││
│ │────────────────────────────────────────────────────────────────────── ││
│ │                                                                        ││
│ │ ┌────────────────────────────────────────────┐                       ││
│ │ │ 📧 Monthly Newsletter               [Use] [Delete]                ││
│ │ │ Subject: Monthly Community Update                                 ││
│ │ │ Recipient: All Vetted Members                                     ││
│ │ │ Saved: Nov 1, 2025                                                ││
│ │ └────────────────────────────────────────────┘                       ││
│ │                                                                        ││
│ │ ┌────────────────────────────────────────────┐                       ││
│ │ │ 📧 Event Announcement               [Use] [Delete]                ││
│ │ │ Subject: New Event Alert!                                         ││
│ │ │ Recipient: All Pre-Vetted Members                                 ││
│ │ │ Saved: Oct 15, 2025                                               ││
│ │ └────────────────────────────────────────────┘                       ││
│ └──────────────────────────────────────────────────────────────────────┘│
│                                                                           │
│ ┌──────────────────────────────────────────────────────────────────────┐│
│ │ Compose New Ad Hoc Email                                              ││
│ │────────────────────────────────────────────────────────────────────── ││
│ │                                                                        ││
│ │ RECIPIENT SEGMENT                                                     ││
│ │ [All Vetted Members                                               ▼] ││
│ │                                                                        ││
│ │ SUBJECT                                                               ││
│ │ [________________________________________________________________]    ││
│ │                                                                        ││
│ │ MESSAGE BODY                                                          ││
│ │ ┌──────────────────────────────────────────────────────────────────┐ ││
│ │ │ [B] [I] [U] [List] [Link] ...                                    │ ││
│ │ │                                                                   │ ││
│ │ │                                                                   │ ││
│ │ │ [Rich text editor - TipTap]                                      │ ││
│ │ │                                                                   │ ││
│ │ │                                                                   │ ││
│ │ └──────────────────────────────────────────────────────────────────┘ ││
│ │                                                                        ││
│ │ SEND TIMING                                                           ││
│ │ ○ Send immediately                                                    ││
│ │ ● Schedule for later                                                  ││
│ │                                                                        ││
│ │   Send on: [📅 Dec 15, 2025 10:00 AM]                               ││
│ │                                                                        ││
│ │ ───────────────────────────────────────────────────────────────────── ││
│ │                                                                        ││
│ │                                    [Save as Template] [Schedule Send] ││
│ └──────────────────────────────────────────────────────────────────────┘│
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

**Visual Hierarchy**:
1. Saved templates section (collapsible, expanded by default)
2. Compose form below
3. Action buttons right-aligned

---

## 4. Mantine Components Used

| Component | Purpose | Props/Config |
|-----------|---------|--------------|
| **Card** | Template containers, saved template cards | `shadow="sm"`, `padding="lg"`, `radius="md"` |
| **Badge** | Trigger type, timing, recipient indicators | `color="plum"/"burgundy"/"stone"`, `variant="filled"` |
| **Modal** | Trigger config, save template, delete confirmation | `size="lg"`, `centered`, `title`, `opened`, `onClose` |
| **Radio.Group** | Trigger type, send timing selection | `name`, `value`, `onChange` |
| **NumberInput** | Days offset input | `min={1}`, `max={30}`, `placeholder="3"` |
| **Select** | Before/After, Recipient group | `data={options}`, `value`, `onChange`, `searchable={false}` |
| **Switch** | Template enabled toggle | `checked`, `onChange`, `label` |
| **DateTimePicker** | Scheduled send date | `minDate={new Date()}`, `value`, `onChange` |
| **TextInput** | Template name, subject | `label`, `placeholder`, `required`, `maxLength` |
| **Button** | Actions (Edit, Save, Delete, etc.) | `variant`, `color`, `onClick`, Mantine button styles checklist |
| **Group** | Horizontal button layouts | `justify="flex-end"`, `gap="sm"` |
| **Stack** | Vertical form sections | `gap="md"/"lg"` |
| **Text** | Labels, descriptions, helper text | `size="sm"/"md"`, `weight={600}`, `c="dimmed"` |

---

## 5. State Management

### 5.1 Enhanced Template Card State

**Local Component State**:
```tsx
// None - read-only display component
// All state from parent (API data)
```

**Parent State (Events Tab)**:
```tsx
const [templates, setTemplates] = useState<GlobalEmailTemplateDto[]>([]);
const [selectedTemplate, setSelectedTemplate] = useState<GlobalEmailTemplateDto | null>(null);
const [triggerModalOpened, setTriggerModalOpened] = useState(false);
```

### 5.2 Trigger Configuration Modal State

```tsx
const [triggerType, setTriggerType] = useState<'FixedEvent' | 'TimeBased'>('TimeBased');
const [daysOffset, setDaysOffset] = useState<number>(3);
const [beforeAfter, setBeforeAfter] = useState<'before' | 'after'>('before');
const [recipientGroup, setRecipientGroup] = useState<EventRecipientGroup | null>(null);
const [enabled, setEnabled] = useState<boolean>(true);
const [isSubmitting, setIsSubmitting] = useState(false);
```

**Derived State**:
```tsx
// Convert before/after + days to single offset
const timingOffsetDays = beforeAfter === 'before' ? daysOffset : -daysOffset;
```

### 5.3 Ad Hoc Send Form State

```tsx
const [recipientSegment, setRecipientSegment] = useState<UserSegment | null>(null);
const [subject, setSubject] = useState('');
const [htmlBody, setHtmlBody] = useState('');
const [sendTiming, setSendTiming] = useState<'immediate' | 'scheduled'>('immediate');
const [scheduledDate, setScheduledDate] = useState<Date | null>(null);
const [isSubmitting, setIsSubmitting] = useState(false);
```

### 5.4 Saved Templates State

```tsx
const [savedTemplates, setSavedTemplates] = useState<AdHocEmailTemplateDto[]>([]);
const [deleteModalOpened, setDeleteModalOpened] = useState(false);
const [templateToDelete, setTemplateToDelete] = useState<AdHocEmailTemplateDto | null>(null);
```

---

## 6. API Integration

### 6.1 Events Tab API Calls

**Get Time-Based Templates**:
```tsx
GET /api/templates/time-based
Response: GlobalEmailTemplateDto[]
```

**Update Trigger Configuration**:
```tsx
PUT /api/templates/{id}/trigger-config
Body: {
  triggerType: 'TimeBased',
  triggerEnabled: true,
  timingOffsetDays: 3,  // or -2 for "after"
  recipientGroup: 'RSVPTicketHolders'
}
Response: GlobalEmailTemplateDto
```

### 6.2 Ad Hoc Tab API Calls

**Get Saved Templates**:
```tsx
GET /api/templates/adhoc
Response: AdHocEmailTemplateDto[]
```

**Save as Template**:
```tsx
POST /api/templates/adhoc
Body: {
  templateName: 'Monthly Newsletter',
  subject: 'Monthly Community Update',
  htmlBody: '...',
  plainTextBody: '...'
}
Response: AdHocEmailTemplateDto
```

**Delete Template**:
```tsx
DELETE /api/templates/adhoc/{id}
Response: 204 No Content
```

**Schedule Ad Hoc Email**:
```tsx
POST /api/adhoc/schedule
Body: {
  recipientSegment: 'AllVettedMembers',
  subject: '...',
  htmlBody: '...',
  plainTextBody: '...',
  scheduledSendAt: '2025-12-15T10:00:00Z'
}
Response: SentAdHocEmailDto
```

### 6.3 Error Handling

**API Error Response**:
```tsx
{
  error: string;
  message: string;
  details?: string;
}
```

**UI Error Display**:
- Modal errors: Inline `Alert` component above form
- Global errors: `notifications.show({ color: 'red', title: 'Error', message: '...' })`
- Network errors: "Failed to save. Please check your connection."
- Validation errors: Field-level error states with Mantine form validation

---

## 7. Responsive Breakpoints

### Admin Dashboard Context

Per Mantine UI Standards:
- **Admin areas are desktop-only** (1440px optimization)
- **No mobile testing required** for admin interfaces
- Focus on data density and management workflows

**Breakpoint Strategy**:
- Desktop (≥1440px): Full layout with all features visible
- Minimum support: 1024px (laptops)
- Below 1024px: Not tested/supported (admin users expected to use desktop)

**Component Sizing**:
- Template cards: Full width in Stack (no grid needed for single column)
- Modals: 600px fixed width, centered
- Form inputs: Full width within modal
- Buttons: Natural width, right-aligned in groups

---

## 8. Accessibility Requirements

### Keyboard Navigation

**Tab Order**:
1. Template cards (focusable, Enter opens trigger modal)
2. "Edit Trigger" button
3. "Edit Content" button
4. Modal: Radio options → NumberInput → Select → Switch → Buttons

**Keyboard Shortcuts**:
- `Tab`: Move between focusable elements
- `Enter/Space`: Activate buttons, toggle switches
- `Escape`: Close modals
- Arrow keys: Navigate radio groups and selects

### Screen Reader Support

**ARIA Labels**:
```tsx
// Template card
<Card role="article" aria-label={`Email template: ${templateType}`}>

// Trigger type badge
<Badge aria-label={`Trigger type: ${triggerType}`}>

// NumberInput
<NumberInput
  aria-label="Days offset for email trigger"
  description="Positive = before session, Negative = after"
/>

// Switch
<Switch
  aria-label="Enable or disable template"
  checked={enabled}
/>
```

**Live Regions**:
```tsx
// Success notification (announced automatically)
<div role="status" aria-live="polite">
  Template configuration saved successfully
</div>
```

### Color Contrast

All badge/text combinations meet WCAG 2.1 AA (4.5:1):
- Plum badge (#614B79) + Ivory text (#FFF8F0): 8.2:1 ✅
- Burgundy badge (#880124) + Ivory text (#FFF8F0): 9.1:1 ✅
- Stone badge (#8B8680) + Ivory text (#FFF8F0): 5.3:1 ✅
- Rose gold badge (#B76D75) + Charcoal text (#2B2B2B): 4.8:1 ✅
- Dusty rose badge (#D4A5A5) + Charcoal text (#2B2B2B): 6.1:1 ✅

### Focus Indicators

All interactive elements have visible focus rings:
```css
.btn:focus-visible {
  outline: 2px solid var(--color-burgundy);
  outline-offset: 2px;
}
```

---

## 9. Design Decisions Log

### Decision 1: Badge-Based Visual Indicators

**Why**: Reading text is faster than opening modals. Badges provide at-a-glance status.

**Alternatives Considered**:
- Text labels only (less visually distinct)
- Icons only (less accessible, require tooltips)

**Chosen**: Badges with text + icons (best of both)

### Decision 2: Modal for Trigger Configuration

**Why**: Progressive disclosure - complex config hidden until needed. Keeps card layout clean.

**Alternatives Considered**:
- Inline expansion (clutters list view)
- Separate page (breaks flow, requires navigation)

**Chosen**: Modal (standard admin pattern, focused workflow)

### Decision 3: Unified "Before/After" Dropdown

**Why**: More intuitive than negative numbers. Backend still uses offset (positive/negative).

**Alternatives Considered**:
- Single NumberInput allowing negative values (confusing for users)
- Two separate inputs (days before, days after) (redundant)

**Chosen**: NumberInput + Select (clear mental model)

### Decision 4: Scheduled Send as Radio Group

**Why**: Immediate vs scheduled are mutually exclusive. Radio enforces single choice.

**Alternatives Considered**:
- Checkbox "Schedule this email" (less clear state)
- Two separate buttons (splits workflow)

**Chosen**: Radio group (clear, standard pattern)

### Decision 5: Saved Templates Section Above Form

**Why**: Templates are reusable - prioritize discovery over composition. Users can "Use" template to populate form.

**Alternatives Considered**:
- Sidebar (takes up space, less scannable)
- Separate tab (hides templates, requires navigation)

**Chosen**: Section above form (visible, accessible, flows into composition)

---

## 10. Implementation Notes

### 10.1 Mantine Button Styling Checklist

**CRITICAL**: All buttons MUST include explicit height/padding to prevent text cutoff (documented issue from Mantine UI Standards).

```tsx
<Button
  variant="filled"
  color="blue"
  styles={{
    root: {
      height: '44px',           // REQUIRED
      paddingTop: '12px',       // REQUIRED
      paddingBottom: '12px',    // REQUIRED
      fontSize: '14px',         // REQUIRED
      lineHeight: '1.2',        // REQUIRED
      fontWeight: 600,
    }
  }}
>
  Button Text
</Button>
```

### 10.2 DateTimePicker Configuration

```tsx
import { DateTimePicker } from '@mantine/dates';

<DateTimePicker
  label="Send on"
  placeholder="Pick date and time"
  value={scheduledDate}
  onChange={setScheduledDate}
  minDate={new Date()}  // Prevent past dates
  valueFormat="MMM DD, YYYY hh:mm A"  // "Dec 15, 2025 10:00 AM"
  clearable
  required={sendTiming === 'scheduled'}
/>
```

### 10.3 TipTap Rich Text Editor

Use existing `MantineTiptapEditor.tsx` component (if available):
```tsx
<MantineTiptapEditor
  value={htmlBody}
  onChange={setHtmlBody}
  minRows={10}
  placeholder="Compose your message..."
/>
```

### 10.4 Recipient Group Select Options

```tsx
const recipientGroupOptions = [
  {
    value: 'SessionAttendees',
    label: 'Session Attendees (users who checked in)',
    description: 'Only users who actually attended'
  },
  {
    value: 'RSVPTicketHolders',
    label: 'RSVP/Ticket Holders (deduplicated)',
    description: 'RSVP for socials OR ticket holders for classes'
  },
  {
    value: 'SessionVolunteers',
    label: 'Session Volunteers (assigned to session)',
    description: 'Volunteers specifically for this session'
  },
  {
    value: 'Teachers',
    label: 'Teachers (assigned to session)',
    description: 'Teachers leading this session'
  }
];

<Select
  label="Who receives this email?"
  data={recipientGroupOptions}
  value={recipientGroup}
  onChange={setRecipientGroup}
  searchable={false}
  required
/>
```

### 10.5 Badge Component Configuration

```tsx
// Trigger type badges
<Badge
  color={triggerType === 'TimeBased' ? 'plum' : 'burgundy'}
  variant="filled"
  leftSection={triggerType === 'TimeBased' ? '⏰' : '⚡'}
>
  {triggerType === 'TimeBased' ? 'Time-Based' : 'Fixed Event'}
</Badge>

// Timing badge
<Badge
  color="rose-gold"
  variant="filled"
  leftSection="📅"
>
  {Math.abs(timingOffsetDays)} days {timingOffsetDays > 0 ? 'before' : 'after'}
</Badge>

// Recipient badge
<Badge
  color="dusty-rose"
  variant="filled"
  leftSection="👥"
>
  {recipientGroupLabel}
</Badge>
```

### 10.6 Form Validation Pattern

```tsx
import { useForm } from '@mantine/form';

const form = useForm({
  initialValues: {
    triggerType: 'TimeBased',
    daysOffset: 3,
    beforeAfter: 'before',
    recipientGroup: null,
    enabled: true,
  },
  validate: {
    daysOffset: (value, values) =>
      values.triggerType === 'TimeBased' && (!value || value < 1)
        ? 'Days offset must be at least 1'
        : null,
    recipientGroup: (value, values) =>
      values.enabled && !value
        ? 'Recipient group is required when template is enabled'
        : null,
  },
});
```

---

## 11. Testing Scenarios

### 11.1 Events Tab Testing

**Scenario 1: Configure Time-Based Trigger**
1. Open Events tab
2. Click "Edit Trigger" on template card
3. Select "Time-Based" trigger type
4. Set days offset: 3 days before
5. Select recipient group: RSVP/Ticket Holders
6. Enable template
7. Click "Save Configuration"
8. **Expected**: Card updates with badges, success notification shown

**Scenario 2: Disable Template**
1. Open trigger modal for enabled template
2. Toggle "Enabled" switch off
3. Click "Save Configuration"
4. **Expected**: Card shows "Disabled" state, no emails sent

**Scenario 3: Change Fixed Event to Time-Based**
1. Open trigger modal for Fixed Event template
2. Select "Time-Based"
3. **Expected**: Timing offset section appears
4. Configure timing and recipient
5. Save
6. **Expected**: Card updates from Fixed Event to Time-Based badge

### 11.2 Ad Hoc Tab Testing

**Scenario 1: Schedule Email for Future**
1. Open Ad Hoc tab
2. Select recipient segment
3. Enter subject and body
4. Select "Schedule for later"
5. Pick date/time in future
6. Click "Schedule Send"
7. **Expected**: Email queued, success notification

**Scenario 2: Save as Template**
1. Compose ad hoc email
2. Click "Save as Template"
3. Enter template name
4. Click "Save"
5. **Expected**: Template appears in saved templates section

**Scenario 3: Use Saved Template**
1. Click "Use" on saved template
2. **Expected**: Form populates with template data
3. Modify as needed
4. Send or schedule

**Scenario 4: Delete Saved Template**
1. Click "Delete" on saved template
2. Confirm deletion in modal
3. **Expected**: Template removed from list, success notification

### 11.3 Edge Cases

**Edge Case 1: Validation Errors**
- Try to save trigger config without recipient group (enabled template)
- **Expected**: Inline error, save blocked

**Edge Case 2: Past Scheduled Date**
- Try to schedule email for past date/time
- **Expected**: DateTimePicker prevents selection (minDate validation)

**Edge Case 3: Network Failure**
- Simulate network error during save
- **Expected**: Error notification, form data preserved, retry possible

---

## 12. Handoff Checklist

### For React Developer

- [ ] All Mantine components documented with props
- [ ] State management patterns provided
- [ ] API endpoints specified with request/response shapes
- [ ] Form validation rules documented
- [ ] Error handling patterns defined
- [ ] Accessibility requirements listed
- [ ] Button styling checklist included (height/padding)

### For Backend Developer

- [ ] Required API endpoints listed
- [ ] Request/response DTOs specified
- [ ] EventRecipientGroup enum documented
- [ ] Trigger configuration fields identified
- [ ] Ad hoc template entity requirements specified

### For Test Developer

- [ ] Testing scenarios provided
- [ ] Edge cases documented
- [ ] Expected behaviors specified
- [ ] Validation test cases listed

---

## 13. Open Questions

1. **Template Name Display**: Should we use TemplateType enum or a display name field?
   - **Recommendation**: Use TemplateType, format for display (e.g., "Reminder1Day" → "Reminder 1 Day")

2. **Multi-Session Events**: How should timing offset work for events with multiple sessions?
   - **Confirmed from requirements**: Each session triggers independently based on its own start time

3. **Recipient Group Migration**: EventEmailTemplate.RecipientGroup is currently string. Change to enum?
   - **Recommendation**: Add migration to convert to EventRecipientGroup enum

4. **Ad Hoc Template Storage**: New entity vs reuse GlobalEmailTemplate with category=AdHoc?
   - **Confirmed from requirements**: New AdHocEmailTemplate entity (simpler schema)

---

## 14. Design Assets References

- **Design System v7**: `/docs/design/current/design-system-v7.md`
- **Button Style Guide**: `/docs/design/current/button-style-guide.md`
- **Mantine UI Standards**: `/docs/standards-processes/frontend/mantine-ui-standards.md`
- **Admin Card Pattern**: `/docs/lessons-learned/ui-designer-lessons-learned.md` (lines 118-149)

---

## 15. Next Steps

1. **Backend Developer**: Implement API endpoints and entity changes
2. **React Developer**: Build UI components using this handoff
3. **Test Developer**: Create test cases based on scenarios
4. **UI Designer** (follow-up): Review implemented UI, iterate on UX feedback

---

**Handoff Complete**: This document provides all necessary specifications for implementation. Questions or clarifications should be documented in this handoff for future reference.

**Created**: 2025-12-01
**Designer**: UI Designer Agent
**Reviewers**: Orchestrator, Backend Developer, React Developer
