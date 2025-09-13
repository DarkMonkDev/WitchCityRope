// CheckInDashboard component for CheckIn system
// Real-time event overview with statistics and recent activity

import React from 'react';
import {
  Grid,
  Card,
  Stack,
  Group,
  Text,
  Progress,
  Badge,
  ActionIcon,
  ScrollArea,
  Box,
  Divider,
  Button,
  Alert,
  Loader,
  Center
} from '@mantine/core';
import { 
  IconUsers, 
  IconClock, 
  IconAlertCircle, 
  IconRefresh,
  IconDownload,
  IconWifi,
  IconWifiOff
} from '@tabler/icons-react';
import type { CheckInDashboard, RecentCheckIn, CapacityInfo } from '../types/checkin.types';
import { EVENT_STATUS_CONFIGS, TOUCH_TARGETS } from '../types/checkin.types';
import { useOfflineSync } from '../hooks/useOfflineSync';

interface CheckInDashboardProps {
  dashboard: CheckInDashboard | null;
  isLoading?: boolean;
  error?: string | null;
  onRefresh: () => void;
  onExport?: () => void;
  canExport?: boolean;
}

/**
 * Statistics card for capacity and attendance metrics
 */
function StatisticsCard({ capacity }: { capacity: CapacityInfo }) {
  const capacityPercentage = (capacity.checkedInCount / capacity.totalCapacity) * 100;
  const isAtCapacity = capacityPercentage >= 100;
  const isNearCapacity = capacityPercentage >= 90;

  return (
    <Card shadow="sm" padding="lg" radius="md">
      <Stack gap="md">
        <Group justify="space-between" align="center">
          <Group align="center" gap="xs">
            <IconUsers size={20} color="#880124" />
            <Text fw={600} size="lg" style={{ fontFamily: 'Montserrat, sans-serif' }}>
              Capacity
            </Text>
          </Group>
          
          <Badge 
            color={isAtCapacity ? "red" : isNearCapacity ? "orange" : "green"}
            variant="filled"
            size="lg"
          >
            {capacity.checkedInCount} / {capacity.totalCapacity}
          </Badge>
        </Group>

        <Progress
          value={capacityPercentage}
          color={isAtCapacity ? "red" : isNearCapacity ? "orange" : "green"}
          size="xl"
          radius="md"
          style={{
            '& .mantine-Progress-bar': {
              transition: 'width 0.3s ease'
            }
          }}
        />

        <Group justify="space-between">
          <Stack gap="xs" align="center">
            <Text size="xs" c="dimmed" ta="center">Available</Text>
            <Text size="lg" fw={700} c={isAtCapacity ? "red" : "green"}>
              {capacity.availableSpots}
            </Text>
          </Stack>

          {capacity.waitlistCount > 0 && (
            <Stack gap="xs" align="center">
              <Text size="xs" c="dimmed" ta="center">Waitlist</Text>
              <Text size="lg" fw={700} c="orange">
                {capacity.waitlistCount}
              </Text>
            </Stack>
          )}

          <Stack gap="xs" align="center">
            <Text size="xs" c="dimmed" ta="center">Percentage</Text>
            <Text size="lg" fw={700}>
              {Math.round(capacityPercentage)}%
            </Text>
          </Stack>
        </Group>

        {isAtCapacity && (
          <Alert color="red" variant="light" size="sm">
            <Group align="center" gap="xs">
              <IconAlertCircle size={16} />
              <Text size="sm">Event at capacity</Text>
            </Group>
          </Alert>
        )}
      </Stack>
    </Card>
  );
}

/**
 * Event status and info card
 */
function EventInfoCard({ dashboard }: { dashboard: CheckInDashboard }) {
  const statusConfig = EVENT_STATUS_CONFIGS[dashboard.eventStatus];
  const eventDate = new Date(dashboard.eventDate);

  return (
    <Card shadow="sm" padding="lg" radius="md">
      <Stack gap="md">
        <Group justify="space-between" align="flex-start">
          <Stack gap="xs" style={{ flex: 1 }}>
            <Text 
              fw={700} 
              size="lg"
              style={{ 
                fontFamily: 'Bodoni Moda, serif',
                lineHeight: 1.2
              }}
              truncate
            >
              {dashboard.eventTitle}
            </Text>
            
            <Group align="center" gap="xs">
              <IconClock size={16} />
              <Text size="sm" c="dimmed">
                {eventDate.toLocaleDateString()} at {eventDate.toLocaleTimeString()}
              </Text>
            </Group>
          </Stack>

          <Badge 
            color={statusConfig.color}
            variant="filled"
            size="lg"
          >
            {statusConfig.label}
          </Badge>
        </Group>

        <Divider />

        <Group justify="space-between" align="center">
          <Text size="sm" fw={500}>Event ID:</Text>
          <Text size="sm" style={{ fontFamily: 'monospace' }}>
            {dashboard.eventId.split('-')[0]}
          </Text>
        </Group>
      </Stack>
    </Card>
  );
}

/**
 * Recent check-ins activity feed
 */
function RecentActivity({ recentCheckIns }: { recentCheckIns: RecentCheckIn[] }) {
  if (recentCheckIns.length === 0) {
    return (
      <Card shadow="sm" padding="lg" radius="md" h="300px">
        <Center h="100%">
          <Stack align="center" gap="md">
            <Text size="lg" c="dimmed">No recent check-ins</Text>
            <Text size="sm" c="dimmed" ta="center">
              Recent attendee check-ins will appear here
            </Text>
          </Stack>
        </Center>
      </Card>
    );
  }

  return (
    <Card shadow="sm" padding="lg" radius="md">
      <Stack gap="md">
        <Group justify="space-between" align="center">
          <Text fw={600} size="lg" style={{ fontFamily: 'Montserrat, sans-serif' }}>
            Recent Check-Ins
          </Text>
          <Badge variant="outline" size="sm">
            {recentCheckIns.length}
          </Badge>
        </Group>

        <ScrollArea.Autosize mah="250px">
          <Stack gap="xs">
            {recentCheckIns.map((checkIn, index) => {
              const checkInTime = new Date(checkIn.checkInTime);
              return (
                <Group
                  key={`${checkIn.attendeeId}-${checkIn.checkInTime}`}
                  justify="space-between"
                  p="sm"
                  style={{
                    backgroundColor: index % 2 === 0 ? '#f8f9fa' : 'transparent',
                    borderRadius: '8px'
                  }}
                >
                  <Stack gap="xs" style={{ flex: 1 }}>
                    <Group align="center" gap="xs">
                      <Text fw={500} size="sm">
                        {checkIn.sceneName}
                      </Text>
                      {checkIn.isManualEntry && (
                        <Badge size="xs" color="blue" variant="outline">
                          Walk-in
                        </Badge>
                      )}
                    </Group>
                    <Text size="xs" c="dimmed">
                      by {checkIn.staffMemberName}
                    </Text>
                  </Stack>
                  
                  <Text size="xs" c="dimmed" ta="right">
                    {checkInTime.toLocaleTimeString()}
                  </Text>
                </Group>
              );
            })}
          </Stack>
        </ScrollArea.Autosize>
      </Stack>
    </Card>
  );
}

/**
 * Sync status and offline indicator
 */
function SyncStatusCard() {
  const { isOnline, pendingCount, lastSync } = useOfflineSync();

  return (
    <Card shadow="sm" padding="md" radius="md">
      <Group justify="space-between" align="center">
        <Group align="center" gap="xs">
          {isOnline ? (
            <IconWifi size={20} color="green" />
          ) : (
            <IconWifiOff size={20} color="red" />
          )}
          <Stack gap="xs">
            <Text size="sm" fw={500}>
              {isOnline ? 'Online' : 'Offline'}
            </Text>
            {pendingCount > 0 && (
              <Text size="xs" c="orange">
                {pendingCount} pending
              </Text>
            )}
          </Stack>
        </Group>

        {lastSync && (
          <Text size="xs" c="dimmed">
            Last sync: {new Date(lastSync).toLocaleTimeString()}
          </Text>
        )}
      </Group>
    </Card>
  );
}

/**
 * Main dashboard component with real-time updates
 */
export function CheckInDashboard({
  dashboard,
  isLoading = false,
  error = null,
  onRefresh,
  onExport,
  canExport = false
}: CheckInDashboardProps) {
  if (error) {
    return (
      <Alert color="red" title="Error Loading Dashboard">
        <Stack gap="md">
          <Text>{error}</Text>
          <Button onClick={onRefresh} variant="outline" size="sm">
            Try Again
          </Button>
        </Stack>
      </Alert>
    );
  }

  if (isLoading && !dashboard) {
    return (
      <Center py="xl">
        <Stack align="center" gap="md">
          <Loader size="lg" />
          <Text size="sm" c="dimmed">Loading dashboard...</Text>
        </Stack>
      </Center>
    );
  }

  if (!dashboard) {
    return (
      <Center py="xl">
        <Stack align="center" gap="md">
          <Text size="lg" c="dimmed">No dashboard data</Text>
          <Button onClick={onRefresh} variant="outline">
            Refresh
          </Button>
        </Stack>
      </Center>
    );
  }

  return (
    <Stack gap="lg">
      {/* Header Actions */}
      <Group justify="space-between" align="center">
        <Text 
          size="xl" 
          fw={700}
          style={{ fontFamily: 'Bodoni Moda, serif' }}
        >
          Event Dashboard
        </Text>
        
        <Group align="center" gap="sm">
          <ActionIcon
            variant="subtle"
            onClick={onRefresh}
            loading={isLoading}
            size="lg"
            aria-label="Refresh dashboard"
            style={{
              minWidth: TOUCH_TARGETS.MINIMUM,
              minHeight: TOUCH_TARGETS.MINIMUM
            }}
          >
            <IconRefresh size={20} />
          </ActionIcon>

          {canExport && onExport && (
            <Button
              variant="outline"
              size="sm"
              onClick={onExport}
              leftSection={<IconDownload size={16} />}
              style={{
                borderRadius: '8px',
                fontFamily: 'Montserrat, sans-serif'
              }}
            >
              Export
            </Button>
          )}
        </Group>
      </Group>

      {/* Main Dashboard Grid */}
      <Grid gutter="lg">
        {/* Event Info - Full width on mobile */}
        <Grid.Col span={{ base: 12, md: 6 }}>
          <EventInfoCard dashboard={dashboard} />
        </Grid.Col>

        {/* Sync Status - Half width on mobile */}
        <Grid.Col span={{ base: 12, md: 6 }}>
          <SyncStatusCard />
        </Grid.Col>

        {/* Statistics - Full width */}
        <Grid.Col span={12}>
          <StatisticsCard capacity={dashboard.capacity} />
        </Grid.Col>

        {/* Recent Activity - Full width */}
        <Grid.Col span={12}>
          <RecentActivity recentCheckIns={dashboard.recentCheckIns} />
        </Grid.Col>
      </Grid>
    </Stack>
  );
}