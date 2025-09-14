// MembershipStatistics Component
// Displays user's membership statistics and attendance metrics

import React from 'react';
import {
  Card,
  Group,
  Stack,
  Text,
  Skeleton,
  Alert,
  ActionIcon,
  Tooltip,
  SimpleGrid,
  Paper,
  RingProgress,
  Badge,
  Box,
  Progress,
  Divider
} from '@mantine/core';
import {
  IconChartPie,
  IconCalendarStats,
  IconTrophy,
  IconClock,
  IconRefresh,
  IconAlertCircle,
  IconCalendarEvent,
  IconCalendarX,
  IconShieldCheck,
  IconCalendarCheck
} from '@tabler/icons-react';
import { useUserStatistics, useDashboardError } from '../hooks/useDashboard';
import { DashboardUtils } from '../types/dashboard.types';

/**
 * MembershipStatistics Component Props
 */
interface MembershipStatisticsProps {
  /** Optional className for styling */
  className?: string;
}

/**
 * Stat Card Component
 * Individual card for each statistic
 */
interface StatCardProps {
  icon: React.ReactNode;
  title: string;
  value: string | number;
  subtitle?: string;
  color: string;
  children?: React.ReactNode;
}

const StatCard: React.FC<StatCardProps> = ({ 
  icon, 
  title, 
  value, 
  subtitle, 
  color, 
  children 
}) => {
  return (
    <Paper p="md" withBorder style={{ height: '100%' }}>
      <Stack gap="sm">
        <Group gap="sm" align="center">
          <Box style={{ color: `var(--mantine-color-${color}-6)` }}>
            {icon}
          </Box>
          <Text size="sm" c="dimmed" fw={500}>
            {title}
          </Text>
        </Group>
        
        <Text size="xl" fw={700} c="dark">
          {value}
        </Text>
        
        {subtitle && (
          <Text size="xs" c="dimmed">
            {subtitle}
          </Text>
        )}
        
        {children}
      </Stack>
    </Paper>
  );
};

/**
 * MembershipStatistics Component
 * Displays comprehensive membership statistics
 */
export const MembershipStatistics: React.FC<MembershipStatisticsProps> = ({ className }) => {
  const { data: statistics, isLoading, error, refetch } = useUserStatistics();
  const dashboardError = useDashboardError(error);

  // Loading state
  if (isLoading) {
    return (
      <Card shadow="sm" padding="lg" className={className}>
        <Stack gap="md">
          <Group gap="sm" align="center">
            <IconChartPie size={20} color="#9b4a75" />
            <Text fw={600} size="lg">
              Membership Statistics
            </Text>
            <Skeleton height={20} width={20} circle />
          </Group>
          
          <SimpleGrid cols={{ base: 2, sm: 4 }} spacing="md">
            {Array.from({ length: 4 }).map((_, index) => (
              <Paper key={index} p="md" withBorder>
                <Stack gap="sm">
                  <Skeleton height={20} width="80%" />
                  <Skeleton height={28} width="60%" />
                  <Skeleton height={14} width="90%" />
                </Stack>
              </Paper>
            ))}
          </SimpleGrid>
        </Stack>
      </Card>
    );
  }

  // Error state
  if (error) {
    return (
      <Card shadow="sm" padding="lg" className={className}>
        <Stack gap="md">
          <Group gap="sm" align="center">
            <IconChartPie size={20} color="#9b4a75" />
            <Text fw={600} size="lg">
              Membership Statistics
            </Text>
          </Group>
          
          <Alert
            icon={<IconAlertCircle />}
            color="red"
            title="Unable to Load Statistics"
          >
            <Group gap="md" mt="sm">
              <Text size="sm">
                {dashboardError?.message || 'Failed to load your membership statistics.'}
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
        </Stack>
      </Card>
    );
  }

  if (!statistics) return null;

  const membershipDuration = DashboardUtils.formatMembershipDuration(statistics.monthsAsMember);
  const joinDate = DashboardUtils.formatDate(statistics.joinDate);
  const vettingDisplay = DashboardUtils.getVettingStatusDisplay(statistics.vettingStatus);
  
  // Calculate activity level based on events attended vs months as member
  const averageEventsPerMonth = statistics.monthsAsMember > 0 ? 
    Math.round((statistics.eventsAttended / statistics.monthsAsMember) * 10) / 10 : 0;
  
  // Calculate engagement ring progress (based on recent activity)
  const engagementScore = Math.min(100, (statistics.recentEvents / 6) * 100); // Max 6 events in 6 months = 100%

  return (
    <Card shadow="sm" padding="lg" className={className}>
      <Stack gap="md">
        {/* Header */}
        <Group gap="sm" align="center">
          <IconChartPie size={20} color="#9b4a75" />
          <Text fw={600} size="lg" style={{ flex: 1 }}>
            Your WitchCityRope Journey
          </Text>
          
          <Tooltip label="Refresh statistics">
            <ActionIcon
              variant="subtle"
              size="sm"
              onClick={() => refetch()}
            >
              <IconRefresh size={14} />
            </ActionIcon>
          </Tooltip>
        </Group>

        {/* Main statistics grid */}
        <SimpleGrid cols={{ base: 2, sm: 4 }} spacing="md">
          <StatCard
            icon={<IconTrophy size={20} />}
            title="Events Attended"
            value={statistics.eventsAttended}
            subtitle="Total events completed"
            color="green"
          />
          
          <StatCard
            icon={<IconClock size={20} />}
            title="Member Duration"
            value={membershipDuration}
            subtitle={`Since ${joinDate}`}
            color="blue"
          />
          
          <StatCard
            icon={<IconCalendarStats size={20} />}
            title="Recent Activity"
            value={statistics.recentEvents}
            subtitle="Events in last 6 months"
            color="orange"
          />
          
          <StatCard
            icon={<IconCalendarEvent size={20} />}
            title="Upcoming Events"
            value={statistics.upcomingRegistrations}
            subtitle="Events registered for"
            color="teal"
          />
        </SimpleGrid>

        <Divider />

        {/* Activity overview */}
        <Box>
          <Text size="sm" fw={500} c="dark" mb="md">
            Activity Overview
          </Text>
          
          <Group gap="xl" align="flex-start">
            {/* Engagement ring */}
            <Box style={{ textAlign: 'center' }}>
              <RingProgress
                size={120}
                thickness={8}
                sections={[
                  { 
                    value: engagementScore, 
                    color: engagementScore > 75 ? 'green' : 
                           engagementScore > 50 ? 'orange' : 
                           engagementScore > 25 ? 'yellow' : 'red'
                  }
                ]}
                label={
                  <Text size="xs" ta="center" px="xs">
                    Recent Activity
                  </Text>
                }
              />
              <Text size="xs" c="dimmed" mt="xs">
                {statistics.recentEvents} events in 6 months
              </Text>
            </Box>
            
            {/* Statistics summary */}
            <Stack gap="sm" style={{ flex: 1 }}>
              <Group gap="sm" align="center">
                <IconCalendarCheck size={16} color="var(--mantine-color-green-6)" />
                <Text size="sm">
                  Average of <strong>{averageEventsPerMonth}</strong> events per month
                </Text>
              </Group>
              
              {statistics.cancelledRegistrations > 0 && (
                <Group gap="sm" align="center">
                  <IconCalendarX size={16} color="var(--mantine-color-red-6)" />
                  <Text size="sm">
                    <strong>{statistics.cancelledRegistrations}</strong> cancelled registration{statistics.cancelledRegistrations !== 1 ? 's' : ''}
                  </Text>
                </Group>
              )}
              
              <Group gap="sm" align="center">
                <IconShieldCheck size={16} color={`var(--mantine-color-${vettingDisplay.color}-6)`} />
                <Text size="sm">
                  Vetting Status: <strong>{vettingDisplay.label}</strong>
                </Text>
              </Group>
              
              {statistics.nextInterviewDate && (
                <Alert color="teal" variant="light">
                  <Text size="sm">
                    📅 Interview scheduled: <strong>
                      {DashboardUtils.formatDate(statistics.nextInterviewDate)}
                    </strong>
                  </Text>
                </Alert>
              )}
            </Stack>
          </Group>
        </Box>

        {/* Engagement encouragement */}
        {statistics.eventsAttended === 0 ? (
          <Alert color="blue" variant="light">
            <Text size="sm">
              🎯 Ready to get started? Check out our upcoming events and register for your first workshop!
            </Text>
          </Alert>
        ) : averageEventsPerMonth < 0.5 ? (
          <Alert color="orange" variant="light">
            <Text size="sm">
              💪 Consider joining more events to get the most out of your membership!
            </Text>
          </Alert>
        ) : (
          <Alert color="green" variant="light">
            <Text size="sm">
              🎉 Great engagement! You're actively participating in the community.
            </Text>
          </Alert>
        )}
      </Stack>
    </Card>
  );
};

export default MembershipStatistics;