// CheckInPage - Main check-in interface page (KIOSK MODE - NO AUTHENTICATION)
// Entry point for event staff to check in attendees using session tokens
// Route: /events/{eventId}/checkin?token={sessionToken}&event={eventId}

import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { Container, Alert, Stack, Text } from '@mantine/core';

import { CheckInInterface } from '../../features/checkin/components/CheckInInterface';

/**
 * CheckIn page with token-based authentication (KIOSK MODE)
 * No user login required - access controlled by session tokens
 * Route: /events/{eventId}/checkin?token={sessionToken}&event={eventId}
 */
export function CheckInPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  // Get token from URL query parameter
  const token = searchParams.get('token');
  const urlEventId = searchParams.get('event');

  // Validate URL has required parameters
  if (!token || !eventId) {
    return (
      <Container size="md" py="xl">
        <Alert color="red" title="Invalid Check-In Link">
          <Stack gap="md">
            <Text>This check-in link is invalid or expired.</Text>
            <Text size="sm" c="dimmed">
              Please contact an event organizer for a new check-in link.
            </Text>
            <Text size="sm" c="dimmed">
              Missing: {!token && 'token'} {!eventId && 'event ID'}
            </Text>
          </Stack>
        </Alert>
      </Container>
    );
  }

  // Optional: Verify event ID from URL matches route param
  if (urlEventId && urlEventId !== eventId) {
    return (
      <Container size="md" py="xl">
        <Alert color="yellow" title="Event Mismatch">
          <Stack gap="md">
            <Text>The event ID in the link doesn't match the event route.</Text>
            <Text size="sm" c="dimmed">
              Route event ID: {eventId}
            </Text>
            <Text size="sm" c="dimmed">
              URL event ID: {urlEventId}
            </Text>
          </Stack>
        </Alert>
      </Container>
    );
  }

  // Pass token to CheckInInterface for API calls
  return (
    <CheckInInterface
      eventId={eventId}
      sessionToken={token}
      onNavigateToDashboard={() => navigate(`/events/${eventId}/checkin/dashboard?token=${token}&event=${eventId}`)}
    />
  );
}