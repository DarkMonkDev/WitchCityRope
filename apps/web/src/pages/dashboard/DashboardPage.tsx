// DashboardPage Component
// Main dashboard page layout with all dashboard components

import React from 'react';
import {
  Container,
  Stack,
  SimpleGrid,
  Title,
  Text,
  Alert,
  Button,
  Group,
  Loader,
  Center,
  Box
} from '@mantine/core';
import {
  IconAlertCircle,
  IconRefresh,
  IconCalendarEvent,
  IconHome
} from '@tabler/icons-react';
import { useNavigate } from 'react-router-dom';
import { UserDashboard } from '../../features/dashboard/components/UserDashboard';
import { UpcomingEvents } from '../../features/dashboard/components/UpcomingEvents';
import { MembershipStatistics } from '../../features/dashboard/components/MembershipStatistics';
import { useDashboardData, useDashboardError } from '../../features/dashboard/hooks/useDashboard';

/**
 * DashboardPage Component
 * Main user dashboard with responsive layout
 */
export const DashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const {
    isLoading,
    isError,
    error,
    isSuccess,
    data,
    refetchAll
  } = useDashboardData(3);
  
  const dashboardError = useDashboardError(error);

  // Navigation handlers
  const handleViewAllEvents = () => {
    navigate('/events');
  };

  const handleGoToEvents = () => {
    navigate('/events');
  };

  // Global loading state
  if (isLoading) {
    return (
      <Container size="xl" py="xl">
        <Stack gap="xl">
          <Box>
            <Title order={1} size="h2" mb="sm">
              Dashboard
            </Title>
            <Text c="dimmed">
              Loading your personal dashboard...
            </Text>
          </Box>
          
          <Center py="xl">
            <Stack gap="md" align="center">
              <Loader size="lg" color="wcr" />
              <Text size="sm" c="dimmed">
                Fetching your latest information
              </Text>
            </Stack>
          </Center>
        </Stack>
      </Container>
    );
  }

  // Global error state
  if (isError) {
    return (
      <Container size="xl" py="xl">
        <Stack gap="xl">
          <Box>
            <Title order={1} size="h2" mb="sm">
              Dashboard
            </Title>
            <Text c="dimmed">
              Your personal WitchCityRope dashboard
            </Text>
          </Box>
          
          <Alert
            icon={<IconAlertCircle size={20} />}
            color="red"
            title="Unable to Load Dashboard"
            variant="light"
          >
            <Stack gap="md">
              <Text>
                {dashboardError?.message || 'We encountered an issue loading your dashboard. Please try again.'}
              </Text>
              
              <Group gap="sm">
                <Button
                  leftSection={<IconRefresh size={16} />}
                  onClick={refetchAll}
                  variant="light"
                  color="blue"
                >
                  Try Again
                </Button>
                
                <Button
                  leftSection={<IconHome size={16} />}
                  onClick={() => navigate('/')}
                  variant="subtle"
                >
                  Go to Homepage
                </Button>
              </Group>
            </Stack>
          </Alert>
        </Stack>
      </Container>
    );
  }

  return (
    <Container size="xl" py="xl">
      <Stack gap="xl">
        {/* Page header */}
        <Box>
          <Title order={1} size="h2" mb="sm">
            Dashboard
          </Title>
          <Text c="dimmed">
            Your personal WitchCityRope dashboard
          </Text>
        </Box>

        {/* Main dashboard layout */}
        <SimpleGrid
          cols={{ base: 1, lg: 3 }}
          spacing="lg"
          verticalSpacing="lg"
        >
          {/* Left column - User info (spans 2 cols on desktop) */}
          <Box style={{ gridColumn: 'span 2' }}>
            <Stack gap="lg">
              <UserDashboard />
              <UpcomingEvents
                count={3}
                onViewAllEvents={handleViewAllEvents}
              />
            </Stack>
          </Box>

          {/* Right column - Statistics */}
          <Box>
            <MembershipStatistics />
          </Box>
        </SimpleGrid>

        {/* Quick actions */}
        <Alert
          color="blue"
          variant="light"
          styles={{
            root: {
              backgroundColor: 'var(--mantine-color-blue-0)',
              border: '1px solid var(--mantine-color-blue-3)',
            }
          }}
        >
          <Group gap="md" justify="space-between" align="center">
            <Box>
              <Text size="sm" fw={500} mb="xs">
                Looking for something to do?
              </Text>
              <Text size="xs" c="dimmed">
                Browse our upcoming events and join the community!
              </Text>
            </Box>
            
            <Button
              leftSection={<IconCalendarEvent size={16} />}
              onClick={handleGoToEvents}
              variant="filled"
              color="blue"
              size="sm"
            >
              Browse Events
            </Button>
          </Group>
        </Alert>

        {/* Debug info (only in development) */}
        {import.meta.env.DEV && isSuccess && (
          <Alert color="gray" variant="light" title="Debug Info (Development Only)">
            <Text size="xs" c="dimmed">
              Dashboard loaded successfully. Data freshness:
              <br />
              • Dashboard: {data.dashboard ? '✅ Loaded' : '❌ Missing'}
              • Events: {data.events ? `✅ ${data.events.upcomingEvents.length} events` : '❌ Missing'}
              • Statistics: {data.statistics ? '✅ Loaded' : '❌ Missing'}
            </Text>
          </Alert>
        )}
      </Stack>
    </Container>
  );
};