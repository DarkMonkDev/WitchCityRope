// Reviewer dashboard component for vetting team
import React, { useState } from 'react';
import {
  Box,
  Paper,
  SimpleGrid,
  Text,
  Group,
  Button,
  TextInput,
  Select,
  DatePickerInput,
  Stack,
  Badge,
  ActionIcon,
  Menu,
  Alert,
  LoadingOverlay,
  Pagination,
  Title,
  Card,
  ThemeIcon,
  Progress,
  Divider
} from '@mantine/core';
import {
  IconSearch,
  IconFilter,
  IconEye,
  IconUserCheck,
  IconClock,
  IconAlertTriangle,
  IconRefresh,
  IconDownload,
  IconDots,
  IconUser,
  IconMail,
  IconCalendar
} from '@tabler/icons-react';
import { useReviewerDashboard } from '../../hooks/useReviewerDashboard';
import type { ApplicationSummaryDto, DashboardFilters } from '../../types/vetting.types';
import { APPLICATION_STATUS_CONFIGS, TOUCH_TARGETS } from '../../types/vetting.types';

interface ReviewerDashboardProps {
  onApplicationSelect?: (application: ApplicationSummaryDto) => void;
  className?: string;
}

export const ReviewerDashboard: React.FC<ReviewerDashboardProps> = ({
  onApplicationSelect,
  className
}) => {
  const [selectedApplications, setSelectedApplications] = useState<string[]>([]);
  const [showFilters, setShowFilters] = useState(false);

  const {
    applications,
    stats,
    pagination,
    filters,
    updateFilters,
    resetFilters,
    isLoading,
    quickActions,
    refetchDashboard,
    getFilteredStats
  } = useReviewerDashboard();

  // Statistics cards configuration
  const statsCards = [
    {
      title: 'New Applications',
      value: stats?.newApplications || 0,
      color: 'blue',
      icon: IconMail,
      description: 'Awaiting initial review'
    },
    {
      title: 'In Review',
      value: stats?.inReview || 0,
      color: 'yellow',
      icon: IconEye,
      description: 'Currently being reviewed'
    },
    {
      title: 'Pending References',
      value: stats?.awaitingReferences || 0,
      color: 'orange',
      icon: IconClock,
      description: 'Waiting for reference responses'
    },
    {
      title: 'Average Review Time',
      value: stats?.averageReviewTime ? `${stats.averageReviewTime.toFixed(1)}h` : 'N/A',
      color: 'green',
      icon: IconUserCheck,
      description: 'Hours per application'
    }
  ];

  // Application card component
  const ApplicationCard = ({ application }: { application: ApplicationSummaryDto }) => {
    const statusConfig = APPLICATION_STATUS_CONFIGS[application.status];
    const daysOld = Math.floor((new Date().getTime() - new Date(application.submittedAt).getTime()) / (1000 * 60 * 60 * 24));

    return (
      <Card
        padding="lg"
        shadow="sm"
        withBorder
        style={{
          cursor: 'pointer',
          transition: 'all 0.2s ease',
          minHeight: TOUCH_TARGETS.CARD_MIN_HEIGHT
        }}
        styles={(theme) => ({
          root: {
            '&:hover': {
              transform: 'translateY(-2px)',
              boxShadow: '0 8px 24px rgba(136, 1, 36, 0.15)'
            }
          }
        })}
        onClick={() => onApplicationSelect?.(application)}
      >
        <Group position="apart" align="flex-start" mb="md">
          <Box style={{ flex: 1 }}>
            <Group spacing="xs" align="center" mb={4}>
              <Text weight={600} size="md" c="wcr.8">
                {application.isAnonymous ? 'Anonymous Applicant' : application.sceneName}
              </Text>
              {application.hasRecentNotes && (
                <Badge size="xs" color="blue" variant="dot">
                  Recent Notes
                </Badge>
              )}
            </Group>
            
            <Text size="sm" c="dimmed" mb="xs">
              Application #{application.applicationNumber}
            </Text>
            
            <Group spacing="xs" mb="xs">
              <Badge
                color={statusConfig?.color || 'gray'}
                variant="light"
                size="sm"
              >
                {statusConfig?.label || application.status}
              </Badge>
              
              <Badge color="gray" variant="outline" size="sm">
                {application.experienceLevel}
              </Badge>
              
              <Badge color="gray" variant="outline" size="sm">
                {application.yearsExperience} years
              </Badge>
            </Group>

            <Text size="xs" c="dimmed">
              Submitted {daysOld} day{daysOld !== 1 ? 's' : ''} ago
              {application.assignedReviewerName && (
                <> • Assigned to {application.assignedReviewerName}</>
              )}
            </Text>
          </Box>

          <Menu shadow="md" width={200}>
            <Menu.Target>
              <ActionIcon variant="light" color="wcr.7">
                <IconDots size={16} />
              </ActionIcon>
            </Menu.Target>

            <Menu.Dropdown>
              <Menu.Item
                icon={<IconEye size={14} />}
                onClick={(e) => {
                  e.stopPropagation();
                  onApplicationSelect?.(application);
                }}
              >
                View Details
              </Menu.Item>
              
              <Menu.Item
                icon={<IconUserCheck size={14} />}
                onClick={(e) => {
                  e.stopPropagation();
                  // quickActions.assignToMe(application.id, currentUserId);
                }}
              >
                Assign to Me
              </Menu.Item>
              
              <Menu.Divider />
              
              <Menu.Item
                icon={<IconMail size={14} />}
                onClick={(e) => {
                  e.stopPropagation();
                  quickActions.requestInfo(application.id, 'Additional information requested');
                }}
              >
                Request Info
              </Menu.Item>
            </Menu.Dropdown>
          </Menu>
        </Group>

        {/* Reference Status */}
        <Box>
          <Group position="apart" mb="xs">
            <Text size="xs" weight={500} c="dimmed">
              References
            </Text>
            <Text size="xs" c="dimmed">
              {application.referenceStatus.respondedReferences}/{application.referenceStatus.totalReferences} responded
            </Text>
          </Group>
          
          <Progress
            value={(application.referenceStatus.respondedReferences / application.referenceStatus.totalReferences) * 100}
            color={application.referenceStatus.allReferencesComplete ? 'green' : 'orange'}
            size="xs"
            mb="xs"
          />
        </Box>

        {/* Priority Indicators */}
        {application.priority > 1 && (
          <Alert
            color="red"
            variant="light"
            size="sm"
            icon={<IconAlertTriangle size={14} />}
            styles={{ root: { padding: '8px 12px' } }}
          >
            High Priority
          </Alert>
        )}

        {/* Pending Actions */}
        {application.hasPendingActions && (
          <Badge color="orange" variant="filled" size="xs" mt="xs">
            Action Needed
          </Badge>
        )}
      </Card>
    );
  };

  return (
    <Box className={className} pos="relative">
      <LoadingOverlay visible={isLoading} />

      {/* Header */}
      <Group position="apart" mb="xl">
        <Title order={1} c="wcr.7">
          Vetting Dashboard
        </Title>
        
        <Group>
          <Button
            leftIcon={<IconRefresh size={16} />}
            variant="light"
            onClick={refetchDashboard}
          >
            Refresh
          </Button>
          
          <Button
            leftIcon={<IconDownload size={16} />}
            variant="outline"
            color="wcr.7"
          >
            Export
          </Button>
        </Group>
      </Group>

      {/* Statistics Cards */}
      <SimpleGrid cols={4} breakpoints={[{ maxWidth: 'md', cols: 2 }, { maxWidth: 'sm', cols: 1 }]} mb="xl">
        {statsCards.map((stat) => (
          <Paper key={stat.title} p="lg" withBorder>
            <Group position="apart">
              <Box>
                <Text size="xs" color="dimmed" transform="uppercase" weight={600}>
                  {stat.title}
                </Text>
                <Text size="xl" weight={700} c="wcr.8" mt="xs">
                  {stat.value}
                </Text>
                <Text size="xs" c="dimmed" mt={4}>
                  {stat.description}
                </Text>
              </Box>
              <ThemeIcon color={stat.color} variant="light" size="lg">
                <stat.icon size={20} />
              </ThemeIcon>
            </Group>
          </Paper>
        ))}
      </SimpleGrid>

      {/* Filters Section */}
      <Paper p="md" withBorder mb="lg">
        <Group position="apart" mb={showFilters ? 'md' : 0}>
          <Group>
            <TextInput
              placeholder="Search applications..."
              leftIcon={<IconSearch size={16} />}
              value={filters.search}
              onChange={(e) => updateFilters({ search: e.target.value })}
              style={{ minWidth: 250 }}
            />
            
            <Select
              placeholder="All statuses"
              value={filters.status}
              onChange={(value) => updateFilters({ status: value as any })}
              data={[
                { value: 'all', label: 'All Statuses' },
                { value: 'submitted', label: 'New Applications' },
                { value: 'under-review', label: 'Under Review' },
                { value: 'pending-interview', label: 'Pending Interview' },
                { value: 'references-contacted', label: 'Awaiting References' }
              ]}
              style={{ minWidth: 150 }}
            />

            <Select
              placeholder="Assignment"
              value={filters.assignedTo}
              onChange={(value) => updateFilters({ assignedTo: value as any })}
              data={[
                { value: 'all', label: 'All Applications' },
                { value: 'mine', label: 'My Applications' },
                { value: 'unassigned', label: 'Unassigned' }
              ]}
              style={{ minWidth: 130 }}
            />
          </Group>

          <Group>
            <Button
              variant="light"
              leftIcon={<IconFilter size={16} />}
              onClick={() => setShowFilters(!showFilters)}
            >
              More Filters
            </Button>
            
            {(filters.search || filters.status !== 'all' || filters.assignedTo !== 'all') && (
              <Button variant="subtle" onClick={resetFilters} size="sm">
                Clear Filters
              </Button>
            )}
          </Group>
        </Group>

        {/* Extended Filters */}
        {showFilters && (
          <>
            <Divider mb="md" />
            <SimpleGrid cols={3} breakpoints={[{ maxWidth: 'md', cols: 2 }, { maxWidth: 'sm', cols: 1 }]}>
              <Select
                label="Date Range"
                placeholder="Select range"
                value={filters.dateRange}
                onChange={(value) => updateFilters({ dateRange: value as any })}
                data={[
                  { value: 'last7', label: 'Last 7 days' },
                  { value: 'last30', label: 'Last 30 days' },
                  { value: 'custom', label: 'Custom range' }
                ]}
              />

              {filters.dateRange === 'custom' && (
                <>
                  <DatePickerInput
                    label="From Date"
                    placeholder="Select start date"
                    value={filters.customDateFrom ? new Date(filters.customDateFrom) : null}
                    onChange={(date) => updateFilters({ 
                      customDateFrom: date?.toISOString().split('T')[0] 
                    })}
                  />
                  
                  <DatePickerInput
                    label="To Date"
                    placeholder="Select end date"
                    value={filters.customDateTo ? new Date(filters.customDateTo) : null}
                    onChange={(date) => updateFilters({ 
                      customDateTo: date?.toISOString().split('T')[0] 
                    })}
                  />
                </>
              )}
            </SimpleGrid>
          </>
        )}
      </Paper>

      {/* Applications Grid */}
      <SimpleGrid
        cols={3}
        breakpoints={[
          { maxWidth: 'lg', cols: 2 },
          { maxWidth: 'sm', cols: 1 }
        ]}
        mb="lg"
      >
        {applications.map((application) => (
          <ApplicationCard key={application.id} application={application} />
        ))}
      </SimpleGrid>

      {/* Empty State */}
      {applications.length === 0 && !isLoading && (
        <Paper p="xl" withBorder ta="center">
          <Stack align="center" spacing="md">
            <ThemeIcon size={64} color="gray" variant="light">
              <IconUser size={32} />
            </ThemeIcon>
            <Text size="lg" weight={500} c="dimmed">
              No applications found
            </Text>
            <Text size="sm" c="dimmed">
              Try adjusting your filters or check back later for new applications.
            </Text>
            <Button variant="light" onClick={resetFilters}>
              Clear All Filters
            </Button>
          </Stack>
        </Paper>
      )}

      {/* Pagination */}
      {pagination && pagination.totalPages > 1 && (
        <Group position="center" mt="xl">
          <Pagination
            total={pagination.totalPages}
            value={pagination.currentPage}
            onChange={(page) => updateFilters({ page })}
            color="wcr.7"
          />
        </Group>
      )}

      {/* Summary Footer */}
      <Group position="apart" mt="xl" pt="md" style={{ borderTop: '1px solid #e0e0e0' }}>
        <Text size="sm" c="dimmed">
          Showing {applications.length} of {pagination?.totalCount || 0} applications
        </Text>
        
        <Text size="sm" c="dimmed">
          {getFilteredStats().newApplications || 0} new this week
        </Text>
      </Group>
    </Box>
  );
};