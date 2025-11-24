# UI Design: Venue Location Privacy
<!-- Last Updated: 2025-11-23 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Draft -->

## Design Overview

This UI design adds location privacy controls to venue management, allowing admins to configure a public "Location" field (city, state) that displays to non-vetted users before they RSVP or purchase tickets. This protects venue privacy while providing general location information for event discovery.

## User Personas

### Admin
- Needs to add Location field to venue management form
- Wants simple text input for city/state information
- Requires clear guidance on field purpose

### Non-Vetted Member
- Sees general location (city, state) on event cards and event details
- Cannot see full venue address until after RSVP/ticket purchase
- Needs clear indication of when full details will be available

### Vetted Member
- Always sees full venue name and directions
- Location field is not displayed (VenueName takes priority)
- Has trusted status that bypasses privacy restrictions

### Event Participant (Post-RSVP)
- Sees full venue name and directions after registering/purchasing
- Location field not displayed once access granted
- Has same access as vetted members for specific event

## Wireframes

### 1. Admin Venue Management Form - Location Field Addition

**Desktop View (≥769px)**

```
┌─────────────────────────────────────────────────────────────────┐
│  VENUE MANAGEMENT                                      [CARD]    │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  VENUE NAME *                                                    │
│  [Salem Community Center                                    ]    │
│                                                                  │
│  LOCATION (CITY, STATE)                                          │
│  [Salem, MA                                             ]        │
│  Public location (city and state) shown to non-vetted users     │
│  before RSVP or ticket purchase. Full venue details shown       │
│  after registration.                                             │
│                                                                  │
│  DIRECTIONS                                                      │
│  ┌────────────────────────────────────────────────────┐         │
│  │ 123 Main Street, Salem, MA 01970                   │         │
│  │ Enter through side door.                           │         │
│  │ Free parking in rear lot.                          │         │
│  │                                                     │         │
│  └────────────────────────────────────────────────────┘         │
│  500/500 characters                                              │
│                                                                  │
│  NOTES                                                           │
│  ┌────────────────────────────────────────────────────┐         │
│  │ Accessible venue, elevator available               │         │
│  │                                                     │         │
│  │                                                     │         │
│  └────────────────────────────────────────────────────┘         │
│  500/500 characters                                              │
│                                                                  │
│  VENUE STATUS                                                    │
│  ☑ Active                                                        │
│                                                                  │
│                                  [UPDATE VENUE] [DELETE VENUE]   │
└─────────────────────────────────────────────────────────────────┘
```

**Mobile View (<768px)**

```
┌──────────────────────────────┐
│  VENUE MANAGEMENT      [CARD]│
├──────────────────────────────┤
│                              │
│  VENUE NAME *                │
│  [Salem Community Center ]   │
│                              │
│  LOCATION (CITY, STATE)      │
│  [Salem, MA              ]   │
│  Public location shown to    │
│  non-vetted users before     │
│  RSVP. Full details after    │
│  registration.               │
│                              │
│  DIRECTIONS                  │
│  ┌────────────────────────┐ │
│  │ 123 Main Street,       │ │
│  │ Salem, MA 01970        │ │
│  │ Enter through side     │ │
│  │ door.                  │ │
│  └────────────────────────┘ │
│  245/500 characters          │
│                              │
│  NOTES                       │
│  ┌────────────────────────┐ │
│  │ Accessible venue       │ │
│  │                        │ │
│  └────────────────────────┘ │
│  24/500 characters           │
│                              │
│  VENUE STATUS                │
│  ☑ Active                    │
│                              │
│  [UPDATE VENUE]              │
│  [DELETE VENUE]              │
└──────────────────────────────┘
```

### 2. Event Card - Location Display (Non-Vetted User)

**Desktop Event Card**

```
┌──────────────────────────────────────────┐
│  [EVENT IMAGE]                           │
│                                          │
├──────────────────────────────────────────┤
│  INTRODUCTION TO ROPE BONDAGE            │
│  Saturday, December 2, 2025 • 7:00 PM   │
│                                          │
│  📍 Salem, MA                            │
│                                          │
│  Taught by: Jane Rigger                 │
│                                          │
│  Learn the fundamentals of safe and...  │
│                                          │
│  💵 $35 (Sliding scale available)        │
│  👥 5 spots remaining                    │
│                                          │
│              [VIEW DETAILS]              │
└──────────────────────────────────────────┘
```

### 3. Event Card - Location Display (Vetted User)

**Desktop Event Card**

```
┌──────────────────────────────────────────┐
│  [EVENT IMAGE]                           │
│                                          │
├──────────────────────────────────────────┤
│  INTRODUCTION TO ROPE BONDAGE            │
│  Saturday, December 2, 2025 • 7:00 PM   │
│                                          │
│  📍 Salem Community Center               │
│                                          │
│  Taught by: Jane Rigger                 │
│                                          │
│  Learn the fundamentals of safe and...  │
│                                          │
│  💵 $35 (Sliding scale available)        │
│  👥 5 spots remaining                    │
│                                          │
│              [VIEW DETAILS]              │
└──────────────────────────────────────────┘
```

### 4. Event Details Page - Location Section (Non-Vetted, Before RSVP)

**Desktop View**

```
┌─────────────────────────────────────────────────────────────┐
│  INTRODUCTION TO ROPE BONDAGE                               │
│  Saturday, December 2, 2025 • 7:00 PM - 10:00 PM           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  📍 LOCATION                                                │
│  Salem, MA                                                  │
│                                                             │
│  ℹ️ Full venue address and directions will be provided     │
│     after registration.                                     │
│                                                             │
│  - - - - - - - - - - - - - - - - - - - - - - - - - - - -   │
│                                                             │
│  👤 INSTRUCTOR                                              │
│  Jane Rigger                                                │
│  ...                                                        │
│                                                             │
│  💵 PRICING                                                 │
│  $35 (Sliding scale available)                             │
│  ...                                                        │
│                                                             │
│  [PURCHASE TICKET] [ADD TO CALENDAR]                        │
└─────────────────────────────────────────────────────────────┘
```

### 5. Event Details Page - Location Section (Vetted User OR After RSVP)

**Desktop View**

```
┌─────────────────────────────────────────────────────────────┐
│  INTRODUCTION TO ROPE BONDAGE                               │
│  Saturday, December 2, 2025 • 7:00 PM - 10:00 PM           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  📍 VENUE                                                   │
│  Salem Community Center                                     │
│                                                             │
│  📝 DIRECTIONS                                              │
│  123 Main Street, Salem, MA 01970                          │
│  Enter through side door.                                  │
│  Free parking in rear lot.                                 │
│                                                             │
│  [📋 COPY ADDRESS] [🗺️ OPEN IN MAPS]                       │
│                                                             │
│  - - - - - - - - - - - - - - - - - - - - - - - - - - - -   │
│                                                             │
│  👤 INSTRUCTOR                                              │
│  Jane Rigger                                                │
│  ...                                                        │
└─────────────────────────────────────────────────────────────┘
```

## Mantine Components Used

| Component | Purpose | Configuration |
|-----------|---------|--------------|
| **TextInput** | Location field in admin form | Label, placeholder, description text, maxLength={100} |
| **Textarea** | Directions field (existing) | Unchanged from current implementation |
| **Text** | Location display on event cards | Size="sm", icon with emoji, color variants |
| **Stack** | Layout for location section in event details | Gap="xs", spacing control |
| **Alert** | Info message about full details after RSVP | Icon, color="blue", variant="light" |
| **Group** | Button group for Copy/Maps actions | Gap="sm", justify="flex-start" |
| **ActionIcon** | Icon buttons for Copy/Maps | Size="md", variant="subtle" |
| **Box** | Container for location sections | Padding, borders, responsive styles |

## Component Specifications

### Admin Form - Location Field

**Component**: Mantine TextInput

```tsx
<TextInput
  label="Location (city, state)"
  placeholder="e.g., Salem, MA"
  description="Public location (city and state) shown to non-vetted users before RSVP or ticket purchase. Full venue details are shown after registration."
  maxLength={100}
  value={location}
  onChange={(e) => setLocation(e.currentTarget.value)}
  styles={{
    label: {
      fontFamily: 'var(--font-heading)',
      fontSize: '14px',
      fontWeight: 600,
      color: 'var(--color-smoke)',
      textTransform: 'uppercase',
      letterSpacing: '0.5px',
      marginBottom: 'var(--space-xs)',
    },
    description: {
      fontSize: '13px',
      color: 'var(--color-stone)',
      marginTop: 'var(--space-xs)',
      lineHeight: 1.5,
    },
  }}
/>
```

**Positioning**: Between "Venue Name" and "Directions" fields

**Validation**:
- Optional field (can be empty)
- Max length: 100 characters
- Character counter shows below input when typing

### Event Card - Location Display

**Component**: Mantine Text with icon

**Non-Vetted User Pattern**:
```tsx
<Text size="sm" c="dimmed" style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
  <span role="img" aria-label="Location">📍</span>
  {event.venue?.location || 'Location TBA'}
</Text>
```

**Vetted User Pattern**:
```tsx
<Text size="sm" c="dimmed" style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
  <span role="img" aria-label="Venue">📍</span>
  {event.venue?.name || 'Venue TBA'}
</Text>
```

**Logic**:
- Check user vetting status OR event participation status
- Display `venue.location` for non-vetted non-participants
- Display `venue.name` for vetted members OR participants

### Event Details - Location Section

**Before RSVP (Non-Vetted)**:

```tsx
<Stack gap="xs">
  <Text
    size="md"
    fw={600}
    style={{
      fontFamily: 'var(--font-heading)',
      textTransform: 'uppercase',
      letterSpacing: '1px',
      color: 'var(--color-charcoal)',
    }}
  >
    📍 LOCATION
  </Text>
  <Text size="md">{event.venue?.location || 'Location TBA'}</Text>

  <Alert
    icon={<IconInfoCircle />}
    color="blue"
    variant="light"
    styles={{
      root: {
        backgroundColor: 'rgba(157, 78, 221, 0.1)',
        borderColor: 'var(--color-electric)',
      },
    }}
  >
    Full venue address and directions will be provided after registration.
  </Alert>
</Stack>
```

**After RSVP (Vetted or Participant)**:

```tsx
<Stack gap="md">
  <Box>
    <Text
      size="md"
      fw={600}
      style={{
        fontFamily: 'var(--font-heading)',
        textTransform: 'uppercase',
        letterSpacing: '1px',
        color: 'var(--color-charcoal)',
        marginBottom: 'var(--space-xs)',
      }}
    >
      📍 VENUE
    </Text>
    <Text size="md">{event.venue?.name}</Text>
  </Box>

  <Box>
    <Text
      size="md"
      fw={600}
      style={{
        fontFamily: 'var(--font-heading)',
        textTransform: 'uppercase',
        letterSpacing: '1px',
        color: 'var(--color-charcoal)',
        marginBottom: 'var(--space-xs)',
      }}
    >
      📝 DIRECTIONS
    </Text>
    <Text
      size="sm"
      style={{
        whiteSpace: 'pre-wrap',
        lineHeight: 1.6,
        color: 'var(--color-smoke)',
      }}
    >
      {event.venue?.directions}
    </Text>
  </Box>

  <Group gap="sm">
    <Button
      variant="outline"
      size="sm"
      leftSection={<IconCopy />}
      onClick={handleCopyAddress}
    >
      Copy Address
    </Button>
    <Button
      variant="outline"
      size="sm"
      leftSection={<IconMap />}
      onClick={handleOpenMaps}
    >
      Open in Maps
    </Button>
  </Group>
</Stack>
```

## Interaction Patterns

### Visibility Logic

**Event Card Location Display**:
```typescript
const shouldShowLocation = !user?.isVetted && !isParticipant;
const locationText = shouldShowLocation
  ? event.venue?.location
  : event.venue?.name;
```

**Event Details Location Section**:
```typescript
const hasVenueAccess = user?.isVetted || isParticipant;

return hasVenueAccess ? (
  <FullVenueDetails venue={event.venue} />
) : (
  <LimitedLocationDisplay location={event.venue?.location} />
);
```

### Copy Address Action

```typescript
const handleCopyAddress = () => {
  // Extract first line of directions (usually the address)
  const addressLine = venue.directions.split('\n')[0];
  navigator.clipboard.writeText(addressLine);

  notifications.show({
    color: 'green',
    title: 'Address copied',
    message: 'Venue address copied to clipboard',
    autoClose: 3000,
  });
};
```

### Open in Maps Action

```typescript
const handleOpenMaps = () => {
  // Extract address from first line of directions
  const addressLine = venue.directions.split('\n')[0];
  const mapsUrl = `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(addressLine)}`;
  window.open(mapsUrl, '_blank');
};
```

## Responsive Breakpoints

**Mobile (<768px)**:
- Location field: Full width, stacked layout
- Event card: Single column, location text wraps
- Event details: Full width sections, stacked buttons
- Alert message: Smaller font size (13px)

**Desktop (≥769px)**:
- Location field: Full width within form container
- Event card: Location text inline with icon
- Event details: Standard spacing, inline button group
- Alert message: Standard font size (14px)

## Accessibility Requirements

### ARIA Labels

```tsx
// Location icon
<span role="img" aria-label="Location">📍</span>

// Venue icon
<span role="img" aria-label="Venue">📍</span>

// Copy button
<Button aria-label="Copy venue address to clipboard">
  Copy Address
</Button>

// Maps button
<Button aria-label="Open venue location in Google Maps">
  Open in Maps
</Button>
```

### Screen Reader Announcements

- Location field description is read with input
- Alert message about full details is announced
- Button actions provide feedback via notifications
- All icons have proper aria-labels

### Keyboard Navigation

- All interactive elements are keyboard accessible
- Tab order: Location input → other form fields → buttons
- Enter/Space activates buttons
- Focus indicators visible on all interactive elements

### Color Contrast

- Location text: 4.5:1 minimum contrast (WCAG AA)
- Alert background: Sufficient contrast with text
- Button text: Meets contrast requirements
- Icon + text combinations: Clear visual hierarchy

## Design System Integration

### Typography

- **Field Labels**: Montserrat 600, 14px, uppercase, 0.5px letter-spacing
- **Section Headers**: Montserrat 600, 16px, uppercase, 1px letter-spacing
- **Body Text**: Source Sans 3 400, 16px, 1.7 line-height
- **Helper Text**: Source Sans 3 400, 13px, dimmed color

### Colors

- **Location text (non-vetted)**: `var(--color-smoke)` (#4A4A4A)
- **Venue text (vetted)**: `var(--color-charcoal)` (#2B2B2B)
- **Alert background**: Electric purple at 10% opacity
- **Alert border**: `var(--color-electric)` (#9D4EDD)
- **Icon color**: Matches text color

### Spacing

- **Field spacing**: `var(--space-md)` (24px) between fields
- **Section spacing**: `var(--space-lg)` (32px) between sections
- **Alert padding**: `var(--space-sm)` (16px) internal padding
- **Button gap**: `var(--space-sm)` (16px) between buttons

### Buttons

- **Copy Address**: Secondary button (burgundy outline)
- **Open in Maps**: Secondary button (burgundy outline)
- Icon + text pattern with left section

## Mobile-First Considerations

### Touch Targets

- All buttons: Minimum 44×44px touch target
- Location field: Full-width for easy tapping
- Icon buttons: Adequate spacing (16px gap)

### Content Wrapping

- Location text wraps on small screens
- Alert message uses responsive font sizing
- Button group stacks vertically on very small screens (<360px)

### Performance

- Location data loaded with event data (no extra API call)
- Conditional rendering minimizes DOM nodes
- Icons use emoji (no additional asset loading)

## Privacy & Security UX

### Clear Communication

- Helper text explains when full details are shown
- Alert message reinforces privacy protection
- Icon usage differentiates public location from full venue

### Progressive Disclosure

- General location visible upfront for discovery
- Full details revealed after commitment (RSVP/ticket)
- Vetted members see full details immediately (trusted status)

### User Trust

- Transparency about what information is shown
- Clear indication of when access is granted
- Consistent pattern across event cards and details pages

## Quality Checklist

- [ ] Matches Design System v7 typography and colors
- [ ] Uses Mantine v7 components consistently
- [ ] Responsive on all devices (mobile-first)
- [ ] Meets WCAG 2.1 AA accessibility standards
- [ ] Clear user flows for all user types
- [ ] Privacy protection is transparent to users
- [ ] Location field integrates smoothly into admin form
- [ ] Event card display logic is clear and testable
- [ ] Event details section provides appropriate access
- [ ] Copy/Maps actions work across browsers

## Implementation Notes

### API Integration

**Venue DTO should include**:
```typescript
interface VenueDto {
  id: number;
  name: string;
  location: string; // NEW: City, state (max 100 chars)
  directions: string;
  notes: string;
  isActive: boolean;
}
```

### User Context Required

Components need access to:
- `user.isVetted` (boolean) - User's vetting status
- `isParticipant` (boolean) - Whether user registered/purchased for this event

### State Management

Location visibility logic can be extracted into a custom hook:

```typescript
const useVenueAccess = (user, event) => {
  const isVetted = user?.isVetted || false;
  const isParticipant = event?.userParticipation?.isRegistered || false;
  const hasFullAccess = isVetted || isParticipant;

  return {
    hasFullAccess,
    shouldShowLocation: !hasFullAccess,
    shouldShowVenue: hasFullAccess,
  };
};
```

## Testing Scenarios

### Admin Form Testing
1. Add new venue with Location field populated
2. Edit existing venue, add Location field
3. Verify character counter at 100 chars
4. Save venue with empty Location field (optional)
5. Verify Location appears in venue list

### Event Card Testing
1. Non-vetted user sees Location (city, state)
2. Vetted user sees VenueName
3. Location text wraps properly on mobile
4. Icon + text alignment correct

### Event Details Testing
1. Non-vetted user before RSVP sees Location + Alert
2. Non-vetted user after RSVP sees VenueName + Directions
3. Vetted user always sees VenueName + Directions
4. Copy Address button copies correct text
5. Open in Maps opens Google Maps with correct query
6. Alert message displays with proper styling

### Accessibility Testing
1. Screen reader announces all labels correctly
2. Keyboard navigation works for all interactive elements
3. Color contrast meets WCAG AA standards
4. Focus indicators visible on all elements

---

**Next Steps**: Use this design as the foundation for React implementation. Reference component specifications for exact Mantine component usage and styling.
