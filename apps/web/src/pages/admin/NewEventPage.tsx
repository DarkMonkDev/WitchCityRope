import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Container, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { EventForm, EventFormData } from '../../components/events/EventForm';
import { useCreateEvent } from '../../features/events/api/mutations';
import type { CreateEventData } from '../../types/api.types';

/**
 * NewEventPage - Create new event page for administrators
 * Route: /admin/events/new
 *
 * ARCHITECTURE:
 * Part of the dedicated page navigation pattern (NOT a modal)
 *
 * How Users Get Here:
 * - Click "Create Event" button in AdminEventsPage → navigates to /admin/events/new
 *
 * Page Content:
 * - Full-page EventForm component (NOT in a modal dialog)
 * - All tabs accessible (Basic Info, Setup, Emails, Volunteers, RSVP/Tickets, Attendees)
 * - Form starts empty (no initial data)
 *
 * On Save:
 * - Creates event via POST /api/events
 * - Shows success notification
 * - Navigates to /admin/events/:id (the newly created event's detail/edit page)
 *
 * On Cancel:
 * - Navigates back to /admin/events (table view)
 *
 * IMPORTANT for Tests:
 * - This is a PAGE, not a modal
 * - Don't look for [role="dialog"]
 * - EventForm renders directly on page
 * - URL changes to /admin/events/new (not a modal overlay)
 */
export const NewEventPage: React.FC = () => {
  const navigate = useNavigate();
  const createEventMutation = useCreateEvent();
  const [formDirty, setFormDirty] = useState(false);

  const handleSubmit = async (formData: EventFormData) => {
    try {
      // Transform EventFormData to CreateEventData format expected by API
      const createData: CreateEventData = {
        title: formData.title,
        description: formData.fullDescription,
        // For now, using placeholder dates - in a real implementation,
        // these would come from the sessions data
        startDate: new Date().toISOString(),
        endDate: new Date(Date.now() + 86400000).toISOString(), // +1 day
        capacity: 100,
        location: formData.venueId,
      };

      const newEvent = await createEventMutation.mutateAsync(createData);

      notifications.show({
        title: 'Event Created',
        message: 'Your event has been created successfully.',
        color: 'green'
      });

      // Navigate to the event detail page
      // Type assertion needed because mutateAsync returns Promise<unknown>
      const eventId = (newEvent as { id: string }).id;
      navigate(`/admin/events/${eventId}`);
    } catch (error) {
      console.error('Failed to create event:', error);

      notifications.show({
        title: 'Creation Failed',
        message: error instanceof Error ? error.message : 'Failed to create event. Please try again.',
        color: 'red'
      });
    }
  };

  const handleCancel = () => {
    if (formDirty) {
      const confirmed = window.confirm('You have unsaved changes. Are you sure you want to leave?');
      if (!confirmed) return;
    }
    navigate('/admin/events');
  };

  const handleFormChange = () => {
    setFormDirty(true);
  };

  return (
    <Container size="xl" py="xl">
      <Title order={1} mb="xl" ta="center" c="burgundy">
        Create New Event
      </Title>

      <EventForm
        onSubmit={handleSubmit}
        onCancel={handleCancel}
        isSubmitting={createEventMutation.isPending}
        onFormChange={handleFormChange}
        formDirty={formDirty}
      />
    </Container>
  );
};
