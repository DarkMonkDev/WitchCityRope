// UserDashboard Component
// Displays user profile information, vetting status, and quick stats

import React from 'react';
import {
  Card,
  Group,
  Stack,
  Text,
  Badge,
  Avatar,
  Skeleton,
  Alert,
  SimpleGrid,
  Divider,
  Box,
  ActionIcon,
  Tooltip
} from '@mantine/core';
import {
  IconUser,
  IconCalendarEvent,
  IconShield,
  IconMail,
  IconRefresh,
  IconAlertCircle
} from '@tabler/icons-react';
import { useUserDashboard, useDashboardError } from '../hooks/useDashboard';
import { DashboardUtils } from '../types/dashboard.types';

/**
 * UserDashboard Component Props
 */
interface UserDashboardProps {
  /** Optional className for styling */
  className?: string;
}

/**
 * UserDashboard Component
 * Displays welcome message, vetting status, and basic profile info
 */
export const UserDashboard: React.FC<UserDashboardProps> = ({ className }) => {
  const { data: dashboard, isLoading, error, refetch } = useUserDashboard();
  const dashboardError = useDashboardError(error);

  // Loading state
  if (isLoading) {
    return (
      <Card shadow="sm" padding="lg" className={className}>
        <Stack gap="md">
          <Group gap="md">
            <Skeleton height={60} circle />
            <Stack gap="xs" style={{ flex: 1 }}>
              <Skeleton height={24} width="40%" />
              <Skeleton height={16} width="60%" />
            </Stack>
          </Group>
          
          <Divider />
          
          <SimpleGrid cols={2} spacing="md">
            <Skeleton height={80} />
            <Skeleton height={80} />
          </SimpleGrid>
        </Stack>
      </Card>
    );
  }

  // Error state
  if (error || !dashboard) {
    return (
      <Card shadow="sm" padding="lg" className={className}>
        <Alert
          icon={<IconAlertCircle size={16} />}
          color="red"
          title="Unable to Load Dashboard"
        >
          <Group gap="md" mt="sm">
            <Text size="sm">
              {dashboardError?.message || 'Failed to load your dashboard information.'}
            </Text>
            <ActionIcon
              variant="light"
              color="blue"
              onClick={() => refetch()}
            >
              <IconRefresh size={16} />
            </ActionIcon>
          </Group>
        </Alert>
      </Card>
    );
  }

  const vettingDisplay = DashboardUtils.getVettingStatusDisplay(dashboard.vettingStatus);
  const memberSince = DashboardUtils.formatDate(dashboard.joinDate);

  return (
    <Card shadow="sm" padding="lg" className={className}>
      <Stack gap="md">
        {/* Header with user info */}
        <Group gap="md" align="flex-start">
          <Avatar
            size={60}
            radius="md"
            color="wcr"
            style={{ backgroundColor: '#9b4a75' }}
          >
            <IconUser size={28} />
          </Avatar>
          
          <Stack gap="xs" style={{ flex: 1 }}>
            <Group gap="sm" align="center">
              <Text size="xl" fw={700} c="dark">
                Welcome back, {dashboard.sceneName}!
              </Text>
              
              <Tooltip label="Refresh dashboard">
                <ActionIcon
                  variant="subtle"
                  size="sm"
                  onClick={() => refetch()}
                >
                  <IconRefresh size={14} />
                </ActionIcon>
              </Tooltip>
            </Group>
            
            <Group gap="sm">
              <Badge
                color="blue"
                variant="light"
                leftSection={<IconShield size={12} />}
                size="sm"
              >
                {dashboard.role}
              </Badge>
              
              <Badge
                color={vettingDisplay.color}
                variant="light"
                size="sm"
              >
                {vettingDisplay.label}
              </Badge>
            </Group>
            
            {dashboard.pronouns && (
              <Text size="sm" c="dimmed">
                Pronouns: {dashboard.pronouns}
              </Text>
            )}
          </Stack>
        </Group>

        <Divider />

        {/* Quick info grid */}
        <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md">
          <Box>
            <Group gap="xs" mb="xs">
              <IconMail size={16} color="#9b4a75" />
              <Text size="sm" fw={500} c="dark">
                Email
              </Text>
            </Group>
            <Text size="sm" c="dimmed">
              {dashboard.email}
            </Text>
          </Box>

          <Box>
            <Group gap="xs" mb="xs">
              <IconCalendarEvent size={16} color="#9b4a75" />
              <Text size="sm" fw={500} c="dark">
                Member Since
              </Text>
            </Group>
            <Text size="sm" c="dimmed">
              {memberSince}
            </Text>
          </Box>
        </SimpleGrid>

        {/* Vetting status info */}
        <Box>
          <Text size="sm" fw={500} c="dark" mb="xs">
            Vetting Status
          </Text>
          <Alert
            color={vettingDisplay.color}
            variant="light"
            styles={{
              root: {
                backgroundColor: 'var(--mantine-color-gray-0)',
                border: `1px solid var(--mantine-color-${vettingDisplay.color}-3)`,
              }
            }}
          >
            <Group gap="sm" align="center">
              <Badge color={vettingDisplay.color} variant="filled" size="sm">
                {vettingDisplay.label}
              </Badge>
              <Text size="sm">
                {vettingDisplay.description}
              </Text>
            </Group>
            
            {dashboard.isVetted && (
              <Text size="xs" c="dimmed" mt="xs">
                🎉 You have full access to all community events and resources.
              </Text>
            )}
          </Alert>
        </Box>
      </Stack>
    </Card>
  );
};

export default UserDashboard;