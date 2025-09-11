import React from 'react';
import { Container, Title } from '@mantine/core';
import { EventForm } from '../components/events/EventForm';

/**
 * Test page to render and verify EventForm wireframe implementation
 */
export const EventFormTestPage: React.FC = () => {
  const handleSubmit = (data: any) => {
    console.log('Form submitted:', data);
  };

  const handleCancel = () => {
    console.log('Form cancelled');
  };

  return (
    <Container size="xl" py="xl">
      <Title order={1} mb="xl" ta="center" c="burgundy">
        Event Form - Wireframe Test
      </Title>
      
      <EventForm
        onSubmit={handleSubmit}
        onCancel={handleCancel}
        isSubmitting={false}
      />
    </Container>
  );
};