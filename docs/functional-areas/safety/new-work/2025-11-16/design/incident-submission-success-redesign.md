# Incident Report Submission Success Page - Design Specification
<!-- Last Updated: 2025-11-16 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Design Phase -->

## Overview

This document provides the complete design specification for redesigning the incident report submission success page. The current implementation is a tracking/status page hybrid; this redesign focuses on creating a clear, reassuring confirmation experience that celebrates the user's contribution to community safety.

## Current Implementation Analysis

**Current File**: `/home/chad/repos/witchcityrope/apps/web/src/features/safety/components/SubmissionConfirmation.tsx`

**Problems Identified**:
1. ❌ Reference number lookup section creates confusion (is this a new page or tracking page?)
2. ❌ Crisis support link feels out of place on success confirmation
3. ❌ "Track Report Status" button suggests this is a status tracking page
4. ❌ Too many alerts and information boxes overwhelm the success message
5. ❌ Green success color conflicts with burgundy brand palette
6. ❌ Layout feels like dashboard/tracking page rather than confirmation page

## Design Goals

1. **Clear Success Confirmation**: Immediately reassure user their report was submitted
2. **Community-Focused Messaging**: Emphasize how they've helped keep the community safe
3. **Simplified Information**: Only essential details (reference number, next steps)
4. **Brand-Aligned Design**: Use WitchCityRope burgundy/plum color palette
5. **Mobile-First**: Excellent experience on all devices

## User Flow Context

```
Report Form → [Submit] → **Success Page** → [Return to Dashboard/Safety Page]
```

**User Mental State**:
- Just completed potentially difficult task of reporting incident
- May be emotionally sensitive
- Needs reassurance and clarity
- Wants to know what happens next

## Page Structure

### Layout Hierarchy

```
┌─────────────────────────────────────────┐
│  [Page Title: Incident Report Submitted] │
│  [Subtitle: Thank you message]           │
├─────────────────────────────────────────┤
│                                          │
│  ┌────────────────────────────────────┐ │
│  │  [Success Icon - Burgundy]         │ │
│  │                                     │ │
│  │  [Confirmation Message]             │ │
│  │                                     │ │
│  │  ┌──────────────────────────────┐  │ │
│  │  │  Reference Number Box        │  │ │
│  │  │  [REF-NUMBER] [Copy Button]  │  │ │
│  │  │  Submitted: [DATE/TIME]      │  │ │
│  │  └──────────────────────────────┘  │ │
│  │                                     │ │
│  │  [What Happens Next - 2-3 lines]   │ │
│  │                                     │ │
│  │  ┌──────────────────────────────┐  │ │
│  │  │  [Return to Safety Page] BTN │  │ │
│  │  └──────────────────────────────┘  │ │
│  └────────────────────────────────────┘ │
│                                          │
└─────────────────────────────────────────┘
```

## Component Specifications

### 1. Page Title Section

**Desktop Layout**:
```tsx
<Box ta="center" mb="xl">
  <Title
    order={1}
    style={{
      fontFamily: 'var(--font-heading)',
      fontSize: '48px',
      fontWeight: 800,
      textTransform: 'uppercase',
      letterSpacing: '3px',
      color: 'var(--color-burgundy)',
      marginBottom: 'var(--space-sm)'
    }}
  >
    Incident Report Submitted
  </Title>

  <Text
    size="lg"
    style={{
      fontFamily: 'var(--font-body)',
      fontSize: '18px',
      color: 'var(--color-smoke)',
      fontWeight: 400
    }}
  >
    Thank you for helping keep our community safe
  </Text>
</Box>
```

**Mobile Layout** (<768px):
- Title: 36px font size
- Subtitle: 16px font size
- Padding: 20px horizontal

**Rationale**:
- Burgundy title immediately signals success within brand palette (not generic green)
- Uppercase heading with letter-spacing creates strong, confident tone
- Subtitle provides warm, human acknowledgment

---

### 2. Success Confirmation Card

**Main Container**:
```tsx
<Paper
  shadow="sm"
  p="xl"
  radius="md"
  style={{
    maxWidth: '600px',
    margin: '0 auto',
    borderTop: '4px solid var(--color-burgundy)', // Brand accent
    backgroundColor: '#FFFFFF'
  }}
>
  <Stack gap="lg" align="center">
    {/* Card content */}
  </Stack>
</Paper>
```

**Mobile**:
- Full width with 20px margin
- Padding: 24px (var(--space-md))

---

### 3. Success Icon

**Implementation**:
```tsx
<Box
  style={{
    width: '80px',
    height: '80px',
    borderRadius: '50%',
    background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 'var(--space-md)'
  }}
>
  <IconCheck size={40} color="var(--color-ivory)" stroke={3} />
</Box>
```

**Visual Effect**:
- Burgundy-to-plum gradient background (brand colors)
- White checkmark icon (high contrast)
- Circular shape creates focal point
- Large enough to be reassuring but not overwhelming

**Rationale**:
- Brand-aligned colors (not generic green success)
- Professional, calm visual confirmation
- Accessible contrast (AAA compliant)

---

### 4. Confirmation Message

**Copy**:
```tsx
<Text
  size="lg"
  ta="center"
  style={{
    fontFamily: 'var(--font-body)',
    fontSize: '18px',
    fontWeight: 400,
    color: 'var(--color-charcoal)',
    lineHeight: 1.7,
    maxWidth: '500px'
  }}
>
  Your safety incident report has been received. The safety team has been
  notified and will review your report promptly.
</Text>
```

**Tone**:
- Professional but warm
- Reassuring without being overly formal
- Focuses on action taken (team notified)

**Length**: 2 sentences max to avoid overwhelming user

---

### 5. Reference Number Display

**Implementation**:
```tsx
<Alert
  variant="light"
  color="grape" // Maps to plum in Mantine
  style={{
    width: '100%',
    border: '1px solid var(--color-plum)',
    backgroundColor: 'rgba(97, 75, 121, 0.05)' // Light plum tint
  }}
>
  <Stack gap="xs">
    <Group justify="space-between" align="center" wrap="nowrap">
      <Box style={{ flex: 1 }}>
        <Text
          size="sm"
          fw={600}
          style={{
            fontFamily: 'var(--font-heading)',
            textTransform: 'uppercase',
            letterSpacing: '0.5px',
            color: 'var(--color-smoke)',
            marginBottom: '4px'
          }}
        >
          Reference Number
        </Text>
        <Code
          style={{
            fontSize: '16px',
            fontWeight: 700,
            color: 'var(--color-burgundy)',
            backgroundColor: 'transparent',
            border: '1px solid var(--color-taupe)',
            padding: '4px 8px'
          }}
        >
          {submissionResult.referenceNumber}
        </Code>
      </Box>

      <Tooltip
        label={clipboard.copied ? 'Copied!' : 'Copy reference number'}
        position="left"
      >
        <ActionIcon
          variant="light"
          color="grape"
          size="lg"
          onClick={() => clipboard.copy(submissionResult.referenceNumber)}
          aria-label="Copy reference number to clipboard"
          style={{
            backgroundColor: clipboard.copied
              ? 'var(--color-plum)'
              : 'rgba(97, 75, 121, 0.1)'
          }}
        >
          <IconCopy size={18} color={clipboard.copied ? '#FFF' : 'var(--color-plum)'} />
        </ActionIcon>
      </Tooltip>
    </Group>

    <Text
      size="xs"
      style={{
        color: 'var(--color-smoke)',
        fontFamily: 'var(--font-body)'
      }}
    >
      Submitted on {formatSubmissionTime(submissionResult.submittedAt)}
    </Text>
  </Stack>
</Alert>
```

**Visual Design**:
- Light plum background (subtle, on-brand)
- Burgundy reference number (stands out)
- Copy button with visual feedback (color change on copy)
- Timestamp in secondary text color

**Responsive**:
- Group wraps on very small screens (<400px)
- Copy button remains accessible (44px touch target)

**Rationale**:
- Reference number is important but not overwhelming
- One-click copy for user convenience
- Plum color ties to safety/serious content
- Timestamp provides context without cluttering

---

### 6. What Happens Next Section

**Copy Options** (Choose one based on stakeholder preference):

**Option A - Minimal** (Recommended):
```tsx
<Text
  size="md"
  ta="center"
  style={{
    fontFamily: 'var(--font-body)',
    fontSize: '16px',
    color: 'var(--color-smoke)',
    lineHeight: 1.7,
    maxWidth: '500px'
  }}
>
  The safety team will review your report and may contact you for additional
  information if needed. You can reference your report number if you need to
  follow up.
</Text>
```

**Option B - Detailed**:
```tsx
<Stack gap="sm" style={{ width: '100%', maxWidth: '500px' }}>
  <Text
    size="md"
    ta="center"
    style={{
      fontFamily: 'var(--font-body)',
      fontSize: '16px',
      color: 'var(--color-charcoal)',
      lineHeight: 1.7,
      fontWeight: 600
    }}
  >
    What happens next?
  </Text>

  <Text
    size="sm"
    ta="center"
    style={{
      fontFamily: 'var(--font-body)',
      fontSize: '14px',
      color: 'var(--color-smoke)',
      lineHeight: 1.7
    }}
  >
    The safety team will review your report and may contact you for additional
    information if needed. Save your reference number if you need to follow up.
  </Text>
</Stack>
```

**Recommendation**: Use Option A (minimal) to keep page focused and reassuring.

**Rationale**:
- Sets expectations without creating anxiety
- Mentions potential follow-up without pressure
- Reminds user of reference number utility
- 2-3 sentences maximum (per requirements)

---

### 7. Action Buttons

**Implementation**:
```tsx
<Group justify="center" gap="md" mt="xl">
  <Button
    className="btn btn-primary"
    onClick={() => navigate('/safety')}
    leftSection={<IconShieldCheck size={18} />}
  >
    Return to Safety Center
  </Button>
</Group>
```

**Mobile** (<768px):
```tsx
<Stack gap="sm" mt="xl" style={{ width: '100%' }}>
  <Button
    className="btn btn-primary"
    fullWidth
    onClick={() => navigate('/safety')}
    leftSection={<IconShieldCheck size={18} />}
  >
    Return to Safety Center
  </Button>
</Stack>
```

**Button Choices**:
- **Primary CTA**: "Return to Safety Center" (gold gradient)
- **NO secondary button** for "Submit Another Report" (reduces clutter, user can navigate if needed)

**Rationale**:
- Single action reduces decision fatigue
- Primary button uses brand's signature gold gradient
- Icon reinforces safety context
- Full-width on mobile (thumb-friendly)
- Allows user to continue their workflow

**Alternative** (if stakeholder wants two buttons):
```tsx
<Group justify="center" gap="md" mt="xl">
  <Button
    className="btn btn-secondary"
    onClick={() => navigate('/dashboard')}
  >
    Return to Dashboard
  </Button>

  <Button
    className="btn btn-primary"
    onClick={() => navigate('/safety')}
    leftSection={<IconShieldCheck size={18} />}
  >
    Safety Center
  </Button>
</Group>
```

---

## Complete Component Code

```tsx
// Incident Report Submission Success Component
// Shows confirmation after successful incident submission

import React from 'react';
import {
  Box,
  Paper,
  Title,
  Text,
  Button,
  Alert,
  Group,
  Stack,
  Code,
  ActionIcon,
  Tooltip
} from '@mantine/core';
import { IconCheck, IconCopy, IconShieldCheck } from '@tabler/icons-react';
import { useClipboard } from '@mantine/hooks';
import { useNavigate } from 'react-router-dom';

interface SubmissionConfirmationProps {
  submissionResult: {
    referenceNumber: string;
    submittedAt: string;
  };
}

export function SubmissionConfirmation({ submissionResult }: SubmissionConfirmationProps) {
  const clipboard = useClipboard({ timeout: 2000 });
  const navigate = useNavigate();

  const formatSubmissionTime = (isoString: string) => {
    const date = new Date(isoString);
    return date.toLocaleString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  };

  return (
    <Box maw={800} mx="auto" p="md">
      {/* Page Title Section */}
      <Box ta="center" mb="xl">
        <Title
          order={1}
          style={{
            fontFamily: 'var(--font-heading)',
            fontSize: '48px',
            fontWeight: 800,
            textTransform: 'uppercase',
            letterSpacing: '3px',
            color: 'var(--color-burgundy)',
            marginBottom: 'var(--space-sm)'
          }}
          sx={(theme) => ({
            [theme.fn.smallerThan('sm')]: {
              fontSize: '36px'
            }
          })}
        >
          Incident Report Submitted
        </Title>

        <Text
          size="lg"
          style={{
            fontFamily: 'var(--font-body)',
            fontSize: '18px',
            color: 'var(--color-smoke)',
            fontWeight: 400
          }}
          sx={(theme) => ({
            [theme.fn.smallerThan('sm')]: {
              fontSize: '16px'
            }
          })}
        >
          Thank you for helping keep our community safe
        </Text>
      </Box>

      {/* Main Confirmation Card */}
      <Paper
        shadow="sm"
        p="xl"
        radius="md"
        style={{
          borderTop: '4px solid var(--color-burgundy)',
          backgroundColor: '#FFFFFF'
        }}
      >
        <Stack gap="lg" align="center">
          {/* Success Icon */}
          <Box
            style={{
              width: '80px',
              height: '80px',
              borderRadius: '50%',
              background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              marginBottom: 'var(--space-md)'
            }}
          >
            <IconCheck size={40} color="var(--color-ivory)" stroke={3} />
          </Box>

          {/* Confirmation Message */}
          <Text
            size="lg"
            ta="center"
            style={{
              fontFamily: 'var(--font-body)',
              fontSize: '18px',
              fontWeight: 400,
              color: 'var(--color-charcoal)',
              lineHeight: 1.7,
              maxWidth: '500px'
            }}
          >
            Your safety incident report has been received. The safety team has been
            notified and will review your report promptly.
          </Text>

          {/* Reference Number Display */}
          <Alert
            variant="light"
            color="grape"
            style={{
              width: '100%',
              border: '1px solid var(--color-plum)',
              backgroundColor: 'rgba(97, 75, 121, 0.05)'
            }}
          >
            <Stack gap="xs">
              <Group justify="space-between" align="center" wrap="nowrap">
                <Box style={{ flex: 1 }}>
                  <Text
                    size="sm"
                    fw={600}
                    style={{
                      fontFamily: 'var(--font-heading)',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                      color: 'var(--color-smoke)',
                      marginBottom: '4px'
                    }}
                  >
                    Reference Number
                  </Text>
                  <Code
                    style={{
                      fontSize: '16px',
                      fontWeight: 700,
                      color: 'var(--color-burgundy)',
                      backgroundColor: 'transparent',
                      border: '1px solid var(--color-taupe)',
                      padding: '4px 8px'
                    }}
                  >
                    {submissionResult.referenceNumber}
                  </Code>
                </Box>

                <Tooltip
                  label={clipboard.copied ? 'Copied!' : 'Copy reference number'}
                  position="left"
                >
                  <ActionIcon
                    variant="light"
                    color="grape"
                    size="lg"
                    onClick={() => clipboard.copy(submissionResult.referenceNumber)}
                    aria-label="Copy reference number to clipboard"
                    style={{
                      backgroundColor: clipboard.copied
                        ? 'var(--color-plum)'
                        : 'rgba(97, 75, 121, 0.1)',
                      minWidth: '44px',
                      minHeight: '44px'
                    }}
                  >
                    <IconCopy
                      size={18}
                      color={clipboard.copied ? '#FFF' : 'var(--color-plum)'}
                    />
                  </ActionIcon>
                </Tooltip>
              </Group>

              <Text
                size="xs"
                style={{
                  color: 'var(--color-smoke)',
                  fontFamily: 'var(--font-body)'
                }}
              >
                Submitted on {formatSubmissionTime(submissionResult.submittedAt)}
              </Text>
            </Stack>
          </Alert>

          {/* What Happens Next */}
          <Text
            size="md"
            ta="center"
            style={{
              fontFamily: 'var(--font-body)',
              fontSize: '16px',
              color: 'var(--color-smoke)',
              lineHeight: 1.7,
              maxWidth: '500px'
            }}
          >
            The safety team will review your report and may contact you for additional
            information if needed. You can reference your report number if you need to
            follow up.
          </Text>

          {/* Action Buttons */}
          <Group
            justify="center"
            gap="md"
            mt="xl"
            sx={(theme) => ({
              [theme.fn.smallerThan('sm')]: {
                width: '100%',
                flexDirection: 'column'
              }
            })}
          >
            <Button
              className="btn btn-primary"
              onClick={() => navigate('/safety')}
              leftSection={<IconShieldCheck size={18} />}
              sx={(theme) => ({
                [theme.fn.smallerThan('sm')]: {
                  width: '100%'
                }
              })}
            >
              Return to Safety Center
            </Button>
          </Group>
        </Stack>
      </Paper>
    </Box>
  );
}
```

---

## Responsive Design Specifications

### Desktop (≥769px)
- **Container**: Max-width 800px, centered
- **Card**: Max-width 600px within container
- **Padding**: 40px around content
- **Title**: 48px font size
- **Button**: Standard size (14px/32px padding)
- **Layout**: Single column, centered

### Tablet (768px)
- **Container**: Full width with 20px margins
- **Padding**: 24px around content
- **Title**: 42px font size
- **Button**: Slightly larger touch targets

### Mobile (<768px)
- **Container**: Full width with 16px margins
- **Padding**: 20px around content
- **Title**: 36px font size
- **Subtitle**: 16px font size
- **Button**: Full-width, stacked vertically
- **Icon**: 70px (slightly smaller)
- **Touch Targets**: Minimum 44px for copy button

---

## Color Palette Reference

**Primary Colors Used**:
```css
--color-burgundy: #880124     /* Title, reference number, accent border */
--color-plum: #614B79         /* Success icon gradient, alert background */
--color-ivory: #FFF8F0        /* Icon checkmark color */
--color-charcoal: #2B2B2B     /* Primary body text */
--color-smoke: #4A4A4A        /* Secondary text, labels */
--color-taupe: #B8B0A8        /* Borders, subtle accents */
```

**Contrast Ratios**:
- Burgundy title on white: 10.2:1 (AAA)
- Charcoal text on white: 12.6:1 (AAA)
- Smoke text on white: 8.9:1 (AAA)
- Ivory on burgundy/plum gradient: 8.1:1 (AAA)

---

## Accessibility Compliance

### WCAG 2.1 AA Standards Met

**Color Contrast**:
- All text meets minimum 4.5:1 ratio (most exceed 7:1 AAA)
- Icon contrast: 8.1:1 (AAA compliant)
- Interactive elements clearly distinguishable

**Keyboard Navigation**:
- Copy button: Tab-accessible
- Focus ring: 2px burgundy outline with 2px offset
- Button: Enter/Space to activate
- Logical tab order: Title → Copy button → Return button

**Screen Reader Support**:
```tsx
// ARIA labels
<ActionIcon aria-label="Copy reference number to clipboard">
<Button leftSection={<IconShieldCheck size={18} />}>
  Return to Safety Center  // Screen reader reads button text + role
</Button>
```

**Focus Management**:
- Auto-focus on copy button when page loads (optional)
- Clear focus indicators on all interactive elements
- No keyboard traps

**Reduced Motion**:
```css
@media (prefers-reduced-motion: reduce) {
  * {
    animation: none !important;
    transition: none !important;
  }
}
```

---

## Component Dependencies

**Mantine Components**:
- `Box` - Layout container
- `Paper` - Card container with shadow
- `Title` - Page title typography
- `Text` - Body text and labels
- `Button` - Primary action button
- `Alert` - Reference number display
- `Group` - Horizontal layout
- `Stack` - Vertical layout
- `Code` - Reference number display
- `ActionIcon` - Copy button
- `Tooltip` - Copy feedback

**Mantine Hooks**:
- `useClipboard` - Reference number copy functionality

**Tabler Icons**:
- `IconCheck` - Success checkmark
- `IconCopy` - Copy button icon
- `IconShieldCheck` - Safety center button icon

**React Router**:
- `useNavigate` - Navigation to safety center

---

## Implementation Notes

### CSS Variables Required

Ensure these CSS variables are defined in `/apps/web/src/index.css`:

```css
:root {
  /* Typography */
  --font-heading: 'Montserrat', sans-serif;
  --font-body: 'Source Sans 3', sans-serif;

  /* Colors */
  --color-burgundy: #880124;
  --color-plum: #614B79;
  --color-ivory: #FFF8F0;
  --color-charcoal: #2B2B2B;
  --color-smoke: #4A4A4A;
  --color-taupe: #B8B0A8;

  /* Spacing */
  --space-xs: 8px;
  --space-sm: 16px;
  --space-md: 24px;
  --space-lg: 32px;
  --space-xl: 40px;
}
```

### Button Classes Required

Use existing button classes from Design System v7:

```css
.btn {
  padding: 14px 32px;
  border-radius: 12px 6px 12px 6px;
  font-family: var(--font-heading);
  font-weight: 600;
  font-size: 14px;
  text-transform: uppercase;
  letter-spacing: 1.5px;
  transition: all 0.3s ease;
  cursor: pointer;
  border: none;
}

.btn-primary {
  background: linear-gradient(135deg, #FFBF00 0%, #FF8C00 100%);
  color: #1A1A2E;
  box-shadow: 0 4px 15px rgba(255, 191, 0, 0.4);
}

.btn-primary:hover {
  border-radius: 6px 12px 6px 12px; /* Corner morphing */
  box-shadow: 0 6px 20px rgba(255, 191, 0, 0.6);
}
```

---

## Content Guidelines

### Copy Tone
- **Professional but warm**: Not corporate, not too casual
- **Reassuring**: User did the right thing
- **Clear**: No ambiguity about what happens next
- **Concise**: Respect user's emotional state

### What to Include
✅ Clear confirmation of submission
✅ Reference number with copy functionality
✅ What happens next (2-3 sentences)
✅ Single clear action (return to safety center)

### What to Remove
❌ Tracking functionality (belongs on separate page)
❌ Crisis support links (belongs on safety center, not success page)
❌ Multiple alerts (too overwhelming)
❌ "Submit another report" option (reduces clutter)
❌ Additional resources section (too much info)

---

## Testing Checklist

### Visual Testing
- [ ] Title displays at correct size (48px desktop, 36px mobile)
- [ ] Success icon shows burgundy-to-plum gradient
- [ ] Checkmark icon is white and clearly visible
- [ ] Reference number is burgundy and stands out
- [ ] Card has 4px burgundy top border
- [ ] Copy button changes color on click
- [ ] Button uses gold gradient (primary CTA)

### Functional Testing
- [ ] Copy button copies reference number to clipboard
- [ ] Tooltip shows "Copied!" feedback for 2 seconds
- [ ] Return button navigates to `/safety`
- [ ] Page displays correctly on mobile (<768px)
- [ ] Page displays correctly on tablet (768px)
- [ ] Page displays correctly on desktop (≥769px)

### Accessibility Testing
- [ ] Tab navigation works (copy button → return button)
- [ ] Focus indicators visible on all interactive elements
- [ ] Screen reader announces "Copy reference number to clipboard"
- [ ] Screen reader announces "Return to Safety Center"
- [ ] Color contrast meets WCAG AA standards
- [ ] Page works with keyboard only (no mouse)
- [ ] Reduced motion preference respected

### Browser Testing
- [ ] Chrome/Edge 90+
- [ ] Firefox 88+
- [ ] Safari 14+
- [ ] Mobile Safari iOS 14+
- [ ] Chrome Android

---

## Future Enhancements (Out of Scope for MVP)

1. **Email Confirmation**: Send copy of reference number to user's email
2. **Save to Calendar**: Option to save follow-up reminder
3. **Print Receipt**: Printable version of confirmation
4. **Animation**: Subtle success animation on page load
5. **Social Proof**: "X reports submitted this month" (privacy-conscious)
6. **Resources Link**: Optional link to support resources (low-priority)

---

## Comparison: Before vs. After

### Before (Current)
- ❌ Green success color (off-brand)
- ❌ Reference number lookup section (confusing)
- ❌ Track report status button (wrong page)
- ❌ Crisis support section (out of place)
- ❌ Multiple alerts (overwhelming)
- ❌ Tracking page layout (wrong context)

### After (Redesign)
- ✅ Burgundy/plum brand colors
- ✅ Single confirmation card
- ✅ Clear success message
- ✅ Reference number with copy
- ✅ Simple "what happens next"
- ✅ Single clear action (return)
- ✅ Mobile-optimized layout
- ✅ Reassuring, professional tone

---

## Implementation Priority

### Phase 1 (MVP - Required)
1. Page title and subtitle
2. Success icon with brand gradient
3. Confirmation message
4. Reference number display with copy button
5. "What happens next" text
6. Return to Safety Center button
7. Mobile responsive layout

### Phase 2 (Nice to Have)
1. Success animation on page load
2. Auto-focus on copy button
3. Keyboard shortcuts (Ctrl+C to copy)

### Phase 3 (Future)
1. Email confirmation integration
2. Print receipt option

---

## Design System Compliance

This design follows:
- ✅ Design System v7 color palette
- ✅ Design System v7 typography scale
- ✅ Design System v7 spacing system
- ✅ Button Style Guide (Primary CTA button)
- ✅ Mobile-first responsive patterns
- ✅ WCAG 2.1 AA accessibility standards
- ✅ Brand voice and tone guidelines
- ✅ Mantine v7 UI Framework standards

---

## Files to Modify

1. **Component**: `/home/chad/repos/witchcityrope/apps/web/src/features/safety/components/SubmissionConfirmation.tsx`
   - Complete redesign per this spec

2. **Route** (if needed): `/home/chad/repos/witchcityrope/apps/web/src/pages/safety/SafetyStatusPage.tsx`
   - Ensure this is truly a separate tracking page
   - Remove any success confirmation logic from tracking page

3. **CSS** (verify variables): `/home/chad/repos/witchcityrope/apps/web/src/index.css`
   - Confirm all CSS variables exist
   - Confirm button classes exist

---

## Sign-Off

**Design Phase Complete**: Ready for implementation by React Developer

**Next Steps**:
1. Review this spec with stakeholders
2. Confirm copy/messaging
3. Create handoff document for React Developer
4. Implement component per spec
5. Test on all devices
6. QA accessibility compliance

---

## Questions for Stakeholders

1. **Copy Preference**: Option A (minimal) or Option B (detailed) for "What happens next"?
2. **Button Options**: Single button (Return to Safety Center) or two buttons (Dashboard + Safety Center)?
3. **Success Animation**: Do we want subtle animation on page load (e.g., fade-in)?
4. **Email Confirmation**: Future feature or out of scope?

**Recommended Answers**:
1. Option A (minimal) - keeps page focused
2. Single button - reduces decision fatigue
3. No animation for MVP - add later if desired
4. Out of scope for MVP - future enhancement

---

**Document Status**: Complete and ready for stakeholder review
**Created**: 2025-11-16
**Author**: UI Designer Agent
**Reviewer**: [Pending]
**Approved**: [Pending]
