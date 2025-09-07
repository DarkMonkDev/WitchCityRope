import React, { useState } from 'react';
import { Container, Title, Paper, Group, Loader, Alert, Text } from '@mantine/core';
import { EventForm, EventFormData, EventSession, EventTicketType } from '../../components/events';
import { notifications } from '@mantine/notifications';
import { 
  useEventSessionMatrix, 
  useCreateEventSession, 
  useUpdateEventSession, 
  useDeleteEventSession,
  useCreateEventTicketType,
  useUpdateEventTicketType,
  useDeleteEventTicketType
} from '../../lib/api/hooks/useEventSessions';
import { 
  convertEventSessionFromDto, 
  convertEventTicketTypeFromDto,
  convertEventSessionToCreateDto,
  convertEventTicketTypeToCreateDto
} from '../../lib/api/utils/eventSessionConversion';

// Demo event ID - in a real application this would come from route params or props
const DEMO_EVENT_ID = 'demo-event-123';

export const EventSessionMatrixDemo: React.FC = () => {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showForm, setShowForm] = useState(true);

  // Load real data from API
  const { 
    sessions: sessionsData, 
    ticketTypes: ticketTypesData, 
    isLoading, 
    hasError 
  } = useEventSessionMatrix(DEMO_EVENT_ID);

  // Convert API DTOs to component format
  const sessions: EventSession[] = Array.isArray(sessionsData) ? sessionsData.map(convertEventSessionFromDto) : [];
  const ticketTypes: EventTicketType[] = Array.isArray(ticketTypesData) ? ticketTypesData.map(dto => 
    convertEventTicketTypeFromDto(dto, sessions)
  ) : [];

  // Mock data for basic event info (in real app this would come from event API)
  const mockInitialData: Partial<EventFormData> = {
    eventType: 'class',
    title: 'Rope Bondage Intensive: 3-Day Series',
    shortDescription: 'Comprehensive rope bondage workshop covering fundamentals through advanced techniques',
    fullDescription: '<p><strong>Join us for an intensive 3-day rope bondage series!</strong></p><p>This comprehensive workshop will take you from the basics through advanced techniques.</p>',
    policies: '<p><strong>Prerequisites:</strong> Basic rope handling experience recommended</p><p><strong>Safety:</strong> All safety equipment provided</p>',
    venueId: 'main-studio',
    teacherIds: ['river-moon', 'sage-blackthorne'],
    sessions,
    ticketTypes,
  };

  const handleSubmit = async (data: EventFormData) => {
    setIsSubmitting(true);
    
    // Simulate API call - in real implementation this would save to backend
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    console.log('Form submitted with data:', data);
    
    notifications.show({
      title: 'Success!',
      message: 'Event session matrix data saved successfully',
      color: 'green',
    });
    
    setIsSubmitting(false);
  };

  const handleCancel = () => {
    setShowForm(false);
    notifications.show({
      title: 'Cancelled',
      message: 'Form editing cancelled',
      color: 'orange',
    });
  };

  // Loading state
  if (isLoading) {
    return (
      <Container size="xl" py="xl">
        <Title order={1} ta="center" mb="xl" c="burgundy">
          Event Session Matrix Demo
        </Title>
        <Paper p="xl" ta="center">
          <Loader size="lg" color="burgundy" />
          <Text mt="md" c="dimmed">Loading event session matrix data...</Text>
        </Paper>
      </Container>
    );
  }

  // Error state
  if (hasError) {
    return (
      <Container size="xl" py="xl">
        <Title order={1} ta="center" mb="xl" c="burgundy">
          Event Session Matrix Demo
        </Title>
        <Alert color="red" mb="xl">
          <Text fw={600}>Unable to load session matrix data</Text>
          <Text size="sm" mt="xs">
            This demo requires backend API endpoints to be available. 
            The form will work with mock data if you proceed.
          </Text>
        </Alert>
        <Paper p="xl" ta="center">
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => window.location.reload()}
          >
            Retry Loading
          </button>
        </Paper>
      </Container>
    );
  }

  return (
    <Container size="xl" py="xl">
      <Title order={1} ta="center" mb="xl" c="burgundy">
        Event Session Matrix Demo
      </Title>

      {/* API Status Indicator */}
      <Alert color="green" mb="xl">
        <Group>
          <Text fw={600}>✅ Connected to Backend API</Text>
          <Text size="sm">
            Sessions: {sessions.length} | Ticket Types: {ticketTypes.length}
          </Text>
        </Group>
      </Alert>

      {showForm ? (
        <Paper shadow="sm" radius="md">
          <EventForm
            initialData={mockInitialData}
            onSubmit={handleSubmit}
            onCancel={handleCancel}
            isSubmitting={isSubmitting}
          />
        </Paper>
      ) : (
        <Paper p="xl" ta="center">
          <Title order={2} c="dimmed" mb="md">Form Cancelled</Title>
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => setShowForm(true)}
          >
            Show Form Again
          </button>
        </Paper>
      )}
    </Container>
  );
};