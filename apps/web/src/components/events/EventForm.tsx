import React, { useState } from 'react';
import {
  Card,
  Tabs,
  TextInput,
  Group,
  Text,
  Radio,
  Select,
  Textarea,
  Stack,
  Title,
  Divider,
  MultiSelect,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { TinyMCERichTextEditor } from '../forms/TinyMCERichTextEditor';

import { EventSessionsGrid, EventSession } from './EventSessionsGrid';
import { EventTicketTypesGrid, EventTicketType } from './EventTicketTypesGrid';

export interface EventFormData {
  // Basic Info
  eventType: 'class' | 'social';
  title: string;
  shortDescription: string;
  fullDescription: string;
  policies: string;
  venueId: string;
  teacherIds: string[];
  
  // Sessions and Tickets
  sessions: EventSession[];
  ticketTypes: EventTicketType[];
}

interface EventFormProps {
  initialData?: Partial<EventFormData>;
  onSubmit: (data: EventFormData) => void;
  onCancel: () => void;
  isSubmitting?: boolean;
}

export const EventForm: React.FC<EventFormProps> = ({
  initialData,
  onSubmit,
  onCancel,
  isSubmitting = false,
}) => {
  const [activeTab, setActiveTab] = useState<string>('basic-info');

  // Form state management
  const form = useForm<EventFormData>({
    initialValues: {
      eventType: 'class',
      title: '',
      shortDescription: '',
      fullDescription: '',
      policies: '',
      venueId: '',
      teacherIds: [],
      sessions: [],
      ticketTypes: [],
      ...initialData,
    },
    validate: {
      title: (value) => (!value ? 'Event title is required' : null),
      shortDescription: (value) => {
        if (!value) return 'Short description is required';
        if (value.length > 160) return 'Short description must be 160 characters or less';
        return null;
      },
      fullDescription: (value) => (!value ? 'Full description is required' : null),
      venueId: (value) => (!value ? 'Venue selection is required' : null),
    },
  });

  // Handler functions for rich text editors
  const handleFullDescriptionChange = (content: string) => {
    form.setFieldValue('fullDescription', content);
  };

  const handlePoliciesChange = (content: string) => {
    form.setFieldValue('policies', content);
  };

  // Mock data for dropdowns
  const venues = [
    { value: 'main-studio', label: 'Main Studio' },
    { value: 'meditation-room', label: 'Meditation Room' },
    { value: 'outdoor-space', label: 'Outdoor Space' },
    { value: 'off-site', label: 'Off-site Location' },
  ];

  const availableTeachers = [
    { value: 'river-moon', label: 'River Moon' },
    { value: 'sage-blackthorne', label: 'Sage Blackthorne' },
    { value: 'phoenix-rose', label: 'Phoenix Rose' },
    { value: 'willow-craft', label: 'Willow Craft' },
    { value: 'raven-night', label: 'Raven Night' },
  ];

  // Session management handlers
  const handleEditSession = (sessionId: string) => {
    // This would open a modal in the real implementation
    console.log('Edit session:', sessionId);
  };

  const handleDeleteSession = (sessionId: string) => {
    const updatedSessions = form.values.sessions.filter(session => session.id !== sessionId);
    form.setFieldValue('sessions', updatedSessions);
  };

  const handleAddSession = () => {
    // This would open a modal in the real implementation
    console.log('Add new session');
  };

  // Ticket type management handlers
  const handleEditTicketType = (ticketTypeId: string) => {
    // This would open a modal in the real implementation
    console.log('Edit ticket type:', ticketTypeId);
  };

  const handleDeleteTicketType = (ticketTypeId: string) => {
    const updatedTicketTypes = form.values.ticketTypes.filter(ticket => ticket.id !== ticketTypeId);
    form.setFieldValue('ticketTypes', updatedTicketTypes);
  };

  const handleAddTicketType = () => {
    // This would open a modal in the real implementation
    console.log('Add new ticket type');
  };

  const handleSubmit = form.onSubmit((values) => {
    onSubmit(values);
  });

  return (
    <Card shadow="md" radius="lg" p="xl" style={{ backgroundColor: 'white' }}>
      <form onSubmit={handleSubmit}>
        <Tabs value={activeTab} onChange={setActiveTab} variant="pills" radius="md">
          <Tabs.List
            style={{
              backgroundColor: 'var(--mantine-color-gray-0)',
              borderBottom: '2px solid var(--mantine-color-burgundy-3)',
              padding: 'var(--mantine-spacing-md)',
            }}
          >
            <Tabs.Tab value="basic-info">Basic Info</Tabs.Tab>
            <Tabs.Tab value="tickets-orders">Tickets/Orders</Tabs.Tab>
            <Tabs.Tab value="emails">Emails</Tabs.Tab>
            <Tabs.Tab value="volunteers-staff">Volunteers/Staff</Tabs.Tab>
          </Tabs.List>

          {/* Basic Info Tab */}
          <Tabs.Panel value="basic-info" pt="xl">
            <Stack gap="xl">
              {/* Event Details Section */}
              <div>
                <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                  Event Details
                </Title>

                {/* Event Type Toggle */}
                <Radio.Group
                  {...form.getInputProps('eventType')}
                  mb="lg"
                >
                  <Group grow>
                    <Card
                      withBorder
                      p="md"
                      radius="md"
                      style={{
                        cursor: 'pointer',
                        borderColor: form.values.eventType === 'class' ? 'var(--mantine-color-burgundy-6)' : 'var(--mantine-color-gray-4)',
                        backgroundColor: form.values.eventType === 'class' ? 'rgba(136, 1, 36, 0.1)' : 'white',
                      }}
                      onClick={() => form.setFieldValue('eventType', 'class')}
                    >
                      <Radio value="class" label="" style={{ display: 'none' }} />
                      <div style={{ textAlign: 'center' }}>
                        <Text fw={600} c="burgundy" size="lg">Class</Text>
                        <Text size="sm" c="dimmed">Educational workshop requiring payment</Text>
                      </div>
                    </Card>
                    <Card
                      withBorder
                      p="md"
                      radius="md"
                      style={{
                        cursor: 'pointer',
                        borderColor: form.values.eventType === 'social' ? 'var(--mantine-color-burgundy-6)' : 'var(--mantine-color-gray-4)',
                        backgroundColor: form.values.eventType === 'social' ? 'rgba(136, 1, 36, 0.1)' : 'white',
                      }}
                      onClick={() => form.setFieldValue('eventType', 'social')}
                    >
                      <Radio value="social" label="" style={{ display: 'none' }} />
                      <div style={{ textAlign: 'center' }}>
                        <Text fw={600} c="burgundy" size="lg">Social Event</Text>
                        <Text size="sm" c="dimmed">Community gathering with volunteers</Text>
                      </div>
                    </Card>
                  </Group>
                </Radio.Group>

                {/* Event Title */}
                <TextInput
                  label="Event Title"
                  placeholder="Enter event title"
                  required
                  mb="md"
                  {...form.getInputProps('title')}
                />

                {/* Short Description */}
                <TextInput
                  label="Short Description"
                  placeholder="Brief description for cards and grid views"
                  description="Brief description for cards and grid views (160 characters max)"
                  required
                  maxLength={160}
                  mb="md"
                  {...form.getInputProps('shortDescription')}
                />

                {/* Full Description */}
                <TinyMCERichTextEditor
                  label="Full Event Description"
                  description="This detailed description will be visible on the public events page"
                  required
                  value={form.values.fullDescription}
                  onChange={handleFullDescriptionChange}
                  error={form.errors.fullDescription}
                  height={200}
                  placeholder="Enter detailed description of the event..."
                />

                {/* Policies & Procedures */}
                <TinyMCERichTextEditor
                  label="Policies & Procedures"
                  description="Studio-specific policies, prerequisites, safety requirements, etc. (managed by studio/admin, teachers cannot edit)"
                  value={form.values.policies}
                  onChange={handlePoliciesChange}
                  error={form.errors.policies}
                  height={150}
                  placeholder="Enter policies and procedures..."
                />
              </div>

              {/* Venue Section */}
              <div>
                <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                  Venue
                </Title>
                <Group grow>
                  <Select
                    label="Venue"
                    placeholder="Select venue..."
                    data={venues}
                    required
                    {...form.getInputProps('venueId')}
                  />
                  <button type="button" className="btn btn-secondary">
                    Add Venue
                  </button>
                </Group>
              </div>

              {/* Teachers/Instructors Section */}
              <div>
                <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                  Teachers/Instructors
                </Title>
                <MultiSelect
                  label="Select Teachers"
                  placeholder="Choose teachers for this event"
                  data={availableTeachers}
                  searchable
                  {...form.getInputProps('teacherIds')}
                />
              </div>

              {/* Save Buttons */}
              <Group justify="flex-end" mt="xl">
                <button type="button" className="btn btn-secondary" onClick={onCancel}>
                  Cancel
                </button>
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={isSubmitting}
                >
                  {isSubmitting ? 'Saving...' : 'Save Draft'}
                </button>
              </Group>
            </Stack>
          </Tabs.Panel>

          {/* Tickets/Orders Tab */}
          <Tabs.Panel value="tickets-orders" pt="xl">
            <Stack gap="xl">
              {/* Event Sessions */}
              <div>
                <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                  Event Sessions
                </Title>
                <EventSessionsGrid
                  sessions={form.values.sessions}
                  onEditSession={handleEditSession}
                  onDeleteSession={handleDeleteSession}
                  onAddSession={handleAddSession}
                />
              </div>

              {/* Ticket Types */}
              <div>
                <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                  Ticket Types
                </Title>
                <EventTicketTypesGrid
                  ticketTypes={form.values.ticketTypes}
                  onEditTicketType={handleEditTicketType}
                  onDeleteTicketType={handleDeleteTicketType}
                  onAddTicketType={handleAddTicketType}
                />
              </div>
            </Stack>
          </Tabs.Panel>

          {/* Emails Tab */}
          <Tabs.Panel value="emails" pt="xl">
            <Stack gap="xl">
              <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                Email Templates
              </Title>
              
              {/* Automated Emails */}
              <div>
                <Title order={3} c="gray.8" mb="md">Automated Event Emails</Title>
                <Stack gap="md">
                  <Card withBorder p="md" radius="md">
                    <Group justify="space-between">
                      <div>
                        <Text fw={600} mb="xs">Registration Confirmation</Text>
                        <Text size="sm" c="dimmed">Sent automatically when someone registers for the event</Text>
                      </div>
                      <Group>
                        <button type="button" className="btn btn-secondary">Preview</button>
                        <button type="button" className="btn btn-secondary">Edit</button>
                      </Group>
                    </Group>
                  </Card>
                  
                  <Card withBorder p="md" radius="md">
                    <Group justify="space-between">
                      <div>
                        <Text fw={600} mb="xs">Event Reminder</Text>
                        <Text size="sm" c="dimmed">Sent 24 hours before the event starts</Text>
                      </div>
                      <Group>
                        <button type="button" className="btn btn-secondary">Preview</button>
                        <button type="button" className="btn btn-secondary">Edit</button>
                      </Group>
                    </Group>
                  </Card>
                  
                  <Card withBorder p="md" radius="md">
                    <Group justify="space-between">
                      <div>
                        <Text fw={600} mb="xs">Cancellation Notice</Text>
                        <Text size="sm" c="dimmed">Sent if the event is cancelled or postponed</Text>
                      </div>
                      <Group>
                        <button type="button" className="btn btn-secondary">Preview</button>
                        <button type="button" className="btn btn-secondary">Edit</button>
                      </Group>
                    </Group>
                  </Card>
                </Stack>
              </div>
              
              {/* Custom Email Options */}
              <div>
                <Title order={3} c="gray.8" mb="md">Custom Communications</Title>
                <Card withBorder p="md" radius="md">
                  <Text mb="md" fw={500}>Send Custom Email to Registrants</Text>
                  <Text size="sm" c="dimmed" mb="lg">
                    Send a custom message to all registered participants for this event.
                  </Text>
                  <Group>
                    <button type="button" className="btn btn-primary">Compose Email</button>
                    <Text size="sm" c="dimmed">
                      Will be sent to {form.values.sessions.reduce((sum, session) => sum + session.registeredCount, 0)} registered participants
                    </Text>
                  </Group>
                </Card>
              </div>
            </Stack>
          </Tabs.Panel>

          {/* Volunteers/Staff Tab */}
          <Tabs.Panel value="volunteers-staff" pt="xl">
            <Stack gap="xl">
              <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                Volunteer Positions
              </Title>
              
              {/* Add New Volunteer Position */}
              <div>
                <Title order={3} c="gray.8" mb="md">Add Volunteer Position</Title>
                <Card withBorder p="md" radius="md">
                  <Stack gap="md">
                    <Group grow>
                      <TextInput
                        label="Position Name"
                        placeholder="e.g., Setup Assistant, Greeter, etc."
                      />
                      <TextInput
                        label="Number Needed"
                        placeholder="1"
                        type="number"
                        min={1}
                      />
                    </Group>
                    <Textarea
                      label="Position Description"
                      placeholder="Describe the responsibilities and requirements for this volunteer position..."
                      minRows={2}
                    />
                    <Group justify="flex-start">
                      <button type="button" className="btn btn-primary">Add Position</button>
                    </Group>
                  </Stack>
                </Card>
              </div>
              
              {/* Current Volunteer Positions */}
              <div>
                <Title order={3} c="gray.8" mb="md">Current Volunteer Positions</Title>
                <Stack gap="md">
                  <Card withBorder p="md" radius="md">
                    <Group justify="space-between" align="flex-start">
                      <div style={{ flex: 1 }}>
                        <Group mb="xs">
                          <Text fw={600}>Setup Assistant</Text>
                          <Text size="sm" c="dimmed">• 2 needed • 1 assigned</Text>
                        </Group>
                        <Text size="sm" c="dimmed">
                          Help set up equipment, mats, and room arrangement before the event begins.
                        </Text>
                      </div>
                      <Group>
                        <button type="button" className="btn btn-secondary">Manage</button>
                        <button type="button" className="btn btn-secondary">Edit</button>
                      </Group>
                    </Group>
                  </Card>
                  
                  <Card withBorder p="md" radius="md">
                    <Group justify="space-between" align="flex-start">
                      <div style={{ flex: 1 }}>
                        <Group mb="xs">
                          <Text fw={600}>Greeter</Text>
                          <Text size="sm" c="dimmed">• 1 needed • 1 assigned</Text>
                        </Group>
                        <Text size="sm" c="dimmed">
                          Welcome participants, check them in, and direct them to changing areas.
                        </Text>
                      </div>
                      <Group>
                        <button type="button" className="btn btn-secondary">Manage</button>
                        <button type="button" className="btn btn-secondary">Edit</button>
                      </Group>
                    </Group>
                  </Card>
                  
                  <Card withBorder p="md" radius="md">
                    <Group justify="space-between" align="flex-start">
                      <div style={{ flex: 1 }}>
                        <Group mb="xs">
                          <Text fw={600}>Cleanup Helper</Text>
                          <Text size="sm" c="dimmed">• 3 needed • 0 assigned</Text>
                        </Group>
                        <Text size="sm" c="dimmed">
                          Assist with cleaning up after the event, putting away equipment and supplies.
                        </Text>
                      </div>
                      <Group>
                        <button type="button" className="btn btn-secondary">Manage</button>
                        <button type="button" className="btn btn-secondary">Edit</button>
                      </Group>
                    </Group>
                  </Card>
                </Stack>
              </div>
            </Stack>
          </Tabs.Panel>
        </Tabs>
      </form>
    </Card>
  );
};