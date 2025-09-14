// CheckInDashboardPage - Event dashboard for organizers
// Full dashboard view with analytics and export capabilities

import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Container, Alert, Stack, Button, Group, Text } from '@mantine/core';
import { IconArrowLeft, IconUsers } from '@tabler/icons-react';

import { CheckInDashboard } from '../../features/checkin/components/CheckInDashboard';
import { SyncStatus } from '../../features/checkin/components/SyncStatus';
import { useEventDashboard, useExportAttendance } from '../../features/checkin/hooks/useCheckIn';
import { useAuth } from '../../contexts/AuthContext';

/**
 * Full dashboard page for event check-in management
 * Route: /events/{eventId}/checkin/dashboard
 */
export function CheckInDashboardPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();

  // API hooks
  const {
    data: dashboard,
    isLoading,
    error,
    refetch
  } = useEventDashboard(eventId || '', !!eventId);

  const exportMutation = useExportAttendance(eventId || '');

  if (!eventId) {
    return (
      <Container size="lg" py="xl">
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
      <Container size="lg" py="xl">
        <Alert color="yellow" title="Authentication Required">
          <Stack gap="md">
            <Text>You must be logged in to access the dashboard.</Text>
            <Button onClick={() => navigate('/login')} color="blue">
              Login
            </Button>
          </Stack>
        </Alert>
      </Container>
    );
  }

  // Check permissions for dashboard access
  const canViewDashboard = user.role && 
    ['CheckInStaff', 'EventOrganizer', 'Administrator'].includes(user.role);

  const canExport = user.role && 
    ['EventOrganizer', 'Administrator'].includes(user.role);

  if (!canViewDashboard) {
    return (
      <Container size="lg" py="xl">
        <Alert color="red" title="Access Denied">
          <Stack gap="md">
            <Text>You don't have permission to access the event dashboard.</Text>
            <Button onClick={() => navigate('/dashboard')} leftSection={<IconArrowLeft size={16} />}>
              Back to Dashboard
            </Button>
          </Stack>
        </Alert>
      </Container>
    );
  }

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
              onClick={() => navigate(`/events/${eventId}/checkin`)}
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
          onExport={canExport ? handleExport : undefined}
          canExport={canExport}
        />

        {/* Additional Actions for Organizers */}
        {canExport && dashboard && (
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