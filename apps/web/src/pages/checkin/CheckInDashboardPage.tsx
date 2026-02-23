// CheckInDashboardPage - Event dashboard for organizers (KIOSK MODE - NO AUTHENTICATION)
// Full dashboard view with analytics and export capabilities
// Uses session token instead of user authentication
// Route: /events/{eventId}/checkin/dashboard?token={sessionToken}&event={eventId}

import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { Container, Alert, Stack, Button, Group, Text } from '@mantine/core';
import { IconArrowLeft, IconUsers } from '@tabler/icons-react';

import { CheckInDashboard } from '../../features/checkin/components/CheckInDashboard';
import { SyncStatus } from '../../features/checkin/components/SyncStatus';
import { useEventDashboard, useExportAttendance } from '../../features/checkin/hooks/useCheckIn';

/**
 * Full dashboard page for event check-in management (KIOSK MODE)
 * No user login required - access controlled by session tokens
 * Route: /events/{eventId}/checkin/dashboard?token={sessionToken}&event={eventId}
 */
export function CheckInDashboardPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  // Get token from URL query parameter
  const token = searchParams.get('token');
  const urlEventId = searchParams.get('event');

  // Validate URL has required parameters
  if (!token || !eventId) {
    return (
      <Container size="lg" py="xl">
        <Alert color="red" title="Invalid Dashboard Link">
          <Stack gap="md">
            <Text>This dashboard link is invalid or expired.</Text>
            <Text size="sm" c="dimmed">
              Please contact an event organizer for a new dashboard link.
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
      <Container size="lg" py="xl">
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

  // API hooks (pass sessionToken for authentication)
  const {
    data: dashboard,
    isLoading,
    error,
    refetch
  } = useEventDashboard(eventId, token, true);

  const exportMutation = useExportAttendance(eventId, token);

  const handleExport = () => {
    exportMutation.mutate('csv');
  };

  return (
    <Container size="xl" py="md">
      <Stack gap="lg">
        {/* Header */}
        <Group justify="space-between" align="center">
          <Group align="center" gap="md">
            <Button
              variant="subtle"
              onClick={() => navigate(`/events/${eventId}/checkin?token=${token}&event=${eventId}`)}
              leftSection={<IconArrowLeft size={16} />}
              size="sm"
            >
              Back to Check-In
            </Button>

            <Group align="center" gap="xs">
              <IconUsers size={20} color="var(--mantine-color-wcr-7)" />
              <Text
                size="xl"
                fw={700}
                style={{ fontFamily: 'Bodoni Moda, serif' }}
              >
                {(dashboard as any)?.eventTitle || 'Event Dashboard'}
              </Text>
            </Group>
          </Group>

          <SyncStatus showDetails />
        </Group>

        {/* Dashboard Content */}
        <CheckInDashboard
          dashboard={dashboard as any}
          isLoading={isLoading}
          error={error?.message || ''}
          onRefresh={refetch}
          onExport={handleExport}
          canExport={true} // All token holders can export (permissions enforced by backend)
        />

        {/* Event Info */}
        {!!dashboard && (
          <Group justify="center" mt="xl">
            <Text size="sm" c="dimmed" ta="center">
              Event ID: {eventId} • {(dashboard as any)?.capacity?.checkedInCount || 0} attendees checked in
            </Text>
          </Group>
        )}
      </Stack>
    </Container>
  );
}