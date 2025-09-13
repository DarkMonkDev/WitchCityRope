// CheckInPage - Main check-in interface page
// Entry point for event staff to check in attendees

import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Container, Alert, Stack, Button, Loader, Center, Text } from '@mantine/core';
import { IconArrowLeft } from '@tabler/icons-react';

import { CheckInInterface } from '../../features/checkin/components/CheckInInterface';
import { useActiveEvents } from '../../features/checkin/hooks/useCheckIn';
import { useAuth } from '../../contexts/AuthContext';

/**
 * CheckIn page with event selection and staff interface
 * Route: /events/{eventId}/checkin
 */
export function CheckInPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [staffMemberId, setStaffMemberId] = useState<string>('');

  // Get active events for validation
  const { 
    data: activeEvents, 
    isLoading: loadingEvents, 
    error: eventsError 
  } = useActiveEvents();

  // Set staff member ID from authenticated user
  useEffect(() => {
    if (user?.id) {
      setStaffMemberId(user.id);
    }
  }, [user]);

  // Validate event ID
  const currentEvent = activeEvents?.find(event => event.id === eventId);

  if (!eventId) {
    return (
      <Container size="md" py="xl">
        <Alert color="red" title="Invalid Event">
          <Stack gap="md">
            <Text>No event ID provided in the URL.</Text>
            <Button onClick={() => navigate('/events')} leftSection={<IconArrowLeft size={16} />}>
              Back to Events
            </Button>
          </Stack>
        </Alert>
      </Container>
    );
  }

  if (!user) {
    return (
      <Container size="md" py="xl">
        <Alert color="yellow" title="Authentication Required">
          <Stack gap="md">
            <Text>You must be logged in to access the check-in interface.</Text>
            <Button onClick={() => navigate('/login')} color="blue">
              Login
            </Button>
          </Stack>
        </Alert>
      </Container>
    );
  }

  // Check if user has check-in permissions
  const canCheckIn = user.roles?.some(role => 
    ['CheckInStaff', 'EventOrganizer', 'Administrator'].includes(role)
  );

  if (!canCheckIn) {
    return (
      <Container size="md" py="xl">
        <Alert color="red" title="Access Denied">
          <Stack gap="md">
            <Text>You don't have permission to access the check-in interface.</Text>
            <Text size="sm" c="dimmed">
              Required roles: Check-in Staff, Event Organizer, or Administrator
            </Text>
            <Button onClick={() => navigate('/dashboard')} leftSection={<IconArrowLeft size={16} />}>
              Back to Dashboard
            </Button>
          </Stack>
        </Alert>
      </Container>
    );
  }

  if (loadingEvents) {
    return (
      <Center py="xl">
        <Stack align="center" gap="md">
          <Loader size="lg" />
          <Text size="sm" c="dimmed">Loading event information...</Text>
        </Stack>
      </Center>
    );
  }

  if (eventsError) {
    return (
      <Container size="md" py="xl">
        <Alert color="red" title="Error Loading Events">
          <Stack gap="md">
            <Text>{eventsError}</Text>
            <Button onClick={() => window.location.reload()} color="blue">
              Retry
            </Button>
          </Stack>
        </Alert>
      </Container>
    );
  }

  if (!currentEvent) {
    return (
      <Container size="md" py="xl">
        <Alert color="yellow" title="Event Not Found">
          <Stack gap="md">
            <Text>
              The event with ID "{eventId}" was not found or is not currently active for check-in.
            </Text>
            <Button onClick={() => navigate('/events')} leftSection={<IconArrowLeft size={16} />}>
              Back to Events
            </Button>
          </Stack>
        </Alert>
      </Container>
    );
  }

  return (
    <CheckInInterface
      eventId={eventId}
      staffMemberId={staffMemberId}
      eventTitle={currentEvent.title}
      onNavigateToDashboard={() => navigate(`/events/${eventId}/checkin/dashboard`)}
    />
  );
}