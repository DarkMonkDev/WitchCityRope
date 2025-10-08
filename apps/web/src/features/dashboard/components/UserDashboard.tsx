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
  Tooltip,
  Button
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
          icon={<IconAlertCircle />}
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

          {!dashboard.hasVettingApplication ? (
            // User has NOT submitted a vetting application yet
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
              <Stack gap="sm">
                <Text size="sm" fw={500}>
                  Ready to join our community?
                </Text>
                <Text size="sm">
                  Submit a vetting application to gain full access to community events and resources.
                </Text>
                <Group gap="md">
                  <Button
                    component="a"
                    href="/join"
                    color="blue"
                    styles={{
                      root: {
                        fontWeight: 600,
                        height: '44px',
                        paddingTop: '12px',
                        paddingBottom: '12px',
                        fontSize: '14px',
                        lineHeight: '1.2'
                      }
                    }}
                  >
                    Submit Vetting Application
                  </Button>
                </Group>
              </Stack>
            </Alert>
          ) : (
            // User has a vetting application
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
              <Stack gap="xs">
                <Group gap="sm" align="center">
                  <Badge color={vettingDisplay.color} variant="filled" size="sm">
                    {vettingDisplay.label}
                  </Badge>
                  <Text size="sm" fw={500}>
                    {vettingDisplay.description}
                  </Text>
                </Group>

                {/* Next steps based on status */}
                {dashboard.vettingStatus === 'UnderReview' && (
                  <Text size="xs" c="dimmed" mt="xs">
                    📋 Your application is being reviewed by our team. You'll receive an email within 1-2 weeks about next steps.
                  </Text>
                )}

                {dashboard.vettingStatus === 'InterviewApproved' && (
                  <Text size="xs" c="dimmed" mt="xs">
                    📅 Great news! You've been approved for an interview. Check your email for scheduling instructions.
                  </Text>
                )}

                {(dashboard.vettingStatus as string) === 'InterviewCompleted' && (
                  <Text size="xs" c="dimmed" mt="xs">
                    ✅ Your interview is complete. Your application is being reviewed. We'll notify you of our decision soon!
                  </Text>
                )}

                {dashboard.vettingStatus === 'FinalReview' && (
                  <Text size="xs" c="dimmed" mt="xs">
                    🔍 Your application is in final review. You'll hear from us soon!
                  </Text>
                )}

                {dashboard.vettingStatus === 'Approved' && (
                  <Text size="xs" c="dimmed" mt="xs">
                    🎉 You have full access to all community events and resources. Welcome!
                  </Text>
                )}

                {dashboard.vettingStatus === 'Denied' && (
                  <Text size="xs" c="red" mt="xs" fw={500}>
                    ⚠️ Your application was not approved at this time. You have limited access to community events.
                  </Text>
                )}

                {dashboard.vettingStatus === 'OnHold' && (
                  <Text size="xs" c="yellow" mt="xs" fw={500}>
                    ⏸️ Your application is on hold. Please check your email or contact an administrator for more information.
                  </Text>
                )}
              </Stack>
            </Alert>
          )}
        </Box>
      </Stack>
    </Card>
  );
};

export default UserDashboard;