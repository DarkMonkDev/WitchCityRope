<!-- Last Updated: 2025-10-06 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Design Options - Pending Stakeholder Review -->

# Vetting Application Detail: Action Button UX Design Options

## Document Purpose

This document presents **3 comprehensive UX solutions** for the vetting application detail page action buttons, addressing the critical missing functionality: **Advance to Next Stage**.

## Problem Statement

### Current Implementation Issues

1. **Missing Primary Action**: "Advance to Next Stage" button is absent
2. **Equal Visual Weight**: All buttons (Approve, On Hold, Deny, Send Reminder) have the same prominence
3. **No Workflow Clarity**: Users cannot easily identify the next logical step in the vetting process
4. **Action Type Confusion**: Primary actions (immediate) and secondary actions (modal-based) are visually identical

### Business Requirements

**Vetting Workflow Stages**:
1. **UnderReview** → Next: InterviewApproved ("Approve for Interview")
2. **InterviewApproved** → Next: InterviewScheduled ("Schedule Interview")
3. **InterviewScheduled** → Next: FinalReview ("Mark Interview Complete")
4. **FinalReview** → Next: Approved ("Approve Application")
5. **Approved** (terminal state)
6. **Denied** (terminal state)
7. **OnHold** (can resume to previous state)

**Action Categories**:

**Primary Actions** (immediate execution with auto-send template email):
- **Advance to Next Stage** - Most prominent, label changes based on status
- **Skip to Approved** - Less prominent, but still primary

**Secondary Actions** (open email template editor):
- **Put On Hold** - Opens modal with customizable email
- **Send Reminder** - Opens modal with customizable email
- **Deny Application** - Opens modal with customizable email

---

## Design Option 1: Split Button with Action Groups

### Visual Description

**Layout**: Primary split button on the left, grouped secondary actions on the right

```
┌─────────────────────────────────────────────────────────────────┐
│  ACTIONS                                                        │
│                                                                 │
│  ┌───────────────────────────┐  ┌──────────┐  ┌──────────┐   │
│  │ Approve for Interview  ▼  │  │ On Hold  │  │ Reminder │   │
│  └───────────────────────────┘  └──────────┘  └──────────┘   │
│         (Electric Purple)         (Outlined)    (Outlined)    │
│                                                                 │
│  Dropdown Menu:                                                │
│  ┌───────────────────────────┐                                │
│  │ ✓ Approve for Interview   │ ← Current stage action        │
│  │ Skip to Final Approval    │ ← Skip ahead option           │
│  │ Put Application On Hold   │ ← Status management           │
│  │ ─────────────────────     │                               │
│  │ Deny Application          │ ← Destructive action (red)    │
│  └───────────────────────────┘                                │
└─────────────────────────────────────────────────────────────────┘
```

### Mantine Component Structure

```typescript
import { Button, Menu, Group, Stack, Text } from '@mantine/core';
import { IconChevronDown, IconCheck, IconClock, IconX, IconMail } from '@tabler/icons-react';

// Primary split button component
<Group gap="md" justify="space-between">
  {/* Left: Split Button Group */}
  <Menu shadow="md" width={280}>
    <Menu.Target>
      <Button
        size="lg"
        leftSection={<IconCheck size={18} />}
        rightSection={<IconChevronDown size={16} />}
        color="electric"
        styles={{
          root: {
            background: 'linear-gradient(135deg, #9D4EDD, #7B2CBF)',
            borderRadius: '12px 6px 12px 6px',
            height: '48px',
            fontSize: '14px',
            fontWeight: 600,
            textTransform: 'uppercase',
            letterSpacing: '1px'
          }
        }}
      >
        {getNextStageLabel(currentStatus)}
      </Button>
    </Menu.Target>

    <Menu.Dropdown>
      <Menu.Label>Workflow Actions</Menu.Label>
      <Menu.Item
        leftSection={<IconCheck size={16} />}
        onClick={handleAdvanceStage}
        color="electric"
      >
        {getNextStageLabel(currentStatus)}
      </Menu.Item>
      <Menu.Item
        leftSection={<IconCheck size={16} />}
        onClick={handleSkipToApproved}
      >
        Skip to Final Approval
      </Menu.Item>

      <Menu.Divider />

      <Menu.Label>Status Management</Menu.Label>
      <Menu.Item
        leftSection={<IconClock size={16} />}
        onClick={handleOnHold}
        color="yellow"
      >
        Put Application On Hold
      </Menu.Item>

      <Menu.Divider />

      <Menu.Item
        leftSection={<IconX size={16} />}
        onClick={handleDeny}
        color="red"
      >
        Deny Application
      </Menu.Item>
    </Menu.Dropdown>
  </Menu>

  {/* Right: Secondary Action Buttons */}
  <Group gap="sm">
    <Button
      variant="outline"
      color="gray"
      leftSection={<IconMail size={16} />}
      onClick={handleSendReminder}
      styles={{
        root: {
          height: '44px',
          borderRadius: '12px 6px 12px 6px'
        }
      }}
    >
      Send Reminder
    </Button>
  </Group>
</Group>
```

### Label Logic Function

```typescript
const getNextStageLabel = (status: VettingStatus): string => {
  const labels: Record<VettingStatus, string> = {
    UnderReview: 'Approve for Interview',
    InterviewApproved: 'Schedule Interview',
    InterviewScheduled: 'Mark Interview Complete',
    FinalReview: 'Approve Application',
    Approved: 'Application Approved',
    Denied: 'Application Denied',
    OnHold: 'Resume Application'
  };

  return labels[status] || 'Advance Application';
};
```

### Accessibility Considerations

**Keyboard Navigation**:
- Tab to focus split button
- Enter/Space to open menu
- Arrow keys to navigate menu items
- Enter to select menu item
- Escape to close menu

**Screen Reader Support**:
```typescript
<Button
  aria-label={`Primary action: ${getNextStageLabel(currentStatus)}. Press to expand menu for more options.`}
  aria-haspopup="menu"
  aria-expanded={menuOpened}
>
  {getNextStageLabel(currentStatus)}
</Button>
```

**Focus Management**:
- Focus returns to split button after menu close
- Visual focus indicator (2px burgundy outline)
- Menu items have hover and focus states

**WCAG Compliance**:
- Color contrast 4.5:1 minimum for all text
- Touch targets minimum 44px height
- Descriptive labels for all actions
- Status changes announced to screen readers

### Pros

✅ **Industry Standard Pattern**: Split buttons are familiar to users from Gmail, Microsoft Office
✅ **Clear Hierarchy**: Primary action is immediately visible and prominent
✅ **Space Efficient**: All actions accessible without cluttering the interface
✅ **Scalable**: Easy to add new actions to the dropdown menu
✅ **Mobile Friendly**: Dropdown works well on touch devices
✅ **Visual Clarity**: Electric purple gradient clearly indicates primary action

### Cons

❌ **Discoverability**: Users might not notice the dropdown arrow initially
❌ **Hover Required**: Desktop users must hover/click to see all options
❌ **Menu Depth**: Secondary actions are hidden one level deep
❌ **Touch Precision**: Small dropdown arrow on mobile requires careful tapping
❌ **Learning Curve**: Users unfamiliar with split buttons might be confused

### When This Pattern Works Best

**Ideal Scenarios**:
- Users who process many applications sequentially (admin power users)
- Workflows where the primary action is used 80%+ of the time
- Desktop-first environments with keyboard shortcuts
- Applications with many possible actions per item

**Not Recommended When**:
- Mobile-first design required
- All actions are equally important
- Users need to see all options at once
- Infrequent users who need maximum clarity

---

## Design Option 2: Tiered Button Groups with Visual Hierarchy

### Visual Description

**Layout**: Three-tier visual hierarchy using size, color, and spacing

```
┌─────────────────────────────────────────────────────────────────┐
│  ACTIONS                                                        │
│                                                                 │
│  Primary Action (Large, Electric Purple Gradient)              │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │          ✓  APPROVE FOR INTERVIEW                       │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Secondary Actions (Medium, Grouped by Type)                   │
│  ┌──────────────────────┐  ┌──────────────────────┐          │
│  │ Skip to Approved  ✓  │  │ Put On Hold  ⏸       │          │
│  └──────────────────────┘  └──────────────────────┘          │
│      (Outlined Green)           (Outlined Yellow)             │
│                                                                 │
│  Tertiary Actions (Small, Low Emphasis)                        │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐             │
│  │ Reminder   │  │ Deny       │  │ Notes      │             │
│  └────────────┘  └────────────┘  └────────────┘             │
│    (Text Link)     (Text Red)      (Text Gray)              │
└─────────────────────────────────────────────────────────────────┘
```

### Mantine Component Structure

```typescript
import { Button, Stack, Group, Text } from '@mantine/core';
import { IconCheck, IconClock, IconX, IconMail, IconNotes } from '@tabler/icons-react';

<Stack gap="lg">
  {/* Tier 1: Primary Next Stage Action */}
  <Button
    size="xl"
    fullWidth
    leftSection={<IconCheck size={20} />}
    onClick={handleAdvanceStage}
    disabled={!canAdvanceStage}
    styles={{
      root: {
        background: 'linear-gradient(135deg, #9D4EDD, #7B2CBF)',
        height: '56px',
        fontSize: '16px',
        fontWeight: 700,
        textTransform: 'uppercase',
        letterSpacing: '1.5px',
        borderRadius: '12px 6px 12px 6px',
        boxShadow: '0 4px 15px rgba(157, 78, 221, 0.4)',
        transition: 'all 0.3s ease',
        '&:hover': {
          borderRadius: '6px 12px 6px 12px',
          boxShadow: '0 6px 20px rgba(157, 78, 221, 0.6)',
          transform: 'scale(1.02)'
        }
      }
    }}
  >
    <Group justify="space-between" style={{ width: '100%' }}>
      <Text>{getNextStageLabel(currentStatus)}</Text>
      <Text size="sm" fw={400} style={{ opacity: 0.9 }}>
        {getNextStageDescription(currentStatus)}
      </Text>
    </Group>
  </Button>

  {/* Tier 2: Secondary Important Actions */}
  <Group gap="md" grow>
    <Button
      variant="outline"
      color="green"
      size="md"
      leftSection={<IconCheck size={18} />}
      onClick={handleSkipToApproved}
      disabled={!canSkipToApproved}
      styles={{
        root: {
          height: '48px',
          borderRadius: '12px 6px 12px 6px',
          borderWidth: '2px',
          fontWeight: 600,
          transition: 'all 0.3s ease',
          '&:hover': {
            borderRadius: '6px 12px 6px 12px'
          }
        }
      }}
    >
      Skip to Approved
    </Button>

    <Button
      variant="outline"
      color="yellow"
      size="md"
      leftSection={<IconClock size={18} />}
      onClick={handleOnHold}
      disabled={!canPutOnHold}
      styles={{
        root: {
          height: '48px',
          borderRadius: '12px 6px 12px 6px',
          borderWidth: '2px',
          fontWeight: 600,
          transition: 'all 0.3s ease',
          '&:hover': {
            borderRadius: '6px 12px 6px 12px'
          }
        }
      }}
    >
      Put On Hold
    </Button>
  </Group>

  {/* Tier 3: Tertiary Actions (Text Links) */}
  <Group gap="lg" justify="flex-start">
    <Button
      variant="subtle"
      color="gray"
      size="sm"
      leftSection={<IconMail size={16} />}
      onClick={handleSendReminder}
      styles={{
        root: {
          height: '36px',
          fontWeight: 500,
          textTransform: 'none'
        }
      }}
    >
      Send Reminder
    </Button>

    <Button
      variant="subtle"
      color="red"
      size="sm"
      leftSection={<IconX size={16} />}
      onClick={handleDeny}
      disabled={!canDeny}
      styles={{
        root: {
          height: '36px',
          fontWeight: 500,
          textTransform: 'none'
        }
      }}
    >
      Deny Application
    </Button>

    <Button
      variant="subtle"
      color="gray"
      size="sm"
      leftSection={<IconNotes size={16} />}
      onClick={handleAddNote}
      styles={{
        root: {
          height: '36px',
          fontWeight: 500,
          textTransform: 'none'
        }
      }}
    >
      Add Note
    </Button>
  </Group>
</Stack>
```

### Stage Description Helper

```typescript
const getNextStageDescription = (status: VettingStatus): string => {
  const descriptions: Record<VettingStatus, string> = {
    UnderReview: 'Send approval email and notify applicant',
    InterviewApproved: 'Coordinate interview time with applicant',
    InterviewScheduled: 'Move to final review after interview',
    FinalReview: 'Grant member access and send welcome email',
    Approved: 'Already approved',
    Denied: 'Application has been denied',
    OnHold: 'Resume from hold status'
  };

  return descriptions[status] || 'Advance to next stage';
};
```

### Accessibility Considerations

**Visual Hierarchy for Screen Readers**:
```typescript
<Stack gap="lg" role="group" aria-labelledby="actions-heading">
  <Text id="actions-heading" visuallyHidden>
    Application Actions
  </Text>

  {/* Primary action */}
  <Button
    aria-label={`Primary action: ${getNextStageLabel(currentStatus)}. ${getNextStageDescription(currentStatus)}`}
  >
    {/* Button content */}
  </Button>

  {/* Secondary actions */}
  <Group role="group" aria-label="Secondary actions">
    {/* Buttons */}
  </Group>

  {/* Tertiary actions */}
  <Group role="group" aria-label="Additional actions">
    {/* Buttons */}
  </Group>
</Stack>
```

**Keyboard Navigation**:
- Tab order: Primary → Secondary (left to right) → Tertiary (left to right)
- Disabled buttons skip in tab order with `aria-disabled="true"`
- Enter/Space activates focused button
- Visual focus indicators (2px burgundy outline with 4px offset)

**Touch Accessibility**:
- Primary button: 56px height (well above 44px minimum)
- Secondary buttons: 48px height (comfortable for touch)
- Tertiary buttons: 36px height (acceptable for less-frequent actions)
- 16px spacing between buttons (prevents mis-taps)

**Reduced Motion Support**:
```css
@media (prefers-reduced-motion: reduce) {
  .btn {
    transition: none;
    transform: none !important;
  }
}
```

### Pros

✅ **Immediate Clarity**: All actions visible at once, no hidden menus
✅ **Clear Visual Hierarchy**: Size and color instantly communicate importance
✅ **Excellent for Learning**: Users quickly understand action priority
✅ **Mobile Optimized**: Large touch targets for primary actions
✅ **Accessible by Default**: Screen readers get clear hierarchy
✅ **No Learning Curve**: Obvious what to do next
✅ **Supports Scanning**: Users can quickly scan all available options

### Cons

❌ **Vertical Space**: Takes more vertical room than horizontal layouts
❌ **Repetitive for Power Users**: Always showing all options can feel cluttered
❌ **Less Dense**: Doesn't work well in compact interfaces
❌ **Button Group Grows**: Adding new actions increases visual complexity
❌ **Not Scalable**: Limited to ~8 actions before becoming overwhelming

### When This Pattern Works Best

**Ideal Scenarios**:
- New or infrequent users who need guidance
- Mobile-first applications
- Workflows where multiple actions are commonly used
- When all action options should be discoverable without interaction
- Training environments or onboarding flows

**Not Recommended When**:
- Space is at a premium
- Users are experienced power users
- More than 8-10 possible actions
- Desktop-heavy workflows with keyboard shortcuts

---

## Design Option 3: Floating Action Button (FAB) with Contextual Menu

### Visual Description

**Layout**: Primary FAB button fixed to bottom-right of detail view, secondary actions in standard horizontal group

```
┌─────────────────────────────────────────────────────────────────┐
│  Application Detail Content                                     │
│                                                                 │
│  [Application information, notes, history...]                  │
│                                                                 │
│  Quick Actions:                                                │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                    │
│  │ On Hold  │  │ Reminder │  │ Deny     │                    │
│  └──────────┘  └──────────┘  └──────────┘                    │
│                                                                 │
│                                                                 │
│                                              ┌────────────┐    │
│                                              │            │    │
│                                              │  APPROVE   │    │
│                                              │    FOR     │    │
│                                              │ INTERVIEW  │    │
│                                              │            │    │
│                                              │     ✓      │    │
│                                              └────────────┘    │
│                                                  (Sticky      │
│                                                   FAB)        │
└─────────────────────────────────────────────────────────────────┘

On FAB Click → Expands to show options:
┌────────────────────────┐
│ Skip to Approved    ✓  │
│ ────────────────────   │
│ Approve for Interview✓ │ ← Default (highlighted)
└────────────────────────┘
```

### Mantine Component Structure

```typescript
import { Button, Group, Menu, Affix, Transition, Box, Text } from '@mantine/core';
import { IconCheck, IconClock, IconX, IconMail, IconChevronUp } from '@tabler/icons-react';
import { useWindowScroll } from '@mantine/hooks';

const VettingApplicationDetailActions = () => {
  const [scroll] = useWindowScroll();
  const [menuOpened, setMenuOpened] = useState(false);

  return (
    <>
      {/* Standard horizontal action bar */}
      <Paper p="md" radius="md" style={{ background: '#FFF8F0' }}>
        <Text fw={600} size="sm" c="dimmed" mb="sm">
          QUICK ACTIONS
        </Text>
        <Group gap="md">
          <Button
            variant="outline"
            color="yellow"
            leftSection={<IconClock size={16} />}
            onClick={handleOnHold}
            disabled={!canPutOnHold}
            styles={{
              root: {
                height: '44px',
                borderRadius: '12px 6px 12px 6px',
                fontWeight: 600
              }
            }}
          >
            Put On Hold
          </Button>

          <Button
            variant="outline"
            color="gray"
            leftSection={<IconMail size={16} />}
            onClick={handleSendReminder}
            styles={{
              root: {
                height: '44px',
                borderRadius: '12px 6px 12px 6px',
                fontWeight: 600
              }
            }}
          >
            Send Reminder
          </Button>

          <Button
            variant="outline"
            color="red"
            leftSection={<IconX size={16} />}
            onClick={handleDeny}
            disabled={!canDeny}
            styles={{
              root: {
                height: '44px',
                borderRadius: '12px 6px 12px 6px',
                fontWeight: 600
              }
            }}
          >
            Deny Application
          </Button>
        </Group>
      </Paper>

      {/* Floating Action Button (FAB) */}
      <Affix position={{ bottom: 20, right: 20 }}>
        <Transition transition="slide-up" mounted={scroll.y > 100}>
          {(transitionStyles) => (
            <Menu
              shadow="xl"
              width={260}
              position="top-end"
              opened={menuOpened}
              onChange={setMenuOpened}
            >
              <Menu.Target>
                <Button
                  size="xl"
                  style={{
                    ...transitionStyles,
                    background: 'linear-gradient(135deg, #9D4EDD, #7B2CBF)',
                    width: '80px',
                    height: '80px',
                    borderRadius: '50%',
                    boxShadow: '0 8px 32px rgba(157, 78, 221, 0.5)',
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    justifyContent: 'center',
                    gap: '4px',
                    transition: 'all 0.3s ease',
                    cursor: 'pointer'
                  }}
                  onMouseEnter={(e) => {
                    e.currentTarget.style.transform = 'scale(1.1)';
                    e.currentTarget.style.boxShadow = '0 12px 40px rgba(157, 78, 221, 0.6)';
                  }}
                  onMouseLeave={(e) => {
                    e.currentTarget.style.transform = 'scale(1)';
                    e.currentTarget.style.boxShadow = '0 8px 32px rgba(157, 78, 221, 0.5)';
                  }}
                  aria-label={`Primary action: ${getNextStageLabel(currentStatus)}`}
                >
                  <IconCheck size={28} stroke={2.5} />
                  <Text size="xs" fw={700} style={{ textAlign: 'center', lineHeight: 1.2 }}>
                    {getNextStageLabelShort(currentStatus)}
                  </Text>
                </Button>
              </Menu.Target>

              <Menu.Dropdown
                style={{
                  borderRadius: '12px',
                  padding: '8px',
                  boxShadow: '0 8px 32px rgba(0, 0, 0, 0.2)'
                }}
              >
                <Menu.Label style={{ fontSize: '12px', fontWeight: 600 }}>
                  WORKFLOW ACTIONS
                </Menu.Label>

                <Menu.Item
                  leftSection={<IconCheck size={18} />}
                  onClick={() => {
                    handleAdvanceStage();
                    setMenuOpened(false);
                  }}
                  disabled={!canAdvanceStage}
                  style={{
                    background: 'rgba(157, 78, 221, 0.1)',
                    borderRadius: '8px',
                    marginBottom: '4px',
                    fontWeight: 600
                  }}
                >
                  <Box>
                    <Text fw={600}>{getNextStageLabel(currentStatus)}</Text>
                    <Text size="xs" c="dimmed">
                      {getNextStageDescription(currentStatus)}
                    </Text>
                  </Box>
                </Menu.Item>

                <Menu.Divider />

                <Menu.Item
                  leftSection={<IconCheck size={18} />}
                  onClick={() => {
                    handleSkipToApproved();
                    setMenuOpened(false);
                  }}
                  disabled={!canSkipToApproved}
                  color="green"
                >
                  <Box>
                    <Text fw={600}>Skip to Final Approval</Text>
                    <Text size="xs" c="dimmed">
                      Bypass remaining stages
                    </Text>
                  </Box>
                </Menu.Item>
              </Menu.Dropdown>
            </Menu>
          )}
        </Transition>
      </Affix>
    </>
  );
};
```

### Short Label Helper (for FAB)

```typescript
const getNextStageLabelShort = (status: VettingStatus): string => {
  const shortLabels: Record<VettingStatus, string> = {
    UnderReview: 'APPROVE',
    InterviewApproved: 'SCHEDULE',
    InterviewScheduled: 'COMPLETE',
    FinalReview: 'APPROVE',
    Approved: 'DONE',
    Denied: 'DENIED',
    OnHold: 'RESUME'
  };

  return shortLabels[status] || 'NEXT';
};
```

### Accessibility Considerations

**FAB Accessibility**:
```typescript
<Button
  aria-label={`Primary action: ${getNextStageLabel(currentStatus)}. Click to expand workflow options.`}
  aria-haspopup="menu"
  aria-expanded={menuOpened}
  role="button"
>
  {/* FAB content */}
</Button>
```

**Keyboard Navigation**:
- Tab reaches FAB (last in tab order)
- Enter/Space opens menu
- Arrow keys navigate menu items
- Enter selects menu item
- Escape closes menu and returns focus to FAB

**Screen Reader Support**:
- FAB announces current primary action
- Menu items provide full descriptions
- Menu label describes section purpose
- Disabled items skip in navigation

**Touch Optimization**:
- FAB: 80px × 80px (well above 44px minimum)
- Bottom-right placement in thumb zone
- Menu items: 48px minimum height
- 20px offset from screen edges

**Scroll Behavior**:
```typescript
const [scroll] = useWindowScroll();

// Show FAB after scrolling down 100px
<Transition transition="slide-up" mounted={scroll.y > 100}>
  {/* FAB appears when user scrolls down */}
</Transition>
```

**Reduced Motion**:
```typescript
const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

<Transition
  transition={prefersReducedMotion ? 'fade' : 'slide-up'}
  mounted={scroll.y > 100}
>
  {/* FAB with appropriate animation */}
</Transition>
```

### Pros

✅ **Always Accessible**: Primary action stays visible during scrolling
✅ **Mobile-First Excellence**: FAB is a proven mobile pattern
✅ **Thumb-Zone Optimized**: Bottom-right placement perfect for one-handed use
✅ **Clean Interface**: Keeps action bar uncluttered
✅ **Modern Design Language**: Familiar from Material Design, Gmail, etc.
✅ **Contextual**: Appears when needed (after scroll)
✅ **Scalable Menu**: Can add more options to FAB menu easily

### Cons

❌ **Unfamiliar in Desktop Apps**: FABs are mobile-first, less common on desktop
❌ **Can Obscure Content**: Floating element may hide important information
❌ **Discovery Challenge**: Users might not realize FAB has a menu
❌ **Accessibility Complexity**: Requires careful implementation for screen readers
❌ **Two Action Areas**: Primary actions split between FAB and horizontal bar
❌ **Z-Index Management**: Must ensure FAB stays above other elements

### When This Pattern Works Best

**Ideal Scenarios**:
- Mobile-first applications with long-scrolling content
- Detail pages with extensive information requiring scrolling
- When primary action should be persistent throughout scrolling
- Modern, app-like interfaces (not traditional web forms)
- Single primary action with few secondary options

**Not Recommended When**:
- Desktop-only applications
- Short pages without scrolling
- Multiple primary actions of equal importance
- Conservative/traditional user base unfamiliar with mobile patterns
- Accessibility is critical concern (requires extra effort)

---

## Comparative Analysis

### Feature Comparison Matrix

| Feature | Option 1: Split Button | Option 2: Tiered Groups | Option 3: FAB |
|---------|------------------------|------------------------|---------------|
| **Primary Action Prominence** | High (electric gradient) | Highest (large full-width) | High (floating, persistent) |
| **All Actions Visible** | No (menu required) | Yes | Partial (FAB menu) |
| **Space Efficiency** | Excellent (horizontal) | Poor (vertical) | Excellent (floating) |
| **Mobile Usability** | Good | Excellent | Excellent |
| **Desktop Usability** | Excellent | Good | Fair |
| **Scalability** | Excellent (add to menu) | Poor (limited space) | Good (add to menu) |
| **Learning Curve** | Medium | Low | Medium-High |
| **Accessibility** | Good (needs testing) | Excellent | Medium (complex) |
| **Visual Hierarchy** | Medium | Excellent | Good |
| **Industry Familiarity** | High (email clients) | High (forms) | High (mobile apps) |

### Use Case Recommendations

**Recommend Option 1 (Split Button) if**:
- Desktop-first workflow
- Power users processing many applications
- Space is at premium
- Familiar with modern productivity tools

**Recommend Option 2 (Tiered Groups) if**:
- Mobile-first design required
- New/infrequent users
- Maximum clarity and discoverability needed
- Accessibility is top priority
- Page has sufficient vertical space

**Recommend Option 3 (FAB) if**:
- Long-scrolling detail pages
- Mobile app-like experience desired
- Single dominant primary action
- Modern, progressive design language
- Primary action should persist during scrolling

---

## Design System Compliance

### Color Palette (Design System v7)

All options use approved colors:
- **Primary Action**: Electric purple gradient (#9D4EDD → #7B2CBF)
- **Success/Approve**: Success green (#228B22)
- **Warning/Hold**: Warning yellow (#DAA520)
- **Error/Deny**: Error red (#DC143C)
- **Secondary Actions**: Burgundy outline (#880124)
- **Backgrounds**: Cream (#FAF6F2), Ivory (#FFF8F0)

### Typography

All options follow Design System v7:
- **Button Text**: Montserrat, 600 weight, uppercase, 1px letter-spacing
- **Labels**: Montserrat, 600 weight, uppercase
- **Descriptions**: Source Sans 3, 400 weight

### Signature Animations

All buttons use corner morphing animation:
```css
border-radius: 12px 6px 12px 6px;
transition: all 0.3s ease;

&:hover {
  border-radius: 6px 12px 6px 12px;
}
```

NO vertical movement (translateY) on buttons as per design system rules.

### Spacing

Using design system spacing scale:
- Button gaps: `var(--space-md)` (24px)
- Section gaps: `var(--space-lg)` (32px)
- Padding: `var(--space-sm)` (16px) to `var(--space-md)` (24px)

---

## Mobile Responsive Considerations

### Breakpoint: 768px

**Option 1 (Split Button)**:
```typescript
// Mobile: Stack split button and secondary actions vertically
<Stack gap="md">
  <Menu.Target>
    <Button fullWidth size="lg">
      {getNextStageLabel(currentStatus)}
    </Button>
  </Menu.Target>

  <Group grow>
    <Button variant="outline">On Hold</Button>
    <Button variant="outline">Reminder</Button>
  </Group>
</Stack>
```

**Option 2 (Tiered Groups)**:
```typescript
// Mobile: Already stacked vertically, just adjust widths
<Stack gap="lg">
  <Button fullWidth size="xl">{/* Primary */}</Button>
  <Stack gap="md">
    <Button fullWidth size="md">{/* Secondary 1 */}</Button>
    <Button fullWidth size="md">{/* Secondary 2 */}</Button>
  </Stack>
  {/* Tertiary actions remain */}
</Stack>
```

**Option 3 (FAB)**:
```typescript
// Mobile: No changes needed, FAB is already optimized
// Horizontal action bar stacks on very small screens (< 480px)
<Stack gap="sm">
  <Button fullWidth variant="outline">On Hold</Button>
  <Button fullWidth variant="outline">Reminder</Button>
  <Button fullWidth variant="outline">Deny</Button>
</Stack>
```

---

## Implementation Recommendations

### Phased Rollout Strategy

**Phase 1: Implement Option 2 (Tiered Groups)** ✅ RECOMMENDED
- **Reason**: Lowest risk, highest clarity for new feature
- **Timeline**: 1-2 days development
- **Benefits**: All actions visible, clear hierarchy, excellent accessibility
- **Drawbacks**: Takes more space, but acceptable for detail page

**Phase 2: A/B Test Option 1 (Split Button)**
- **Reason**: Test if power users prefer consolidated interface
- **Timeline**: 2 weeks after Phase 1 deployment
- **Metrics**: Time to complete vetting workflow, error rate, user satisfaction
- **Decision Point**: If 80%+ of users use primary action only, consider split button

**Phase 3: Consider Option 3 (FAB) for Mobile App**
- **Reason**: If mobile usage > 50%, FAB pattern provides best UX
- **Timeline**: 1-2 months after Phase 1
- **Implementation**: Mobile-only variant with desktop fallback to Option 1 or 2

### Development Priorities

**Must Have (MVP)**:
1. Dynamic label based on vetting status
2. Proper button hierarchy (primary vs secondary)
3. Email template modals for secondary actions
4. Disabled states when actions not available
5. Basic accessibility (keyboard nav, screen readers)

**Should Have (Enhancement)**:
1. Status-based descriptions
2. Success/error notifications
3. Loading states during API calls
4. Undo capability for accidental actions
5. Keyboard shortcuts for power users

**Could Have (Future)**:
1. Analytics tracking for action usage
2. Personalized action suggestions based on admin history
3. Batch actions for multiple applications
4. Custom email template library per admin

---

## Quality Validation Checklist

### UX Validation
- [ ] Primary action is most prominent visually
- [ ] All actions are discoverable within 3 seconds
- [ ] Clear visual hierarchy (primary > secondary > tertiary)
- [ ] Button labels match user mental model
- [ ] Disabled states are clear and explained
- [ ] Success/error feedback is immediate
- [ ] Mobile touch targets ≥ 44px

### Accessibility Validation
- [ ] WCAG 2.1 AA color contrast (4.5:1 minimum)
- [ ] Keyboard navigation works completely
- [ ] Screen reader announces all actions correctly
- [ ] Focus indicators visible (2px burgundy outline)
- [ ] Disabled buttons skip in tab order
- [ ] ARIA labels provide context
- [ ] Reduced motion support implemented

### Design System Compliance
- [ ] Colors match Design System v7 palette exactly
- [ ] Typography uses approved font families and weights
- [ ] Signature corner morphing animation on all buttons
- [ ] NO vertical translateY movement
- [ ] Spacing uses CSS variable scale
- [ ] Responsive breakpoint at 768px
- [ ] Mobile-first approach maintained

### Technical Validation
- [ ] Mantine v7 components used correctly
- [ ] TypeScript types for all props and state
- [ ] API integration with proper error handling
- [ ] Loading states prevent double-clicks
- [ ] Email template modals function correctly
- [ ] Status-based logic tested for all 7 states
- [ ] No console errors or warnings

---

## Next Steps

### Stakeholder Review Required

**Decision Points**:
1. **Primary Pattern Selection**: Which option (1, 2, or 3) best fits WitchCityRope admin workflow?
2. **Mobile Priority**: Is this feature primarily desktop or mobile-used?
3. **User Experience**: Are admins power users or occasional users?
4. **Phased Rollout**: Approve phased implementation strategy?

### Post-Approval Actions

**If Option 1 Approved (Split Button)**:
- Create Mantine Menu component with workflow actions
- Implement dynamic label logic
- Test dropdown interactions thoroughly
- Develop keyboard shortcut overlay (optional enhancement)

**If Option 2 Approved (Tiered Groups)**:
- Implement three-tier Stack layout
- Create responsive mobile variant
- Test with screen readers extensively
- Optimize vertical spacing on smaller screens

**If Option 3 Approved (FAB)**:
- Implement Affix component with scroll detection
- Create FAB menu with workflow options
- Test z-index conflicts with modals
- Develop desktop fallback pattern

### Documentation Handoff

After approval, create:
1. **Implementation Guide**: Step-by-step Mantine component setup
2. **Test Plan**: Comprehensive E2E and accessibility tests
3. **UI Handoff Document**: Detailed specifications for react-developer agent
4. **User Documentation**: Admin guide for new action buttons

---

## Appendix: Current Implementation Analysis

### Existing Code Review

**File**: `/apps/web/src/features/admin/vetting/components/VettingApplicationDetail.tsx`

**Current Action Buttons** (Lines 271-377):
```typescript
<Group gap="md">
  <Button onClick={handleApproveApplication}>Approve Application</Button>
  <Button onClick={handlePutOnHold}>Put On Hold</Button>
  <Button onClick={handleDenyApplication}>Deny Application</Button>
  <Button onClick={handleSendReminder}>Send Reminder</Button>
  {availableActions.canSchedule && (
    <Button onClick={() => setScheduleInterviewModalOpen(true)}>
      Schedule Interview
    </Button>
  )}
</Group>
```

**Issues**:
1. ❌ "Approve Application" is actually "Skip to Approved" (not next stage)
2. ❌ Missing "Advance to Next Stage" button entirely
3. ❌ All buttons have equal visual weight
4. ❌ No distinction between immediate and modal actions
5. ❌ Button labels don't change based on status
6. ❌ No visual hierarchy or grouping

**Fix Required**:
Replace entire action section with one of the three proposed options.

### Migration Path

**Low Risk Migration** (Option 2):
1. Add new tiered button structure below existing buttons
2. Test with stakeholders
3. Remove old button group after approval
4. Update E2E tests

**Medium Risk Migration** (Option 1 or 3):
1. Implement new pattern in feature branch
2. A/B test with 50% of admin users
3. Collect feedback for 2 weeks
4. Make permanent based on data

---

**Document Status**: Ready for stakeholder review
**Author**: UI Designer Agent
**Date**: 2025-10-06
**Version**: 1.0
**Review Required**: Product Owner, UX Lead, Engineering Lead
