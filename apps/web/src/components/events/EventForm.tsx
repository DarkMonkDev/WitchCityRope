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
  Button,
  Badge,
  Table,
  ActionIcon,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { Editor } from '@tinymce/tinymce-react';

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
  const [activeEmailTemplate, setActiveEmailTemplate] = useState<string>('confirmation');

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

  // TinyMCE configuration (commented out - using simple textarea for now)
  // const tinyMCEConfig = {
  //   height: 300,
  //   menubar: false,
  //   plugins: [
  //     'advlist', 'autolink', 'lists', 'link', 'image', 'charmap', 'preview',
  //     'anchor', 'searchreplace', 'visualblocks', 'code', 'fullscreen',
  //     'insertdatetime', 'media', 'table', 'help', 'wordcount'
  //   ],
  //   toolbar: 'undo redo | blocks | ' +
  //     'bold italic forecolor | alignleft aligncenter ' +
  //     'alignright alignjustify | bullist numlist outdent indent | ' +
  //     'removeformat | help',
  //   content_style: 'body { font-family: -apple-system, BlinkMacSystemFont, San Francisco, Segoe UI, Roboto, Helvetica Neue, sans-serif; font-size: 14px }'
  // };

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

  // Email template helper functions
  const getActiveTemplateTitle = () => {
    switch (activeEmailTemplate) {
      case 'ad-hoc':
        return 'Ad-Hoc Email';
      case 'confirmation':
        return 'Confirmation Email';
      case 'reminder-1day':
        return 'Reminder - 1 Day Before';
      case 'cancellation':
        return 'Cancellation Notice';
      default:
        return 'Unknown Template';
    }
  };

  const getTemplateSubject = () => {
    switch (activeEmailTemplate) {
      case 'confirmation':
        return 'Welcome to {event} - Registration Confirmed!';
      case 'reminder-1day':
        return 'Reminder: {event} starts tomorrow!';
      case 'cancellation':
        return 'Important: {event} has been cancelled';
      default:
        return '';
    }
  };

  const getTemplateContent = () => {
    switch (activeEmailTemplate) {
      case 'confirmation':
        return '<p><strong>Hi {name},</strong></p><p>Your registration for <strong>{event}</strong> has been confirmed!</p><p><strong>Event Details:</strong></p><ul><li><strong>Date:</strong> {date}</li><li><strong>Time:</strong> {time}</li><li><strong>Venue:</strong> {venue}</li><li><strong>Address:</strong> {venue_address}</li></ul><p>We\'re excited to see you there!</p><p>Best regards,<br>WitchCityRope Team</p>';
      case 'reminder-1day':
        return '<p>Hello {name},</p><p>This is a friendly reminder that <strong>{event}</strong> starts tomorrow!</p><p><strong>What to bring:</strong><br/>- Comfortable clothes<br/>- Water bottle<br/>- Positive attitude</p><p>See you there!</p>';
      case 'cancellation':
        return '<p>Dear {name},</p><p>We regret to inform you that <strong>{event}</strong> has been cancelled.</p><p><strong>Reason:</strong> [To be filled in when needed]</p><p>You will receive a full refund within 3-5 business days.</p><p>We apologize for any inconvenience.</p>';
      default:
        return '';
    }
  };

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
                <div style={{ marginBottom: 'var(--mantine-spacing-md)' }}>
                  <Text size="sm" fw={500} mb={5}>
                    Full Event Description <Text component="span" c="red">*</Text>
                  </Text>
                  <Text size="xs" c="dimmed" mb="xs">
                    This detailed description will be visible on the public events page
                  </Text>
                  <Textarea
                    rows={6}
                    value={form.values.fullDescription}
                    onChange={(event) => form.setFieldValue('fullDescription', event.currentTarget.value)}
                    placeholder="Enter detailed event description..."
                  />
                  {form.errors.fullDescription && (
                    <Text size="xs" c="red" mt={5}>
                      {form.errors.fullDescription}
                    </Text>
                  )}
                </div>

                {/* Policies & Procedures */}
                <div style={{ marginBottom: 'var(--mantine-spacing-md)' }}>
                  <Text size="sm" fw={500} mb={5}>
                    Policies & Procedures
                  </Text>
                  <Text size="xs" c="dimmed" mb="xs">
                    Studio-specific policies, prerequisites, safety requirements, etc. (managed by studio/admin, teachers cannot edit)
                  </Text>
                  <Textarea
                    rows={4}
                    value={form.values.policies}
                    onChange={(event) => form.setFieldValue('policies', event.currentTarget.value)}
                    placeholder="Enter studio policies, safety requirements, prerequisites, etc..."
                  />
                  {form.errors.policies && (
                    <Text size="xs" c="red" mt={5}>
                      {form.errors.policies}
                    </Text>
                  )}
                </div>
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

          {/* Emails Tab - EXACT WIREFRAME MATCH */}
          <Tabs.Panel value="emails" pt="xl">
            <Stack gap="xl">
              <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                Email Templates
              </Title>
              
              <Text size="sm" c="dimmed" mb="lg">
                Click on a template card to edit it below, or select "Send Ad-Hoc Email" to send one-time messages.
              </Text>
              
              {/* Template Cards Container - EXACT WIREFRAME MATCH */}
              <Group gap="md" style={{ flexWrap: 'wrap' }}>
                {/* Send Ad-Hoc Email Card - Always Present */}
                <Card
                  withBorder
                  p="md"
                  style={{
                    cursor: 'pointer',
                    borderColor: activeEmailTemplate === 'ad-hoc' ? 'var(--mantine-color-burgundy-6)' : 'var(--mantine-color-rose-3)',
                    backgroundColor: activeEmailTemplate === 'ad-hoc' ? 'rgba(136, 1, 36, 0.05)' : 'white',
                    minWidth: '220px',
                    flex: 1,
                    maxWidth: '300px',
                    position: 'relative',
                    transition: 'all 0.3s ease',
                  }}
                  onClick={() => setActiveEmailTemplate('ad-hoc')}
                >
                  <Text fw={600} c="burgundy" mb={4}>Send Ad-Hoc Email</Text>
                  <Text size="sm" c="stone" mb="xs">Send one-time messages to specific groups</Text>
                  <Text size="xs" c="dimmed" style={{ fontStyle: 'italic' }}>Any recipients</Text>
                </Card>

                {/* Confirmation Email Card - Required, cannot be removed */}
                <Card
                  withBorder
                  p="md"
                  style={{
                    cursor: 'pointer',
                    borderColor: activeEmailTemplate === 'confirmation' ? 'var(--mantine-color-burgundy-6)' : 'var(--mantine-color-rose-3)',
                    backgroundColor: activeEmailTemplate === 'confirmation' ? 'rgba(136, 1, 36, 0.05)' : 'white',
                    minWidth: '220px',
                    flex: 1,
                    maxWidth: '300px',
                    position: 'relative',
                    transition: 'all 0.3s ease',
                  }}
                  onClick={() => setActiveEmailTemplate('confirmation')}
                >
                  <ActionIcon
                    size="sm"
                    radius="xl"
                    color="red"
                    style={{ position: 'absolute', top: 8, right: 8, opacity: 0.5 }}
                    disabled
                    title="Required template"
                  >
                    ×
                  </ActionIcon>
                  <Text fw={600} c="burgundy" mb={4}>Confirmation Email</Text>
                  <Text size="sm" c="stone" mb="xs">Sent immediately when ticket is purchased</Text>
                  <Text size="xs" c="dimmed" style={{ fontStyle: 'italic' }}>All sessions</Text>
                </Card>

                {/* Reminder - 1 Day Before Card */}
                <Card
                  withBorder
                  p="md"
                  style={{
                    cursor: 'pointer',
                    borderColor: activeEmailTemplate === 'reminder-1day' ? 'var(--mantine-color-burgundy-6)' : 'var(--mantine-color-rose-3)',
                    backgroundColor: activeEmailTemplate === 'reminder-1day' ? 'rgba(136, 1, 36, 0.05)' : 'white',
                    minWidth: '220px',
                    flex: 1,
                    maxWidth: '300px',
                    position: 'relative',
                    transition: 'all 0.3s ease',
                  }}
                  onClick={() => setActiveEmailTemplate('reminder-1day')}
                >
                  <ActionIcon
                    size="sm"
                    radius="xl"
                    color="red"
                    style={{ position: 'absolute', top: 8, right: 8 }}
                    onClick={(e) => {
                      e.stopPropagation();
                      // Remove template logic
                    }}
                  >
                    ×
                  </ActionIcon>
                  <Text fw={600} c="burgundy" mb={4}>Reminder - 1 Day Before</Text>
                  <Text size="sm" c="stone" mb="xs">Sent 1 day before event start</Text>
                  <Text size="xs" c="dimmed" style={{ fontStyle: 'italic' }}>S1 attendees only</Text>
                </Card>

                {/* Cancellation Notice Card */}
                <Card
                  withBorder
                  p="md"
                  style={{
                    cursor: 'pointer',
                    borderColor: activeEmailTemplate === 'cancellation' ? 'var(--mantine-color-burgundy-6)' : 'var(--mantine-color-rose-3)',
                    backgroundColor: activeEmailTemplate === 'cancellation' ? 'rgba(136, 1, 36, 0.05)' : 'white',
                    minWidth: '220px',
                    flex: 1,
                    maxWidth: '300px',
                    position: 'relative',
                    transition: 'all 0.3s ease',
                  }}
                  onClick={() => setActiveEmailTemplate('cancellation')}
                >
                  <ActionIcon
                    size="sm"
                    radius="xl"
                    color="red"
                    style={{ position: 'absolute', top: 8, right: 8 }}
                    onClick={(e) => {
                      e.stopPropagation();
                      // Remove template logic
                    }}
                  >
                    ×
                  </ActionIcon>
                  <Text fw={600} c="burgundy" mb={4}>Cancellation Notice</Text>
                  <Text size="sm" c="stone" mb="xs">Sent when event is cancelled</Text>
                  <Text size="xs" c="dimmed" style={{ fontStyle: 'italic' }}>All sessions</Text>
                </Card>
              </Group>
              
              {/* Add Template Controls */}
              <Group align="end" gap="sm">
                <Select
                  label=""
                  placeholder="Select template to add..."
                  data={[
                    { value: 'reminder-1week', label: 'Reminder - 1 Week Before' },
                    { value: 'waitlist', label: 'Waitlist Notification' },
                    { value: 'survey', label: 'Post-Event Survey' },
                    { value: 'schedule-change', label: 'Schedule Change Alert' },
                  ]}
                  style={{ flex: 1 }}
                />
                <Button color="burgundy" disabled>Add Email Template</Button>
              </Group>

              {/* Unified Editor Section */}
              <div style={{ marginTop: 'var(--mantine-spacing-xl)' }}>
                <div style={{ marginBottom: 'var(--mantine-spacing-md)' }}>
                  <Text fw={600} c="burgundy">
                    Currently Editing: {getActiveTemplateTitle()}
                  </Text>
                </div>

                {/* Recipient Group (shown for ad-hoc) */}
                {activeEmailTemplate === 'ad-hoc' && (
                  <Select
                    label="Recipient Group"
                    data={[
                      { value: 'all-tickets', label: 'All Ticket Holders' },
                      { value: 'all-rsvps', label: 'All RSVPs' },
                      { value: 'volunteers', label: 'All Volunteers' },
                      { value: 'everyone', label: 'Everyone' },
                      { value: 'teachers', label: 'Teachers' },
                      { value: 'session-s1', label: 'S1 Attendees' },
                      { value: 'session-s2', label: 'S2 Attendees' },
                      { value: 'session-s3', label: 'S3 Attendees' },
                    ]}
                    mb="md"
                  />
                )}

                {/* Target Sessions (shown for templates) */}
                {activeEmailTemplate !== 'ad-hoc' && (
                  <MultiSelect
                    label="Target Sessions"
                    description="Which sessions should trigger this email? Hold Ctrl/Cmd for multiple selections."
                    data={[
                      { value: 'all', label: 'All Sessions' },
                      { value: 's1', label: 'S1' },
                      { value: 's2', label: 'S2' },
                      { value: 's3', label: 'S3' },
                    ]}
                    defaultValue={['all']}
                    mb="md"
                  />
                )}

                <TextInput
                  label="Subject Line"
                  value={getTemplateSubject()}
                  onChange={(event) => {
                    // Update subject logic
                  }}
                  mb="md"
                />

                <div>
                  <Text size="sm" fw={500} mb={5}>Email Content</Text>
                  <Text size="xs" c="dimmed" mb="xs">
                    Available variables: {'{name}'}, {'{event}'}, {'{date}'}, {'{time}'}, {'{venue}'}, {'{venue_address}'}
                  </Text>
                  <Editor
                    value={getTemplateContent()}
                    onEditorChange={(content) => {
                      // Update content logic - will be implemented when form state is connected
                      console.log('Content changed:', content);
                    }}
                    init={{
                      height: 300,
                      menubar: false,
                      plugins: 'advlist autolink lists link charmap preview anchor textcolor colorpicker',
                      toolbar: 'undo redo | blocks | bold italic underline strikethrough | link | bullist numlist | indent outdent | removeformat',
                      content_style: `
                        body { 
                          font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Helvetica Neue', Arial, sans-serif;
                          font-size: 14px;
                          color: #333;
                          line-height: 1.6;
                        }
                      `,
                      branding: false,
                    }}
                  />
                </div>

                <Group mt="md">
                  {activeEmailTemplate === 'ad-hoc' ? (
                    <Button color="burgundy">Send Email</Button>
                  ) : (
                    <Button color="burgundy">Save Changes</Button>
                  )}
                </Group>
              </div>
            </Stack>
          </Tabs.Panel>

          {/* Volunteers/Staff Tab - EXACT WIREFRAME MATCH */}
          <Tabs.Panel value="volunteers-staff" pt="xl">
            <Stack gap="xl">
              {/* Volunteer Positions - EXACT TABLE STRUCTURE FROM WIREFRAME */}
              <div>
                <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                  Volunteer Positions
                </Title>
                
                <Table
                  striped
                  highlightOnHover
                  style={{
                    backgroundColor: 'white',
                    borderRadius: '12px',
                    overflow: 'hidden',
                    boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
                  }}
                >
                  <Table.Thead style={{ backgroundColor: 'var(--mantine-color-burgundy-6)' }}>
                    <Table.Tr>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Edit
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Position
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Sessions
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Time
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Description
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Needed
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Assigned
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Delete
                      </Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {/* Door Monitor Row */}
                    <Table.Tr>
                      <Table.Td>
                        <ActionIcon size="sm" color="burgundy" variant="light">
                          ✏️
                        </ActionIcon>
                      </Table.Td>
                      <Table.Td>
                        <Text fw={600} c="burgundy">Door Monitor</Text>
                      </Table.Td>
                      <Table.Td>
                        <Text fw={600}>S1, S2</Text>
                      </Table.Td>
                      <Table.Td>
                        <Text size="sm" c="stone">6:00 PM - 7:00 PM</Text>
                      </Table.Td>
                      <Table.Td>Check IDs, welcome members</Table.Td>
                      <Table.Td style={{ textAlign: 'center', fontWeight: 600 }}>2</Table.Td>
                      <Table.Td>
                        <div>
                          <Text size="sm" c="green">✓ Jamie Davis</Text>
                          <Text size="sm" c="yellow">○ 1 more needed</Text>
                        </div>
                      </Table.Td>
                      <Table.Td>
                        <ActionIcon size="sm" color="red">
                          🗑️
                        </ActionIcon>
                      </Table.Td>
                    </Table.Tr>

                    {/* Safety Monitor Row */}
                    <Table.Tr>
                      <Table.Td>
                        <ActionIcon size="sm" color="burgundy" variant="light">
                          ✏️
                        </ActionIcon>
                      </Table.Td>
                      <Table.Td>
                        <Text fw={600} c="burgundy">Safety Monitor</Text>
                      </Table.Td>
                      <Table.Td>
                        <Text fw={600}>All</Text>
                      </Table.Td>
                      <Table.Td>
                        <Text size="sm" c="stone">7:00 PM - 10:00 PM</Text>
                      </Table.Td>
                      <Table.Td>Monitor play areas, handle incidents</Table.Td>
                      <Table.Td style={{ textAlign: 'center', fontWeight: 600 }}>3</Table.Td>
                      <Table.Td>
                        <div>
                          <Text size="sm" c="green">✓ Sam Singh</Text>
                          <Text size="sm" c="green">✓ Alex Chen</Text>
                          <Text size="sm" c="yellow">○ 1 more needed</Text>
                        </div>
                      </Table.Td>
                      <Table.Td>
                        <ActionIcon size="sm" color="red">
                          🗑️
                        </ActionIcon>
                      </Table.Td>
                    </Table.Tr>

                    {/* Setup Crew Row */}
                    <Table.Tr>
                      <Table.Td>
                        <ActionIcon size="sm" color="burgundy" variant="light">
                          ✏️
                        </ActionIcon>
                      </Table.Td>
                      <Table.Td>
                        <Text fw={600} c="burgundy">Setup Crew</Text>
                      </Table.Td>
                      <Table.Td>
                        <Text fw={600}>S1</Text>
                      </Table.Td>
                      <Table.Td>
                        <Text size="sm" c="stone">6:00 PM - 7:00 PM</Text>
                      </Table.Td>
                      <Table.Td>Arrange furniture, setup equipment</Table.Td>
                      <Table.Td style={{ textAlign: 'center', fontWeight: 600 }}>4</Table.Td>
                      <Table.Td>
                        <Text size="sm" c="red">⚠ 4 positions open</Text>
                      </Table.Td>
                      <Table.Td>
                        <ActionIcon size="sm" color="red">
                          🗑️
                        </ActionIcon>
                      </Table.Td>
                    </Table.Tr>
                  </Table.Tbody>
                </Table>

                {/* Add Position Button - Below Table as per wireframe */}
                <Button
                  mt="md"
                  style={{
                    background: 'linear-gradient(135deg, var(--mantine-color-amber-6), #DAA520)',
                    border: 'none',
                    color: 'var(--mantine-color-dark-9)',
                    borderRadius: '12px 6px 12px 6px',
                    fontWeight: 600,
                  }}
                >
                  Add Position
                </Button>
              </div>

              {/* Staff Assignments Section - Below Volunteer Positions */}
              <div>
                <Title order={3} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                  Staff Assignments
                </Title>
                
                <Text size="sm" c="dimmed" mb="md">
                  Staff assignments for the volunteer positions above
                </Text>

                {/* Staff assignment content would go here - this section is less detailed in the wireframe */}
                <Card withBorder p="md" style={{ backgroundColor: 'rgba(250, 246, 242, 0.5)' }}>
                  <Text c="dimmed" ta="center" style={{ fontStyle: 'italic' }}>
                    Staff assignment interface would be implemented here based on the volunteer positions defined above.
                  </Text>
                </Card>
              </div>
            </Stack>
          </Tabs.Panel>
        </Tabs>
      </form>
    </Card>
  );
};