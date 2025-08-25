import React, { useState } from 'react';
import { Container, Title, Paper, Group } from '@mantine/core';
import { EventForm, EventFormData, EventSession, EventTicketType } from '../../components/events';
import { notifications } from '@mantine/notifications';

// Mock data for demonstration
const mockSessions: EventSession[] = [
  {
    id: '1',
    sessionIdentifier: 'S1',
    name: 'Fundamentals Day',
    date: '2025-02-15',
    startTime: '19:00',
    endTime: '21:00',
    capacity: 20,
    registeredCount: 8,
  },
  {
    id: '2',
    sessionIdentifier: 'S2',
    name: 'Intermediate Practice',
    date: '2025-02-16',
    startTime: '19:00',
    endTime: '21:00',
    capacity: 20,
    registeredCount: 15,
  },
  {
    id: '3',
    sessionIdentifier: 'S3',
    name: 'Advanced Techniques',
    date: '2025-02-17',
    startTime: '19:00',
    endTime: '21:00',
    capacity: 20,
    registeredCount: 8,
  },
];

const mockTicketTypes: EventTicketType[] = [
  {
    id: '1',
    name: 'Full 3-Day Series Pass',
    type: 'Single',
    sessionIdentifiers: ['S1', 'S2', 'S3'],
    minPrice: 120,
    maxPrice: 150,
    quantityAvailable: 15,
    salesEndDate: '2025-02-17',
  },
  {
    id: '2',
    name: 'Day 1: Fundamentals Only',
    type: 'Single',
    sessionIdentifiers: ['S1'],
    minPrice: 45,
    maxPrice: 60,
    quantityAvailable: 10,
    salesEndDate: '2025-02-15',
  },
  {
    id: '3',
    name: 'Weekend Pass (Days 2-3)',
    type: 'Couples',
    sessionIdentifiers: ['S2', 'S3'],
    minPrice: 80,
    maxPrice: 100,
    quantityAvailable: 8,
    salesEndDate: '2025-02-16',
  },
];

const mockInitialData: Partial<EventFormData> = {
  eventType: 'class',
  title: 'Rope Bondage Intensive: 3-Day Series',
  shortDescription: 'Comprehensive rope bondage workshop covering fundamentals through advanced techniques',
  fullDescription: '<p><strong>Join us for an intensive 3-day rope bondage series!</strong></p><p>This comprehensive workshop will take you from the basics through advanced techniques.</p>',
  policies: '<p><strong>Prerequisites:</strong> Basic rope handling experience recommended</p><p><strong>Safety:</strong> All safety equipment provided</p>',
  venueId: 'main-studio',
  teacherIds: ['river-moon', 'sage-blackthorne'],
  sessions: mockSessions,
  ticketTypes: mockTicketTypes,
};

export const EventSessionMatrixDemo: React.FC = () => {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showForm, setShowForm] = useState(true);

  const handleSubmit = async (data: EventFormData) => {
    setIsSubmitting(true);
    
    // Simulate API call
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

  return (
    <Container size="xl" py="xl">
      <Title order={1} ta="center" mb="xl" c="burgundy">
        Event Session Matrix Demo
      </Title>

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
          <Button
            onClick={() => setShowForm(true)}
            style={{
              background: 'linear-gradient(135deg, var(--mantine-color-amber-6), #DAA520)',
              border: 'none',
              color: 'var(--mantine-color-dark-9)',
              borderRadius: '12px 6px 12px 6px',
              fontWeight: 600,
            }}
          >
            Show Form Again
          </Button>
        </Paper>
      )}
    </Container>
  );
};