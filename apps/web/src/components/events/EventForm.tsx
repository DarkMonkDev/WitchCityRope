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
import { RichTextEditor, Link } from '@mantine/tiptap';
import { useEditor } from '@tiptap/react';
import Highlight from '@tiptap/extension-highlight';
import StarterKit from '@tiptap/starter-kit';
import Underline from '@tiptap/extension-underline';
import TextAlign from '@tiptap/extension-text-align';
import Superscript from '@tiptap/extension-superscript';
import SubScript from '@tiptap/extension-subscript';

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

  // Rich text editor configurations
  const editorExtensions = [
    StarterKit.configure({
      // StarterKit includes basic formatting but not Link or Underline
      // No need to disable anything, just add what we need
    }),
    Underline, // StarterKit doesn't include underline, so safe to add
    Link.configure({
      openOnClick: false,
      HTMLAttributes: {
        class: 'rich-text-link',
      },
    }),
    Superscript,
    SubScript, 
    Highlight.configure({
      multicolor: true,
    }),
    TextAlign.configure({ 
      types: ['heading', 'paragraph'] 
    }),
  ];

  const fullDescriptionEditor = useEditor({
    extensions: editorExtensions,
    content: form.values.fullDescription,
    onUpdate: ({ editor }) => {
      form.setFieldValue('fullDescription', editor.getHTML());
    },
  });

  const policiesEditor = useEditor({
    extensions: editorExtensions,
    content: form.values.policies,
    onUpdate: ({ editor }) => {
      form.setFieldValue('policies', editor.getHTML());
    },
  });

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
                <div style={{ marginBottom: 'var(--mantine-spacing-md)' }}>
                  <Text size="sm" fw={500} mb={5}>
                    Full Event Description <Text component="span" c="red">*</Text>
                  </Text>
                  <Text size="xs" c="dimmed" mb="xs">
                    This detailed description will be visible on the public events page
                  </Text>
                  <RichTextEditor editor={fullDescriptionEditor}>
                    <RichTextEditor.Toolbar sticky stickyOffset={60}>
                      <RichTextEditor.ControlsGroup>
                        <RichTextEditor.Bold />
                        <RichTextEditor.Italic />
                        <RichTextEditor.Underline />
                        <RichTextEditor.Strikethrough />
                        <RichTextEditor.ClearFormatting />
                        <RichTextEditor.Highlight />
                      </RichTextEditor.ControlsGroup>

                      <RichTextEditor.ControlsGroup>
                        <RichTextEditor.H1 />
                        <RichTextEditor.H2 />
                        <RichTextEditor.H3 />
                        <RichTextEditor.H4 />
                      </RichTextEditor.ControlsGroup>

                      <RichTextEditor.ControlsGroup>
                        <RichTextEditor.Blockquote />
                        <RichTextEditor.Hr />
                        <RichTextEditor.BulletList />
                        <RichTextEditor.OrderedList />
                        <RichTextEditor.Subscript />
                        <RichTextEditor.Superscript />
                      </RichTextEditor.ControlsGroup>

                      <RichTextEditor.ControlsGroup>
                        <RichTextEditor.Link />
                        <RichTextEditor.Unlink />
                      </RichTextEditor.ControlsGroup>

                      <RichTextEditor.ControlsGroup>
                        <RichTextEditor.AlignLeft />
                        <RichTextEditor.AlignCenter />
                        <RichTextEditor.AlignJustify />
                        <RichTextEditor.AlignRight />
                      </RichTextEditor.ControlsGroup>

                      <RichTextEditor.ControlsGroup>
                        <RichTextEditor.Undo />
                        <RichTextEditor.Redo />
                      </RichTextEditor.ControlsGroup>
                    </RichTextEditor.Toolbar>

                    <RichTextEditor.Content 
                      style={{ minHeight: '200px' }}
                    />
                  </RichTextEditor>
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
                  <RichTextEditor editor={policiesEditor}>
                    <RichTextEditor.Toolbar sticky stickyOffset={60}>
                      <RichTextEditor.ControlsGroup>
                        <RichTextEditor.Bold />
                        <RichTextEditor.Italic />
                        <RichTextEditor.Underline />
                        <RichTextEditor.Strikethrough />
                        <RichTextEditor.ClearFormatting />
                        <RichTextEditor.Highlight />
                      </RichTextEditor.ControlsGroup>

                      <RichTextEditor.ControlsGroup>
                        <RichTextEditor.H1 />
                        <RichTextEditor.H2 />
                        <RichTextEditor.H3 />
                        <RichTextEditor.H4 />
                      </RichTextEditor.ControlsGroup>

                      <RichTextEditor.ControlsGroup>
                        <RichTextEditor.Blockquote />
                        <RichTextEditor.Hr />
                        <RichTextEditor.BulletList />
                        <RichTextEditor.OrderedList />
                        <RichTextEditor.Subscript />
                        <RichTextEditor.Superscript />
                      </RichTextEditor.ControlsGroup>

                      <RichTextEditor.ControlsGroup>
                        <RichTextEditor.Link />
                        <RichTextEditor.Unlink />
                      </RichTextEditor.ControlsGroup>

                      <RichTextEditor.ControlsGroup>
                        <RichTextEditor.AlignLeft />
                        <RichTextEditor.AlignCenter />
                        <RichTextEditor.AlignJustify />
                        <RichTextEditor.AlignRight />
                      </RichTextEditor.ControlsGroup>

                      <RichTextEditor.ControlsGroup>
                        <RichTextEditor.Undo />
                        <RichTextEditor.Redo />
                      </RichTextEditor.ControlsGroup>
                    </RichTextEditor.Toolbar>

                    <RichTextEditor.Content 
                      style={{ minHeight: '150px' }}
                    />
                  </RichTextEditor>
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

          {/* Emails Tab */}
          <Tabs.Panel value="emails" pt="xl">
            <Stack gap="xl">
              <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                Email Templates
              </Title>
              <Text c="dimmed">Email template management will be implemented here.</Text>
            </Stack>
          </Tabs.Panel>

          {/* Volunteers/Staff Tab */}
          <Tabs.Panel value="volunteers-staff" pt="xl">
            <Stack gap="xl">
              <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                Volunteer Positions
              </Title>
              <Text c="dimmed">Volunteer position management will be implemented here.</Text>
            </Stack>
          </Tabs.Panel>
        </Tabs>
      </form>
    </Card>
  );
};