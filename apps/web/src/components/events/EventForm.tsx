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

  // TinyMCE configuration
  const tinyMCEConfig = {
    height: 300,
    menubar: false,
    plugins: [
      'advlist', 'autolink', 'lists', 'link', 'image', 'charmap', 'preview',
      'anchor', 'searchreplace', 'visualblocks', 'code', 'fullscreen',
      'insertdatetime', 'media', 'table', 'help', 'wordcount'
    ],
    toolbar: 'undo redo | blocks | ' +
      'bold italic forecolor | alignleft aligncenter ' +
      'alignright alignjustify | bullist numlist outdent indent | ' +
      'removeformat | help',
    content_style: 'body { font-family: -apple-system, BlinkMacSystemFont, San Francisco, Segoe UI, Roboto, Helvetica Neue, sans-serif; font-size: 14px }'
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
                <div style={{ marginBottom: 'var(--mantine-spacing-md)' }}>
                  <Text size="sm" fw={500} mb={5}>
                    Full Event Description <Text component="span" c="red">*</Text>
                  </Text>
                  <Text size="xs" c="dimmed" mb="xs">
                    This detailed description will be visible on the public events page
                  </Text>
                  <Editor
                    apiKey="no-api-key"
                    init={tinyMCEConfig}
                    initialValue={form.values.fullDescription}
                    onEditorChange={(content) => form.setFieldValue('fullDescription', content)}
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
                    apiKey="no-api-key"
                    init={{...tinyMCEConfig, height: 150}}
                    initialValue={form.values.policies}
                    onEditorChange={(content) => form.setFieldValue('policies', content)}
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

          {/* Emails Tab */}
          <Tabs.Panel value="emails" pt="xl">
            <Stack gap="xl">
              <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                Email Templates
              </Title>
              
              <Stack gap="lg">
                {/* Registration Confirmation Email */}
                <Card withBorder p="md">
                  <Title order={4} mb="sm">Registration Confirmation Email</Title>
                  <Text size="sm" c="dimmed" mb="md">
                    Sent automatically when someone registers for this event
                  </Text>
                  
                  <TextInput
                    label="Subject Line"
                    placeholder="Your registration for {EVENT_TITLE} is confirmed!"
                    defaultValue="Your registration for {EVENT_TITLE} is confirmed!"
                    mb="md"
                  />
                  
                  <div>
                    <Text size="sm" fw={500} mb={5}>Email Body</Text>
                    <Text size="xs" c="dimmed" mb="xs">
                      Use {'{EVENT_TITLE}'}, {'{PARTICIPANT_NAME}'}, {'{SESSION_DATES}'} as placeholders
                    </Text>
                    <Editor
                      apiKey="no-api-key"
                      init={{...tinyMCEConfig, height: 200}}
                      initialValue="<p>Dear {PARTICIPANT_NAME},</p><p>Thank you for registering for <strong>{EVENT_TITLE}</strong>!</p><p><strong>Session Details:</strong><br/>{SESSION_DATES}</p><p>We look forward to seeing you there!</p>"
                    />
                  </div>
                </Card>

                {/* Event Reminder Email */}
                <Card withBorder p="md">
                  <Title order={4} mb="sm">Event Reminder Email</Title>
                  <Text size="sm" c="dimmed" mb="md">
                    Sent 24 hours before the event starts
                  </Text>
                  
                  <TextInput
                    label="Subject Line"
                    placeholder="Reminder: {EVENT_TITLE} is tomorrow!"
                    defaultValue="Reminder: {EVENT_TITLE} is tomorrow!"
                    mb="md"
                  />
                  
                  <div>
                    <Text size="sm" fw={500} mb={5}>Email Body</Text>
                    <Editor
                      apiKey="no-api-key"
                      init={{...tinyMCEConfig, height: 200}}
                      initialValue="<p>Hello {PARTICIPANT_NAME},</p><p>This is a friendly reminder that <strong>{EVENT_TITLE}</strong> starts tomorrow!</p><p><strong>What to bring:</strong><br/>- Comfortable clothes<br/>- Water bottle<br/>- Positive attitude</p><p>See you there!</p>"
                    />
                  </div>
                </Card>

                {/* Cancellation Email */}
                <Card withBorder p="md">
                  <Title order={4} mb="sm">Event Cancellation Email</Title>
                  <Text size="sm" c="dimmed" mb="md">
                    Sent if the event needs to be cancelled
                  </Text>
                  
                  <TextInput
                    label="Subject Line"
                    placeholder="Important: {EVENT_TITLE} has been cancelled"
                    defaultValue="Important: {EVENT_TITLE} has been cancelled"
                    mb="md"
                  />
                  
                  <div>
                    <Text size="sm" fw={500} mb={5}>Email Body</Text>
                    <Editor
                      apiKey="no-api-key"
                      init={{...tinyMCEConfig, height: 200}}
                      initialValue="<p>Dear {PARTICIPANT_NAME},</p><p>We regret to inform you that <strong>{EVENT_TITLE}</strong> has been cancelled.</p><p><strong>Reason:</strong> [To be filled in when needed]</p><p>You will receive a full refund within 3-5 business days.</p><p>We apologize for any inconvenience.</p>"
                    />
                  </div>
                </Card>
              </Stack>
            </Stack>
          </Tabs.Panel>

          {/* Volunteers/Staff Tab */}
          <Tabs.Panel value="volunteers-staff" pt="xl">
            <Stack gap="xl">
              <Title order={2} c="burgundy" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                Volunteer Positions & Staff Assignments
              </Title>
              
              <Stack gap="lg">
                {/* Volunteer Positions */}
                <Card withBorder p="md">
                  <Group justify="space-between" mb="md">
                    <Title order={4}>Volunteer Positions Needed</Title>
                    <Button size="sm" variant="light" color="burgundy">
                      Add Position
                    </Button>
                  </Group>
                  
                  <Table striped highlightOnHover>
                    <Table.Thead>
                      <Table.Tr>
                        <Table.Th>Position</Table.Th>
                        <Table.Th>Needed</Table.Th>
                        <Table.Th>Filled</Table.Th>
                        <Table.Th>Session</Table.Th>
                        <Table.Th>Time</Table.Th>
                        <Table.Th>Actions</Table.Th>
                      </Table.Tr>
                    </Table.Thead>
                    <Table.Tbody>
                      <Table.Tr>
                        <Table.Td>Setup Crew</Table.Td>
                        <Table.Td>3</Table.Td>
                        <Table.Td>
                          <Badge color="green" variant="light">2/3</Badge>
                        </Table.Td>
                        <Table.Td>All Sessions</Table.Td>
                        <Table.Td>6:00 PM - 6:30 PM</Table.Td>
                        <Table.Td>
                          <Group gap="xs">
                            <Button size="xs" variant="light">Edit</Button>
                            <Button size="xs" variant="light" color="red">Delete</Button>
                          </Group>
                        </Table.Td>
                      </Table.Tr>
                      <Table.Tr>
                        <Table.Td>Safety Monitor</Table.Td>
                        <Table.Td>2</Table.Td>
                        <Table.Td>
                          <Badge color="red" variant="light">0/2</Badge>
                        </Table.Td>
                        <Table.Td>S1, S2, S3</Table.Td>
                        <Table.Td>During sessions</Table.Td>
                        <Table.Td>
                          <Group gap="xs">
                            <Button size="xs" variant="light">Edit</Button>
                            <Button size="xs" variant="light" color="red">Delete</Button>
                          </Group>
                        </Table.Td>
                      </Table.Tr>
                      <Table.Tr>
                        <Table.Td>Cleanup Crew</Table.Td>
                        <Table.Td>2</Table.Td>
                        <Table.Td>
                          <Badge color="yellow" variant="light">1/2</Badge>
                        </Table.Td>
                        <Table.Td>All Sessions</Table.Td>
                        <Table.Td>9:00 PM - 9:30 PM</Table.Td>
                        <Table.Td>
                          <Group gap="xs">
                            <Button size="xs" variant="light">Edit</Button>
                            <Button size="xs" variant="light" color="red">Delete</Button>
                          </Group>
                        </Table.Td>
                      </Table.Tr>
                    </Table.Tbody>
                  </Table>
                </Card>

                {/* Staff Assignments */}
                <Card withBorder p="md">
                  <Group justify="space-between" mb="md">
                    <Title order={4}>Staff Assignments</Title>
                    <Button size="sm" variant="light" color="burgundy">
                      Assign Staff
                    </Button>
                  </Group>
                  
                  <Table striped highlightOnHover>
                    <Table.Thead>
                      <Table.Tr>
                        <Table.Th>Staff Member</Table.Th>
                        <Table.Th>Role</Table.Th>
                        <Table.Th>Sessions</Table.Th>
                        <Table.Th>Responsibilities</Table.Th>
                        <Table.Th>Actions</Table.Th>
                      </Table.Tr>
                    </Table.Thead>
                    <Table.Tbody>
                      <Table.Tr>
                        <Table.Td>
                          <div>
                            <Text fw={500}>River Moon</Text>
                            <Text size="sm" c="dimmed">Lead Instructor</Text>
                          </div>
                        </Table.Td>
                        <Table.Td>
                          <Badge color="burgundy" variant="filled">Primary Teacher</Badge>
                        </Table.Td>
                        <Table.Td>S1, S2, S3</Table.Td>
                        <Table.Td>
                          <Text size="sm">
                            • Lead instruction<br/>
                            • Safety oversight<br/>
                            • Student questions
                          </Text>
                        </Table.Td>
                        <Table.Td>
                          <Group gap="xs">
                            <Button size="xs" variant="light">Edit</Button>
                            <Button size="xs" variant="light" color="red">Remove</Button>
                          </Group>
                        </Table.Td>
                      </Table.Tr>
                      <Table.Tr>
                        <Table.Td>
                          <div>
                            <Text fw={500}>Sage Blackthorne</Text>
                            <Text size="sm" c="dimmed">Assistant Instructor</Text>
                          </div>
                        </Table.Td>
                        <Table.Td>
                          <Badge color="blue" variant="filled">Co-Teacher</Badge>
                        </Table.Td>
                        <Table.Td>S2, S3</Table.Td>
                        <Table.Td>
                          <Text size="sm">
                            • Individual guidance<br/>
                            • Advanced techniques<br/>
                            • Equipment setup
                          </Text>
                        </Table.Td>
                        <Table.Td>
                          <Group gap="xs">
                            <Button size="xs" variant="light">Edit</Button>
                            <Button size="xs" variant="light" color="red">Remove</Button>
                          </Group>
                        </Table.Td>
                      </Table.Tr>
                    </Table.Tbody>
                  </Table>
                </Card>

                {/* Special Requirements */}
                <Card withBorder p="md">
                  <Title order={4} mb="md">Special Requirements & Notes</Title>
                  <div>
                    <Text size="sm" fw={500} mb={5}>Staff Requirements</Text>
                    <Text size="xs" c="dimmed" mb="xs">
                      Additional notes for staff and volunteer coordination
                    </Text>
                    <Editor
                      apiKey="no-api-key"
                      init={{...tinyMCEConfig, height: 150}}
                      initialValue="<p><strong>Setup Requirements:</strong></p><ul><li>Tables and chairs for 20 participants</li><li>Rope bundles organized by skill level</li><li>Safety equipment check</li></ul><p><strong>During Event:</strong></p><ul><li>Monitor participant safety at all times</li><li>Assist with complex ties</li><li>Answer questions about techniques</li></ul>"
                    />
                  </div>
                </Card>
              </Stack>
            </Stack>
          </Tabs.Panel>
        </Tabs>
      </form>
    </Card>
  );
};