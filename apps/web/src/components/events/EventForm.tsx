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
import { SessionFormModal } from './SessionFormModal';
import { TicketTypeFormModal, EventTicketType as ModalTicketType } from './TicketTypeFormModal';

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

  // Modal state management
  const [sessionModalOpen, setSessionModalOpen] = useState(false);
  const [ticketModalOpen, setTicketModalOpen] = useState(false);
  const [editingSession, setEditingSession] = useState<EventSession | null>(null);
  const [editingTicketType, setEditingTicketType] = useState<EventTicketType | null>(null);

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
    const session = form.values.sessions.find(s => s.id === sessionId);
    if (session) {
      setEditingSession(session);
      setSessionModalOpen(true);
    }
  };

  const handleDeleteSession = (sessionId: string) => {
    const updatedSessions = form.values.sessions.filter(session => session.id !== sessionId);
    form.setFieldValue('sessions', updatedSessions);
  };

  const handleAddSession = () => {
    setEditingSession(null);
    setSessionModalOpen(true);
  };

  const handleSessionSubmit = (sessionData: Omit<EventSession, 'id'>) => {
    if (editingSession) {
      // Update existing session
      const updatedSessions = form.values.sessions.map(session =>
        session.id === editingSession.id
          ? { ...sessionData, id: editingSession.id }
          : session
      );
      form.setFieldValue('sessions', updatedSessions);
    } else {
      // Add new session
      const newSession: EventSession = {
        ...sessionData,
        id: crypto.randomUUID()
      };
      form.setFieldValue('sessions', [...form.values.sessions, newSession]);
    }
  };

  // Ticket type management handlers
  const handleEditTicketType = (ticketTypeId: string) => {
    const ticketType = form.values.ticketTypes.find(t => t.id === ticketTypeId);
    if (ticketType) {
      setEditingTicketType(ticketType);
      setTicketModalOpen(true);
    }
  };

  const handleDeleteTicketType = (ticketTypeId: string) => {
    const updatedTicketTypes = form.values.ticketTypes.filter(ticket => ticket.id !== ticketTypeId);
    form.setFieldValue('ticketTypes', updatedTicketTypes);
  };

  const handleAddTicketType = () => {
    setEditingTicketType(null);
    setTicketModalOpen(true);
  };

  const handleTicketTypeSubmit = (ticketTypeData: Omit<ModalTicketType, 'id'>) => {
    // Convert from modal format to grid format
    const gridFormatTicketType: Omit<EventTicketType, 'id'> = {
      name: ticketTypeData.name,
      type: 'Single', // Default, could be enhanced to support couples later
      sessionIdentifiers: ticketTypeData.sessionsIncluded,
      minPrice: ticketTypeData.price,
      maxPrice: ticketTypeData.price,
      quantityAvailable: ticketTypeData.quantityAvailable,
      salesEndDate: undefined, // Could be enhanced later
    };

    if (editingTicketType) {
      // Update existing ticket type
      const updatedTicketTypes = form.values.ticketTypes.map(ticketType =>
        ticketType.id === editingTicketType.id
          ? { ...gridFormatTicketType, id: editingTicketType.id }
          : ticketType
      );
      form.setFieldValue('ticketTypes', updatedTicketTypes);
    } else {
      // Add new ticket type
      const newTicketType: EventTicketType = {
        ...gridFormatTicketType,
        id: crypto.randomUUID()
      };
      form.setFieldValue('ticketTypes', [...form.values.ticketTypes, newTicketType]);
    }
  };

  // Convert grid format to modal format for editing
  const convertTicketTypeForModal = (ticketType: EventTicketType): ModalTicketType => {
    return {
      id: ticketType.id,
      name: ticketType.name,
      description: '', // Not stored in grid format currently
      price: ticketType.minPrice,
      sessionsIncluded: ticketType.sessionIdentifiers,
      quantityAvailable: ticketType.quantityAvailable || 100,
      quantitySold: 0, // Not tracked in current grid format
      allowMultiplePurchase: true, // Default value
      isEarlyBird: false, // Default value
      earlyBirdDiscount: undefined,
    };
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
    <Card shadow="md" radius="lg" p="xl" style={{ backgroundColor: 'white' }} data-testid="event-form">
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
            <Tabs.Tab value="volunteers">Volunteers</Tabs.Tab>
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
                  <Editor
                    apiKey="3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp"
                    value={form.values.fullDescription}
                    onEditorChange={(content) => form.setFieldValue('fullDescription', content)}
                    init={{
                      height: 300,
                      menubar: false,
                      plugins: 'advlist autolink lists link charmap preview anchor',
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
                  <Editor
                    apiKey="3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp"
                    value={form.values.policies}
                    onEditorChange={(content) => form.setFieldValue('policies', content)}
                    init={{
                      height: 150,
                      menubar: false,
                      plugins: 'advlist autolink lists link charmap preview anchor',
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
                  <Button variant="outline" color="burgundy">
                    Add Venue
                  </Button>
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
                <Button variant="outline" color="burgundy" onClick={onCancel} style={{ minWidth: '120px', height: '48px', fontWeight: 600 }}>
                  Cancel
                </Button>
                <Button
                  type="submit"
                  loading={isSubmitting}
                  style={{
                    background: 'linear-gradient(135deg, var(--mantine-color-amber-6), #DAA520)',
                    border: 'none',
                    color: 'var(--mantine-color-dark-9)',
                    borderRadius: '12px 6px 12px 6px',
                    fontWeight: 600,
                    minWidth: '140px',
                    height: '48px',
                  }}
                >
                  {isSubmitting ? 'Saving...' : 'Save Draft'}
                </Button>
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
                <Button color="burgundy" disabled style={{
                  minWidth: '160px',
                  height: '48px',
                  fontWeight: 600,
                }}>Add Email Template</Button>
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
                    apiKey="3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp"
                    value={getTemplateContent()}
                    onEditorChange={(content) => {
                      // Update content logic - will be implemented when form state is connected
                      console.log('Content changed:', content);
                    }}
                    init={{
                      height: 300,
                      menubar: false,
                      plugins: 'advlist autolink lists link charmap preview anchor',
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
                    <Button color="burgundy" style={{
                      minWidth: '120px',
                      height: '48px',
                      fontWeight: 600,
                    }}>Send Email</Button>
                  ) : (
                    <Button color="burgundy" style={{
                      minWidth: '140px',
                      height: '48px',
                      fontWeight: 600,
                    }}>Save Changes</Button>
                  )}
                </Group>
              </div>
            </Stack>
          </Tabs.Panel>

          {/* Volunteers Tab - EXACT WIREFRAME MATCH */}
          <Tabs.Panel value="volunteers" pt="xl">
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

              {/* Volunteer Position Edit Form - From Wireframe */}
              <div>
                <Title order={3} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                  Volunteer Position Management
                </Title>
                
                <Text size="sm" c="dimmed" mb="md">
                  Define volunteer positions and manage assignments
                </Text>

                {/* Add New Position Form */}
                <Card withBorder p="md" style={{ backgroundColor: 'white', marginBottom: 'var(--mantine-spacing-md)' }}>
                  <Title order={4} c="burgundy" mb="md">Add New Position</Title>
                  
                  <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr 1fr 1fr', gap: 'var(--mantine-spacing-sm)', marginBottom: 'var(--mantine-spacing-md)', alignItems: 'end' }}>
                    <TextInput
                      label="Position Name"
                      placeholder="Enter position name"
                    />
                    <Select
                      label="Session"
                      placeholder="Select session..."
                      data={[
                        { value: 's1', label: 'S1' },
                        { value: 's2', label: 'S2' },
                        { value: 's3', label: 'S3' },
                        { value: 'all', label: 'All Sessions' },
                      ]}
                    />
                    <TextInput
                      type="time"
                      label="Start Time"
                      placeholder="Start time"
                    />
                    <TextInput
                      type="time"
                      label="End Time"
                      placeholder="End time"
                    />
                  </div>
                  
                  <Textarea
                    label="Description"
                    placeholder="Describe the volunteer duties..."
                    minRows={3}
                    mb="md"
                  />
                  
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'end', gap: 'var(--mantine-spacing-md)' }}>
                    <TextInput
                      type="number"
                      label="Number Needed"
                      placeholder="1"
                      min={1}
                      style={{ width: '120px' }}
                    />
                    <Button
                      style={{
                        background: 'linear-gradient(135deg, var(--mantine-color-amber-6), #DAA520)',
                        border: 'none',
                        color: 'var(--mantine-color-dark-9)',
                        borderRadius: '12px 6px 12px 6px',
                        fontWeight: 600,
                        minWidth: '140px',
                        height: '48px',
                      }}
                    >
                      Add Position
                    </Button>
                  </div>
                </Card>
              </div>
            </Stack>
          </Tabs.Panel>
        </Tabs>
      </form>

      {/* Session Form Modal */}
      <SessionFormModal
        opened={sessionModalOpen}
        onClose={() => {
          setSessionModalOpen(false);
          setEditingSession(null);
        }}
        onSubmit={handleSessionSubmit}
        session={editingSession}
        existingSessions={form.values.sessions}
      />

      {/* Ticket Type Form Modal */}
      <TicketTypeFormModal
        opened={ticketModalOpen}
        onClose={() => {
          setTicketModalOpen(false);
          setEditingTicketType(null);
        }}
        onSubmit={handleTicketTypeSubmit}
        ticketType={editingTicketType ? convertTicketTypeForModal(editingTicketType) : null}
        availableSessions={form.values.sessions}
      />
    </Card>
  );
};