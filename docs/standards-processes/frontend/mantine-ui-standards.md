# Mantine UI Standards

**Purpose**: Mantine v7 component usage patterns, theming, and UI consistency standards.
**When to Read**: When implementing UI components, forms, or layouts with Mantine.
**Related**: [React Patterns](./react-patterns.md), [Form Patterns](/docs/standards-processes/forms-standardization.md)

## Mantine Version

**Current Version**: Mantine v7
**Migration Status**: Migrated from Blazor to React + Mantine v7

## 🎯 Responsive Context Strategy

**WitchCityRope has different responsive requirements by feature area:**

### Admin Areas (`/features/admin/*`)
- **Desktop-only optimization** (1440px)
- **NO mobile testing required**
- Focus on data tables, forms, management interfaces
- Use fixed layouts where appropriate

### Check-In System (`/features/checkin/*`)
- **Tablet + Desktop** (768px + 1440px)
- **NO mobile support needed**
- Kiosk-style interface for event check-in
- Larger touch targets for tablets

### Public Areas (`/features/public/*`, `/features/events/public/*`)
- **Mobile-first approach** (375px + 768px + 1440px)
- **All breakpoints required**
- Optimize for community members browsing on phones
- Progressive enhancement from mobile → desktop

## Component Import Pattern

```typescript
// ✅ CORRECT: Import from @mantine/core
import { Button, TextInput, Modal, Table, Alert } from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { modals } from '@mantine/modals';
```

## Form Components

### Standard Form Pattern
```typescript
import { useForm } from '@mantine/form';
import { TextInput, Button, Stack } from '@mantine/core';

interface FormValues {
  name: string;
  email: string;
}

export function MyForm() {
  const form = useForm<FormValues>({
    initialValues: {
      name: '',
      email: '',
    },
    validate: {
      name: (value) => (value.length < 2 ? 'Name too short' : null),
      email: (value) => (/^\S+@\S+$/.test(value) ? null : 'Invalid email'),
    },
  });

  const handleSubmit = async (values: FormValues) => {
    // Handle submission
  };

  return (
    <form onSubmit={form.onSubmit(handleSubmit)}>
      <Stack gap="md">
        <TextInput
          label="Name"
          placeholder="Enter your name"
          {...form.getInputProps('name')}
        />
        <TextInput
          label="Email"
          type="email"
          placeholder="your@email.com"
          {...form.getInputProps('email')}
        />
        <Button type="submit">Submit</Button>
      </Stack>
    </form>
  );
}
```

## Modal Pattern

```typescript
import { Modal, Button } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';

export function MyComponent() {
  const [opened, { open, close }] = useDisclosure(false);

  return (
    <>
      <Button onClick={open}>Open Modal</Button>

      <Modal
        opened={opened}
        onClose={close}
        title="Modal Title"
        size="lg"
      >
        <div>Modal content goes here</div>
      </Modal>
    </>
  );
}
```

## Notifications

```typescript
import { notifications } from '@mantine/notifications';

// Success notification
notifications.show({
  title: 'Success',
  message: 'Your changes have been saved',
  color: 'green',
});

// Error notification
notifications.show({
  title: 'Error',
  message: 'Failed to save changes',
  color: 'red',
});

// Info notification
notifications.show({
  title: 'Information',
  message: 'Please review your input',
  color: 'blue',
});
```

## Table Pattern

```typescript
import { Table } from '@mantine/core';

interface Event {
  id: number;
  name: string;
  date: string;
}

export function EventTable({ events }: { events: Event[] }) {
  return (
    <Table>
      <Table.Thead>
        <Table.Tr>
          <Table.Th>ID</Table.Th>
          <Table.Th>Name</Table.Th>
          <Table.Th>Date</Table.Th>
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {events.map((event) => (
          <Table.Tr key={event.id}>
            <Table.Td>{event.id}</Table.Td>
            <Table.Td>{event.name}</Table.Td>
            <Table.Td>{event.date}</Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  );
}
```

## Layout Components

### Stack for Vertical Spacing
```typescript
import { Stack } from '@mantine/core';

<Stack gap="md">
  <div>Item 1</div>
  <div>Item 2</div>
  <div>Item 3</div>
</Stack>
```

### Group for Horizontal Spacing
```typescript
import { Group } from '@mantine/core';

<Group gap="md" justify="space-between">
  <Button>Cancel</Button>
  <Button>Submit</Button>
</Group>
```

### Grid for Responsive Layouts
```typescript
import { Grid } from '@mantine/core';

<Grid>
  <Grid.Col span={{ base: 12, md: 6, lg: 4 }}>
    <div>Column 1</div>
  </Grid.Col>
  <Grid.Col span={{ base: 12, md: 6, lg: 4 }}>
    <div>Column 2</div>
  </Grid.Col>
  <Grid.Col span={{ base: 12, md: 6, lg: 4 }}>
    <div>Column 3</div>
  </Grid.Col>
</Grid>
```

## Loading States

```typescript
import { Loader, Center } from '@mantine/core';

export function LoadingState() {
  return (
    <Center h="200px">
      <Loader size="lg" />
    </Center>
  );
}
```

## Alert Components

```typescript
import { Alert } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';

<Alert
  icon={<IconAlertCircle size={16} />}
  title="Warning"
  color="yellow"
>
  Please review your input before submitting.
</Alert>
```

## Button Variants

```typescript
import { Button, Group } from '@mantine/core';

<Group>
  <Button variant="filled">Filled</Button>
  <Button variant="light">Light</Button>
  <Button variant="outline">Outline</Button>
  <Button variant="subtle">Subtle</Button>
  <Button variant="default">Default</Button>
</Group>
```

## 🚨 MANDATORY Button Styling Checklist

**EVERY Button component MUST include these styles to prevent text cutoff.**

### Critical Issue: Recurring Button Text Cutoff
**Documented**: 2025-09-22 AND 2025-10-05 (same bug twice!)
**Cause**: Missing explicit height/padding in Button styles
**Solution**: ALWAYS use this checklist

### Mandatory Button Styles Pattern

```typescript
import { Button } from '@mantine/core';

// ✅ CORRECT: All buttons MUST use this pattern
<Button
  variant="filled"
  color="blue"
  styles={{
    root: {
      height: '44px',           // REQUIRED: Explicit height
      paddingTop: '12px',       // REQUIRED: Explicit top padding
      paddingBottom: '12px',    // REQUIRED: Explicit bottom padding
      fontSize: '14px',         // REQUIRED: Consistent font size
      lineHeight: '1.2',        // REQUIRED: Prevents text cutoff
      fontWeight: 600,          // Optional: Adjust as needed
    }
  }}
>
  Button Text
</Button>

// ❌ WRONG: Using size prop alone causes text cutoff
<Button size="sm">
  Button Text  {/* Text will be cut off! */}
</Button>

// ❌ WRONG: Style props without explicit height/padding
<Button
  variant="filled"
  color="blue"
  style={{ borderColor: '#880124' }}
>
  Button Text  {/* Text will still cut off! */}
</Button>
```

### Button Implementation Checklist

**Before committing ANY button, verify:**
- [ ] `height: '44px'` is set in `styles.root`
- [ ] `paddingTop: '12px'` is set in `styles.root`
- [ ] `paddingBottom: '12px'` is set in `styles.root`
- [ ] `fontSize: '14px'` is set in `styles.root`
- [ ] `lineHeight: '1.2'` is set in `styles.root`
- [ ] Button text is fully visible in Chrome DevTools screenshot
- [ ] NO reliance on `size` prop alone

### Why This Matters

**Without explicit height/padding:**
- Mantine's default Button styles conflict with custom CSS
- Text gets cut off at top and bottom
- Issue is invisible in code but obvious to users
- Recurring bug costs developer time (documented twice)

**Prevention is mandatory, not optional.**

## Color Scheme

Use Mantine's built-in colors:
- `blue`: Primary actions
- `green`: Success states
- `red`: Errors, destructive actions
- `yellow`: Warnings
- `gray`: Neutral elements
- `dark`: Dark mode elements

```typescript
<Button color="blue">Primary Action</Button>
<Button color="green">Success Action</Button>
<Button color="red">Delete</Button>
```

## Responsive Design

### Mantine Breakpoints
```typescript
// Mantine v7 default breakpoints
base: 0px      // Mobile (< 576px) - Use for mobile-first base styles
xs: 576px      // Small mobile (≥ 576px)
sm: 768px      // Tablet (≥ 768px)
md: 1024px     // Desktop (≥ 1024px)
lg: 1440px     // Large desktop (≥ 1440px)
xl: 1920px     // Extra large (≥ 1920px)
```

### 🚨 CRITICAL: Always Use `base` Property for Mobile

**Problem**: Mantine responsive props default to `xs` (576px), leaving mobile screens < 576px unstyled.

```typescript
// ❌ WRONG: Mobile screens < 576px get no spacing
<Stack gap={{ sm: 'md', lg: 'lg' }}>
  {/* On 375px mobile, gap is undefined! */}
</Stack>

// ✅ CORRECT: Use `base` for mobile-first styling
<Stack gap={{ base: 'sm', sm: 'md', lg: 'lg' }}>
  {/* base applies to ALL screens, then sm/lg override */}
</Stack>
```

### Responsive Props Pattern

```typescript
import { Stack, Grid, Box } from '@mantine/core';

// Mobile-first spacing (public areas)
<Stack
  gap={{ base: 'sm', sm: 'md', lg: 'lg' }}
  p={{ base: 'xs', sm: 'sm', lg: 'md' }}
>
  {/* Progressive enhancement from mobile → desktop */}
</Stack>

// Grid with responsive columns
<Grid>
  <Grid.Col span={{ base: 12, sm: 6, lg: 4 }}>
    {/* Full width mobile, half tablet, third desktop */}
  </Grid.Col>
</Grid>
```

### Show/Hide Elements Responsively

**Performance-optimized visibility controls:**

```typescript
import { Box } from '@mantine/core';

// ✅ CORRECT: Use hiddenFrom/visibleFrom (better performance)
<Box hiddenFrom="sm">Mobile only content</Box>
<Box visibleFrom="md">Desktop only content</Box>

// ❌ AVOID: Responsive display prop (less performant)
<Box display={{ base: 'block', sm: 'none' }}>Mobile only</Box>
```

### Layout Component Selection

**Grid vs SimpleGrid vs Flex - Decision Tree:**

```typescript
// Use Grid when: Variable column widths, complex responsive layouts
<Grid>
  <Grid.Col span={{ base: 12, md: 8 }}>Main content</Grid.Col>
  <Grid.Col span={{ base: 12, md: 4 }}>Sidebar</Grid.Col>
</Grid>

// Use SimpleGrid when: Equal-width columns, simple grids
<SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="lg">
  <Card>Item 1</Card>
  <Card>Item 2</Card>
  <Card>Item 3</Card>
</SimpleGrid>

// Use Flex when: Horizontal/vertical alignment, dynamic spacing
<Flex
  direction={{ base: 'column', sm: 'row' }}
  gap="md"
  justify="space-between"
>
  <Button>Cancel</Button>
  <Button>Submit</Button>
</Flex>
```

### Common Responsive Patterns

```typescript
// Mobile: Stack vertically, Desktop: Horizontal
<Flex direction={{ base: 'column', md: 'row' }} gap="md">
  <Box style={{ flex: 1 }}>Content 1</Box>
  <Box style={{ flex: 1 }}>Content 2</Box>
</Flex>

// Responsive text sizing
<Text size={{ base: 'sm', md: 'md', lg: 'lg' }}>
  Scales with screen size
</Text>

// Responsive padding/margins
<Box
  p={{ base: 'xs', sm: 'md', lg: 'xl' }}
  m={{ base: 0, md: 'md' }}
>
  Content
</Box>
```

## Accessibility

Mantine components include accessibility features by default:
- Use `label` props for form inputs
- Use `title` for modals
- Use `aria-label` for icon-only buttons

```typescript
<Button aria-label="Close modal" onClick={close}>
  <IconX size={16} />
</Button>
```

## Standards Maintenance

When implementing new UI patterns:
1. Use Mantine v7 components first
2. Document custom patterns here
3. Ensure responsive design
4. Test accessibility
5. Follow color scheme standards

---

*This document is maintained by the UI Designer and React Developer agents.*
