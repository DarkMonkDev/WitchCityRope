# Mantine UI Standards

**Purpose**: Mantine v7 component usage patterns, theming, and UI consistency standards.
**When to Read**: When implementing UI components, forms, or layouts with Mantine.
**Related**: [React Patterns](./react-patterns.md), [Form Patterns](/docs/standards-processes/forms-standardization.md)

## Mantine Version

**Current Version**: Mantine v7
**Migration Status**: Migrated from Blazor to React + Mantine v7

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

Use Mantine's responsive props:

```typescript
import { Stack } from '@mantine/core';

<Stack
  gap={{ base: 'sm', sm: 'md', lg: 'lg' }}
  p={{ base: 'xs', sm: 'sm', lg: 'md' }}
>
  {/* Responsive spacing and padding */}
</Stack>
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
