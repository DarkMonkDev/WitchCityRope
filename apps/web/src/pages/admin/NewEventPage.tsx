import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Container, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { EventForm, EventFormData } from '../../components/events/EventForm';
import { useCreateEvent } from '../../features/events/api/mutations';
import type { components } from '@witchcityrope/shared-types';

// ✅ Use auto-generated CreateEventRequest and SessionDto from backend DTOs
type CreateEventRequest = components['schemas']['CreateEventRequest'];


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
      // ✅ Transform EventFormData to CreateEventRequest format (auto-generated from backend DTO)

      // Auto-create default session if none provided
      let sessions = formData.sessions || [];
      if (sessions.length === 0) {
        // Create default session 60 days in the future (using UTC to avoid timezone bugs)
        const now = new Date();
        const year = now.getUTCFullYear();
        const month = now.getUTCMonth();
        const day = now.getUTCDate() + 60; // 60 days from today

        // Create datetime objects using Date.UTC() to ensure UTC timezone
        const startDateTime = new Date(Date.UTC(year, month, day, 19, 0, 0, 0)); // 7:00 PM UTC
        const endDateTime = new Date(Date.UTC(year, month, day, 21, 0, 0, 0));   // 9:00 PM UTC
        const dateOnly = new Date(Date.UTC(year, month, day, 0, 0, 0, 0));

        sessions = [{
          id: '', // Backend will generate
          sessionIdentifier: 'S1',
          name: 'Default',
          startDate: dateOnly.toISOString(),
          startTime: startDateTime.toISOString(),
          endTime: endDateTime.toISOString(),
          capacity: 20,
          registrationCount: 0
        }];
      }

      // Calculate total capacity from sessions
      const totalCapacity = sessions.reduce((sum, session) => sum + (session.capacity || 0), 0);

      // Determine if event is published based on status
      const isPublished = formData.status === 'Published';

      // Use first session date as event start date
      const firstSessionDate = sessions[0].startTime;
      // EndDate must be AFTER startDate - use last session's end time, or add 2 hours for single session
      const lastSessionEndTime = sessions[sessions.length - 1].endTime;
      const endDate = new Date(new Date(lastSessionEndTime).getTime()).toISOString();

      const createData: CreateEventRequest = {
        title: formData.title,
        description: formData.fullDescription,
        shortDescription: formData.shortDescription || null,
        policies: formData.policies || null,
        startDate: firstSessionDate,
        endDate: endDate, // Must be AFTER startDate (validation requirement)
        // ✅ CRITICAL: venueId must be number, boolean flags for event registration options
        venueId: parseInt(formData.venueId) || 1,
        // Boolean flags replace eventType enum
        allowRsvps: formData.allowRsvps ?? false,
        requireTicketPurchase: formData.requireTicketPurchase ?? true,
        vettedMembersOnly: formData.vettedMembersOnly ?? false,
        capacity: totalCapacity,
        isPublished: isPublished,
        // Send sessions - backend will create them with the event
        sessions: sessions.map(s => ({
          sessionIdentifier: s.sessionIdentifier,
          name: s.name,
          startDate: s.startDate,
          startTime: s.startTime,
          endTime: s.endTime,
          capacity: s.capacity
        })),
        // Don't send ticket types or volunteer positions on create - add them after
        ticketTypes: null,
        volunteerPositions: null,
        teacherIds: formData.teacherIds || null,
        // Timing controls (all optional)
        registrationOpenHours: formData.registrationOpenHours || null,
        registrationCloseHours: formData.registrationCloseHours || null,
        cancellationCloseHours: formData.cancellationCloseHours || null,
        volunteerRegistrationCloseHours: formData.volunteerRegistrationCloseHours || null,
        volunteerCancellationCloseHours: formData.volunteerCancellationCloseHours || null,
      };

      console.log('NewEventPage: Submitting event creation with data:', createData);

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
