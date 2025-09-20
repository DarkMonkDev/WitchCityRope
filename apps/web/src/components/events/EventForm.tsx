import React, { useState, useEffect, useRef } from 'react';
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
  Badge,
  Table,
  ActionIcon,
  Switch,
  Alert,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { Editor } from '@tinymce/tinymce-react';

import { EventSessionsGrid, EventSession } from './EventSessionsGrid';
import { EventTicketTypesGrid, EventTicketType } from './EventTicketTypesGrid';
import { SessionFormModal } from './SessionFormModal';
import { TicketTypeFormModal, EventTicketType as ModalTicketType } from './TicketTypeFormModal';
import { VolunteerPositionsGrid } from './VolunteerPositionsGrid';
import { VolunteerPositionFormModal, VolunteerPosition } from './VolunteerPositionFormModal';
import { WCRButton } from '../ui';
import { useTeachers, formatTeachersForMultiSelect } from '../../lib/api/hooks/useTeachers';
import { useEventParticipations, type EventParticipationDto } from '../../lib/api/hooks/useEventParticipations';

export interface EventFormData {
  // Basic Info
  eventType: 'class' | 'social';
  title: string;
  shortDescription: string;
  fullDescription: string;
  policies: string;
  venueId: string;
  teacherIds: string[];
  
  // Status
  status: 'Draft' | 'Published' | 'Cancelled' | 'Completed';
  
  // Sessions and Tickets
  sessions: EventSession[];
  ticketTypes: EventTicketType[];
  
  // Volunteer Positions
  volunteerPositions: VolunteerPosition[];
}

interface EventFormProps {
  initialData?: Partial<EventFormData>;
  onSubmit: (data: EventFormData) => void;
  onCancel: () => void;
  isSubmitting?: boolean;
  onFormChange?: () => void;
  formDirty?: boolean;
  eventId?: string; // For fetching participation data
}

export const EventForm: React.FC<EventFormProps> = ({
  initialData,
  onSubmit,
  onCancel,
  isSubmitting = false,
  onFormChange,
  formDirty = false,
  eventId,
}) => {
  const [activeTab, setActiveTab] = useState<string>('basic-info');
  const [activeEmailTemplate, setActiveEmailTemplate] = useState<string>('confirmation');

  // TinyMCE configuration - only use if API key is available
  const tinyMCEApiKey = import.meta.env.VITE_TINYMCE_API_KEY;
  const shouldUseTinyMCE = !!tinyMCEApiKey;

  // Fetch teachers from API
  const { data: teachersData, isLoading: teachersLoading, error: teachersError } = useTeachers();

  // Fetch event participations for admin view (only if eventId provided)
  const { data: participationsData, isLoading: participationsLoading, error: participationsError } = useEventParticipations(eventId || '', !!eventId);

  // Smart Rich Text Editor component - uses TinyMCE if API key available, otherwise Textarea
  const RichTextEditor: React.FC<{
    value: string;
    onChange: (content: string) => void;
    height?: number;
    placeholder?: string;
  }> = ({ value, onChange, height = 300, placeholder }) => {
    if (!shouldUseTinyMCE) {
      return (
        <>
          <Alert color="blue" mb="xs" title="Development Mode">
            TinyMCE disabled to prevent API usage costs. Using simple text editor.
          </Alert>
          <Textarea
            value={value}
            onChange={(event) => onChange(event.target.value)}
            minRows={height / 20} // Approximate rows based on height
            placeholder={placeholder}
            autosize
            styles={{
              input: {
                fontFamily: '-apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Helvetica Neue, sans-serif',
                fontSize: '14px',
                lineHeight: '1.6'
              }
            }}
          />
        </>
      );
    }

    return (
      <Editor
        apiKey={tinyMCEApiKey}
        value={value}
        onEditorChange={onChange}
        init={{
          height,
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
    );
  };

  // Modal state management
  const [sessionModalOpen, setSessionModalOpen] = useState(false);
  const [ticketModalOpen, setTicketModalOpen] = useState(false);
  const [volunteerModalOpen, setVolunteerModalOpen] = useState(false);
  const [editingSession, setEditingSession] = useState<EventSession | null>(null);
  const [editingTicketType, setEditingTicketType] = useState<EventTicketType | null>(null);
  const [editingVolunteerPosition, setEditingVolunteerPosition] = useState<VolunteerPosition | null>(null);

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
      status: 'Draft',
      sessions: [],
      ticketTypes: [],
      volunteerPositions: [],
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

  // Update form values when initialData changes (for loading from API)
  // Use a ref to track if we've already initialized to prevent overriding user changes
  const hasInitialized = useRef(false);

  useEffect(() => {
    if (initialData && Object.keys(initialData).length > 0 && !hasInitialized.current) {
      form.setValues({
        eventType: 'class',
        title: '',
        shortDescription: '',
        fullDescription: '',
        policies: '',
        venueId: '',
        teacherIds: [],
        status: 'Draft',
        sessions: [],
        ticketTypes: [],
        volunteerPositions: [],
        ...initialData,
      });
      hasInitialized.current = true;
    }
  }, [initialData]);
  
  // Track form changes
  const previousValues = useRef(form.values);
  const onFormChangeRef = useRef(onFormChange);
  
  // Update the ref when the callback changes
  useEffect(() => {
    onFormChangeRef.current = onFormChange;
  }, [onFormChange]);
  
  useEffect(() => {
    // Compare current values with previous values to detect changes
    if (JSON.stringify(form.values) !== JSON.stringify(previousValues.current)) {
      if (onFormChangeRef.current) {
        onFormChangeRef.current();
      }
      previousValues.current = form.values;
    }
  }, [form.values]); // Remove onFormChange from dependency array to prevent loops

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

  // Format teachers for MultiSelect (with fallback to empty array)
  const availableTeachers = (teachersData && Array.isArray(teachersData)) ? formatTeachersForMultiSelect(teachersData) : [];

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
      salesEndDate: ticketTypeData.saleEndDate?.toISOString(),
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
      saleEndDate: ticketType.salesEndDate ? new Date(ticketType.salesEndDate) : undefined,
    };
  };

  // Volunteer position management handlers
  const handleEditVolunteerPosition = (positionId: string) => {
    const position = form.values.volunteerPositions.find(p => p.id === positionId);
    if (position) {
      setEditingVolunteerPosition(position);
      setVolunteerModalOpen(true);
    }
  };

  const handleDeleteVolunteerPosition = (positionId: string) => {
    const updatedPositions = form.values.volunteerPositions.filter(position => position.id !== positionId);
    form.setFieldValue('volunteerPositions', updatedPositions);
  };

  const handleAddVolunteerPosition = () => {
    setEditingVolunteerPosition(null);
    setVolunteerModalOpen(true);
  };

  const handleVolunteerPositionSubmit = (positionData: Omit<VolunteerPosition, 'id' | 'slotsFilled'>) => {
    if (editingVolunteerPosition) {
      // Update existing position
      const updatedPositions = form.values.volunteerPositions.map(position =>
        position.id === editingVolunteerPosition.id
          ? { ...positionData, id: editingVolunteerPosition.id, slotsFilled: editingVolunteerPosition.slotsFilled }
          : position
      );
      form.setFieldValue('volunteerPositions', updatedPositions);
    } else {
      // Add new position
      const newPosition: VolunteerPosition = {
        ...positionData,
        id: crypto.randomUUID(),
        slotsFilled: 0, // Start with no volunteers filled
      };
      form.setFieldValue('volunteerPositions', [...form.values.volunteerPositions, newPosition]);
    }
  };

  const handleSubmit = form.onSubmit((values) => {
    // DEBUG: Log form values when submitted
    console.log('🔍 [DEBUG] EventForm submitting values:', {
      teacherIds: values.teacherIds,
      formKeys: Object.keys(values),
      fullValues: values
    });
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
        <Tabs value={activeTab} onChange={setActiveTab} variant="pills" radius="md" data-testid="tabs-event-management">
          <Tabs.List
            style={{
              backgroundColor: 'var(--mantine-color-gray-0)',
              borderBottom: '2px solid var(--mantine-color-burgundy-3)',
              padding: 'var(--mantine-spacing-md)',
            }}
          >
            <Tabs.Tab value="basic-info" data-testid="tab-basic-info">Basic Info</Tabs.Tab>
            <Tabs.Tab value="setup" data-testid="setup-tab">Setup</Tabs.Tab>
            <Tabs.Tab value="emails" data-testid="tab-emails">Emails</Tabs.Tab>
            <Tabs.Tab value="volunteers" data-testid="tab-volunteers">Volunteers</Tabs.Tab>
            <Tabs.Tab value="rsvp-tickets" data-testid="rsvp-tickets-tab">RSVP/Tickets</Tabs.Tab>
            <Tabs.Tab value="attendees" data-testid="attendees-tab">Attendees</Tabs.Tab>
          </Tabs.List>

          {/* Basic Info Tab */}
          <Tabs.Panel value="basic-info" pt="xl" data-testid="panel-basic-info">
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
                  <RichTextEditor
                    value={form.values.fullDescription}
                    onChange={(content) => form.setFieldValue('fullDescription', content)}
                    height={300}
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
                  <RichTextEditor
                    value={form.values.policies}
                    onChange={(content) => form.setFieldValue('policies', content)}
                    height={150}
                    placeholder="Enter policies and procedures..."
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
                  <WCRButton variant="outline" size="md">
                    Add Venue
                  </WCRButton>
                </Group>
              </div>

              {/* Teachers/Instructors Section */}
              <div>
                <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                  Teachers/Instructors
                </Title>
                {/* DEBUG: Log teacher selection data */}
                {/* {console.log('🔍 [DEBUG] Teacher selection data:', {
                  teachersData,
                  teachersLoading,
                  teachersError,
                  availableTeachers,
                  currentTeacherIds: form.values.teacherIds,
                  teacherInputProps: form.getInputProps('teacherIds')
                })} */}

                {teachersError && (
                  <Alert color="red" mb="md" title="Error Loading Teachers">
                    Failed to load teachers list. Using form without teacher selection.
                  </Alert>
                )}

                <MultiSelect
                  label="Select Teachers"
                  placeholder={teachersLoading ? "Loading teachers..." : "Choose teachers for this event"}
                  data={availableTeachers}
                  searchable
                  disabled={teachersLoading || !!teachersError}
                  {...form.getInputProps('teacherIds')}
                  onChange={(value) => {
                    console.log('🔍 [DEBUG] MultiSelect onChange called with:', value);
                    console.log('🔍 [DEBUG] Value type:', typeof value, Array.isArray(value) ? 'array' : 'not array');
                    form.setFieldValue('teacherIds', value);
                    console.log('🔍 [DEBUG] Form teacherIds after set:', form.values.teacherIds);
                  }}
                />
                {/* DEBUG: Show current teacherIds and API status */}
                <Text size="xs" c="dimmed" mt="xs">
                  Debug - Current teacherIds: {JSON.stringify(form.values.teacherIds)}
                  <br />
                  Teachers loading: {teachersLoading ? 'Yes' : 'No'}
                  <br />
                  Teachers available: {availableTeachers.length}
                  <br />
                  Teachers error: {teachersError ? String(teachersError) : 'None'}
                </Text>
              </div>

              {/* Save Buttons */}
              <Group justify="flex-end" mt="xl">
                <WCRButton 
                  variant="outline" 
                  onClick={onCancel}
                  size="lg"
                >
                  Cancel
                </WCRButton>
                <WCRButton
                  type="submit"
                  loading={isSubmitting}
                  variant="secondary"
                  size="lg"
                  disabled={!formDirty}
                >
                  {isSubmitting ? 'Saving...' : 'Save'}
                </WCRButton>
              </Group>
            </Stack>
          </Tabs.Panel>

          {/* Setup Tab - Combined Sessions and Tickets */}
          <Tabs.Panel value="setup" pt="xl" data-testid="setup-tab">
            <Stack gap="xl">
              {/* Event Sessions Section */}
              <div data-testid="sessions-section">
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

              {/* Ticket Types Section */}
              <div data-testid="tickets-section">
                <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                  Ticket Types
                </Title>
                <EventTicketTypesGrid
                  ticketTypes={form.values.ticketTypes}
                  onEditTicketType={handleEditTicketType}
                  onDeleteTicketType={handleDeleteTicketType}
                  onAddTicketType={handleAddTicketType}
                  hasSessions={form.values.sessions.length > 0}
                />
              </div>
            </Stack>
          </Tabs.Panel>

          {/* Emails Tab - EXACT WIREFRAME MATCH */}
          <Tabs.Panel value="emails" pt="xl" data-testid="panel-emails">
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
                <WCRButton variant="primary" size="lg" disabled>
                  Add Email Template
                </WCRButton>
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
                  <RichTextEditor
                    value={getTemplateContent()}
                    onChange={(content) => {
                      // Update content logic - will be implemented when form state is connected
                      console.log('Content changed:', content);
                    }}
                    height={300}
                    placeholder="Enter email content..."
                  />
                </div>

                <Group mt="md">
                  {activeEmailTemplate === 'ad-hoc' ? (
                    <WCRButton variant="primary" size="lg">
                      Send Email
                    </WCRButton>
                  ) : (
                    <WCRButton variant="primary" size="lg">
                      Save Changes
                    </WCRButton>
                  )}
                </Group>
              </div>
            </Stack>
          </Tabs.Panel>

          {/* Volunteers Tab - Modal-based consistent with other tabs */}
          <Tabs.Panel value="volunteers" pt="xl" data-testid="panel-volunteers">
            <Stack gap="xl">
              {/* Volunteer Positions */}
              <div>
                <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                  Volunteer Positions
                </Title>
                <VolunteerPositionsGrid
                  positions={form.values.volunteerPositions}
                  onEditPosition={handleEditVolunteerPosition}
                  onDeletePosition={handleDeleteVolunteerPosition}
                  onAddPosition={handleAddVolunteerPosition}
                />
              </div>
            </Stack>
          </Tabs.Panel>

          {/* RSVP/Tickets Tab - Updated per requirements */}
          <Tabs.Panel value="rsvp-tickets" pt="xl" data-testid="rsvp-tickets-tab">
            <Stack gap="xl">
              {/* RSVPs Table - Hidden for CLASS events */}
              {form.values.eventType === 'social' && (
                <div data-testid="rsvps-section">
                  <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                    RSVPs Management
                  </Title>
                  <Text size="sm" c="dimmed" mb="lg">
                    View and manage all RSVPs for this social event.
                  </Text>
                  
                  <Table
                    striped
                    highlightOnHover
                    withTableBorder
                    data-testid="rsvps-table"
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
                          Name
                        </Table.Th>
                        <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                          Email
                        </Table.Th>
                        <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                          Status
                        </Table.Th>
                        <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                          RSVP Date
                        </Table.Th>
                        <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                          Actions
                        </Table.Th>
                      </Table.Tr>
                    </Table.Thead>
                    <Table.Tbody>
                      {participationsLoading ? (
                        <Table.Tr>
                          <Table.Td colSpan={5}>
                            <Text ta="center" c="dimmed" py="xl">
                              Loading RSVPs...
                            </Text>
                          </Table.Td>
                        </Table.Tr>
                      ) : participationsError ? (
                        <Table.Tr>
                          <Table.Td colSpan={5}>
                            <Text ta="center" c="red" py="xl">
                              Error loading RSVPs: {participationsError.message}
                            </Text>
                          </Table.Td>
                        </Table.Tr>
                      ) : participationsData && (participationsData as EventParticipationDto[]).length > 0 ? (
                        (participationsData as EventParticipationDto[])
                          .filter(p => p.participationType === 'RSVP')
                          .map((participation) => (
                            <Table.Tr key={participation.id}>
                              <Table.Td>
                                <Text fw={500}>{participation.userSceneName}</Text>
                              </Table.Td>
                              <Table.Td>
                                <Text size="sm" c="dimmed">{participation.userEmail}</Text>
                              </Table.Td>
                              <Table.Td>
                                <Badge
                                  color={participation.status === 'Active' ? 'green' : 'red'}
                                  variant="light"
                                >
                                  {participation.status}
                                </Badge>
                              </Table.Td>
                              <Table.Td>
                                <Text size="sm">
                                  {new Date(participation.participationDate).toLocaleDateString()}
                                </Text>
                              </Table.Td>
                              <Table.Td>
                                <Group gap="xs">
                                  <ActionIcon
                                    size="sm"
                                    variant="light"
                                    color="blue"
                                    title="View Details"
                                  >
                                    👁️
                                  </ActionIcon>
                                  {participation.canCancel && (
                                    <ActionIcon
                                      size="sm"
                                      variant="light"
                                      color="red"
                                      title="Cancel RSVP"
                                    >
                                      ❌
                                    </ActionIcon>
                                  )}
                                </Group>
                              </Table.Td>
                            </Table.Tr>
                          ))
                      ) : (
                        <Table.Tr>
                          <Table.Td colSpan={5}>
                            <Text ta="center" c="dimmed" py="xl">
                              No RSVPs yet. RSVPs will appear here once people respond to invitations.
                            </Text>
                          </Table.Td>
                        </Table.Tr>
                      )}
                    </Table.Tbody>
                  </Table>
                </div>
              )}

              {/* Tickets Sold Table */}
              <div data-testid="tickets-sold-section">
                <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                  Tickets Sold
                </Title>
                <Text size="sm" c="dimmed" mb="lg">
                  View all sold tickets for this event.
                </Text>
                
                <Table
                  striped
                  highlightOnHover
                  withTableBorder
                  data-testid="tickets-sold-table"
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
                        Ticket Holder
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Ticket Type
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Sessions
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Purchase Date
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Amount Paid
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Actions
                      </Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {participationsLoading ? (
                      <Table.Tr>
                        <Table.Td colSpan={6}>
                          <Text ta="center" c="dimmed" py="xl">
                            Loading tickets...
                          </Text>
                        </Table.Td>
                      </Table.Tr>
                    ) : participationsError ? (
                      <Table.Tr>
                        <Table.Td colSpan={6}>
                          <Text ta="center" c="red" py="xl">
                            Error loading tickets: {participationsError.message}
                          </Text>
                        </Table.Td>
                      </Table.Tr>
                    ) : participationsData && (participationsData as EventParticipationDto[]).length > 0 ? (
                      (participationsData as EventParticipationDto[])
                        .filter(p => p.participationType === 'Ticket')
                        .map((participation) => (
                          <Table.Tr key={participation.id}>
                            <Table.Td>
                              <Text fw={500}>{participation.userSceneName}</Text>
                            </Table.Td>
                            <Table.Td>
                              <Text size="sm">Standard Ticket</Text>
                            </Table.Td>
                            <Table.Td>
                              <Text size="sm">All Sessions</Text>
                            </Table.Td>
                            <Table.Td>
                              <Text size="sm">
                                {new Date(participation.participationDate).toLocaleDateString()}
                              </Text>
                            </Table.Td>
                            <Table.Td>
                              <Text size="sm" fw={500}>$50.00</Text>
                            </Table.Td>
                            <Table.Td>
                              <Group gap="xs">
                                <ActionIcon
                                  size="sm"
                                  variant="light"
                                  color="blue"
                                  title="View Details"
                                >
                                  👁️
                                </ActionIcon>
                                {participation.canCancel && (
                                  <ActionIcon
                                    size="sm"
                                    variant="light"
                                    color="red"
                                    title="Refund Ticket"
                                  >
                                    💰
                                  </ActionIcon>
                                )}
                              </Group>
                            </Table.Td>
                          </Table.Tr>
                        ))
                    ) : (
                      <Table.Tr>
                        <Table.Td colSpan={6}>
                          <Text ta="center" c="dimmed" py="xl">
                            No tickets sold yet. Ticket purchases will appear here once people buy tickets.
                          </Text>
                        </Table.Td>
                      </Table.Tr>
                    )}
                  </Table.Tbody>
                </Table>
              </div>
            </Stack>
          </Tabs.Panel>

          {/* Attendees Tab - New tab for people who actually attended */}
          <Tabs.Panel value="attendees" pt="xl" data-testid="attendees-tab">
            <Stack gap="xl">
              {/* Attendees List */}
              <div data-testid="attendees-list">
                <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                  Event Attendees
                </Title>
                <Text size="sm" c="dimmed" mb="lg">
                  View and manage people who actually attended (checked in) to this event.
                </Text>
                
                <Table
                  striped
                  highlightOnHover
                  withTableBorder
                  data-testid="attendees-table"
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
                        Name
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Type
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Session(s)
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Check-in Time
                      </Table.Th>
                      <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
                        Actions
                      </Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {/* Empty state for now - will be populated with real attendee data */}
                    <Table.Tr>
                      <Table.Td colSpan={5}>
                        <Text ta="center" c="dimmed" py="xl">
                          No attendees checked in yet. Attendee check-ins will appear here during the event.
                        </Text>
                      </Table.Td>
                    </Table.Tr>
                  </Table.Tbody>
                </Table>
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
        existingSessions={form.values.sessions || []}
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
        availableSessions={form.values.sessions || []}
      />

      {/* Volunteer Position Form Modal */}
      <VolunteerPositionFormModal
        opened={volunteerModalOpen}
        onClose={() => {
          setVolunteerModalOpen(false);
          setEditingVolunteerPosition(null);
        }}
        onSubmit={handleVolunteerPositionSubmit}
        position={editingVolunteerPosition}
        availableSessions={form.values.sessions || []}
      />
    </Card>
  );
};