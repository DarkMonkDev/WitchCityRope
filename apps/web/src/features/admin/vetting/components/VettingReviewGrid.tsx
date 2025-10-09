import React, { useState, useMemo, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Table,
  Text,
  Badge,
  Group,
  TextInput,
  Select,
  Button,
  Paper,
  Stack,
  Pagination,
  Box,
  Skeleton,
  Alert,
  ActionIcon,
  Menu,
  rem
} from '@mantine/core';
import {
  IconSearch,
  IconFilter,
  IconRefresh,
  IconX,
  IconDots,
  IconCheck,
  IconX as IconCancel,
  IconClock,
  IconCalendar,
  IconMail
} from '@tabler/icons-react';
import { useVettingApplications } from '../hooks/useVettingApplications';
import { VettingStatusBadge } from './VettingStatusBadge';
import type { ApplicationFilterRequest } from '../types/vetting.types';

interface VettingReviewGridProps {
  onViewDetails?: (applicationId: string) => void;
  onActionComplete?: () => void;
}

/**
 * Admin Vetting Review Grid Component
 *
 * Main landing page for admin vetting workflow. Displays all applications
 * in a filterable, sortable grid with quick actions.
 *
 * Features:
 * - Search by scene name/email
 * - Filter by status
 * - Sort by status and submitted date
 * - Pagination (25 items per page)
 * - Quick action menus
 * - Responsive design
 */
export const VettingReviewGrid: React.FC<VettingReviewGridProps> = ({
  onViewDetails,
  onActionComplete
}) => {
  const navigate = useNavigate();

  // Filter state
  const [filters, setFilters] = useState<ApplicationFilterRequest>({
    page: 1,
    pageSize: 25,
    statusFilters: [],
    priorityFilters: [],
    experienceLevelFilters: [],
    skillsFilters: [],
    searchQuery: '',
    sortBy: 'SubmittedAt',
    sortDirection: 'Desc'
  });

  // Fetch data
  const { data, isLoading, error, refetch } = useVettingApplications(filters);

  // Status filter options
  const statusOptions = useMemo(() => [
    { value: '', label: 'All Statuses' },
    { value: 'Draft', label: 'Draft' },
    { value: 'Submitted', label: 'Submitted' },
    { value: 'UnderReview', label: 'Under Review' },
    { value: 'InterviewApproved', label: 'Interview Approved' },
    { value: 'PendingInterview', label: 'Pending Interview' },
    { value: 'InterviewCompleted', label: 'Interview Completed' },
    { value: 'OnHold', label: 'On Hold' },
    { value: 'Approved', label: 'Approved' },
    { value: 'Denied', label: 'Denied' },
    { value: 'Withdrawn', label: 'Withdrawn' }
  ], []);

  // Filter handlers
  const handleSearchChange = useCallback((value: string) => {
    setFilters(prev => ({
      ...prev,
      searchQuery: value,
      page: 1 // Reset to first page on search
    }));
  }, []);

  const handleStatusFilterChange = useCallback((value: string | null) => {
    setFilters(prev => ({
      ...prev,
      statusFilters: value && value !== '' ? [value] : [],
      page: 1 // Reset to first page on filter
    }));
  }, []);

  const handleClearFilters = useCallback(() => {
    setFilters({
      page: 1,
      pageSize: 25,
      statusFilters: [],
      priorityFilters: [],
      experienceLevelFilters: [],
      skillsFilters: [],
      searchQuery: '',
      sortBy: 'SubmittedAt',
      sortDirection: 'Desc'
    });
  }, []);

  const handlePageChange = useCallback((page: number) => {
    setFilters(prev => ({ ...prev, page }));
  }, []);

  // Row click handler
  const handleRowClick = useCallback((applicationId: string) => {
    if (onViewDetails) {
      onViewDetails(applicationId);
    } else {
      navigate(`/admin/vetting/applications/${applicationId}`);
    }
  }, [navigate, onViewDetails]);

  // Quick action handlers (TODO: Implement modals)
  const handleApprove = useCallback((applicationId: string) => {
    // TODO: Open approval modal
  }, []);

  const handleDeny = useCallback((applicationId: string) => {
    // TODO: Open deny modal
  }, []);

  const handlePutOnHold = useCallback((applicationId: string) => {
    // TODO: Open on-hold modal
  }, []);

  const handleScheduleInterview = useCallback((applicationId: string) => {
    // TODO: Open schedule interview modal
  }, []);

  const handleSendReminder = useCallback((applicationId: string) => {
    // TODO: Open send reminder modal
  }, []);

  // Date formatter
  const formatDate = useCallback((dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }, []);

  // Calculate active filter count
  const activeFilterCount = useMemo(() => {
    let count = 0;
    if (filters.searchQuery) count++;
    if (filters.statusFilters && filters.statusFilters.length > 0) count++;
    return count;
  }, [filters.searchQuery, filters.statusFilters]);

  // Loading state
  if (isLoading && !data) {
    return (
      <Stack gap="md">
        <Paper p="md" radius="md">
          <Group gap="md">
            <Skeleton height={42} style={{ flex: 1 }} />
            <Skeleton height={42} width={200} />
          </Group>
        </Paper>
        <Paper p="md" radius="md">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} height={60} mb="sm" />
          ))}
        </Paper>
      </Stack>
    );
  }

  // Error state
  if (error) {
    return (
      <Alert
        color="red"
        title="Error Loading Applications"
        icon={<IconX />}
      >
        <Text>{error.message}</Text>
        <Button
          onClick={() => refetch()}
          leftSection={<IconRefresh size={16} />}
          mt="md"
          variant="light"
        >
          Retry
        </Button>
      </Alert>
    );
  }

  return (
    <Stack gap="md">
      {/* Filters Section */}
      <Paper p="md" radius="md" style={{ background: '#FFF8F0' }}>
        <Stack gap="md">
          <Group gap="md" align="flex-end" wrap="wrap">
            <TextInput
              placeholder="Search by scene name or email..."
              leftSection={<IconSearch size={16} />}
              value={filters.searchQuery || ''}
              onChange={(e) => handleSearchChange(e.currentTarget.value)}
              style={{ flex: 1, minWidth: rem(300) }}
              styles={{
                input: {
                  fontSize: '16px',
                  height: '42px'
                }
              }}
            />

            <Select
              placeholder="Filter by status"
              data={statusOptions}
              value={filters.statusFilters?.[0] || ''}
              onChange={handleStatusFilterChange}
              leftSection={<IconFilter size={16} />}
              clearable
              style={{ minWidth: rem(200) }}
              styles={{
                input: {
                  fontSize: '16px',
                  height: '42px'
                }
              }}
            />

            {activeFilterCount > 0 && (
              <Button
                variant="subtle"
                color="gray"
                onClick={handleClearFilters}
                leftSection={<IconX size={16} />}
              >
                Clear Filters ({activeFilterCount})
              </Button>
            )}
          </Group>
        </Stack>
      </Paper>

      {/* Data Grid */}
      <Paper shadow="sm" radius="md">
        <Table striped highlightOnHover>
          <Table.Thead
            style={{
              backgroundColor: '#880124',
              color: 'white'
            }}
          >
            <Table.Tr>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                  Scene Name
                </Text>
              </Table.Th>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                  Email
                </Text>
              </Table.Th>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                  Status
                </Text>
              </Table.Th>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                  Submitted Date
                </Text>
              </Table.Th>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none', width: 100 }}>
                <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                  Actions
                </Text>
              </Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {data?.items?.map((application) => (
              <Table.Tr
                key={application.id}
                onClick={() => handleRowClick(application.id!)}
                style={{
                  cursor: 'pointer'
                }}
              >
                <Table.Td>
                  <Text size="sm" fw={600} style={{ color: '#2B2B2B' }}>
                    {application.sceneName || 'Not provided'}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm" style={{ color: '#2B2B2B' }}>
                    {(application as any).email || 'Not provided'}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <VettingStatusBadge status={application.status || 'Unknown'} />
                </Table.Td>
                <Table.Td>
                  <Text size="sm" style={{ color: '#2B2B2B' }}>
                    {formatDate(application.submittedAt!)}
                  </Text>
                </Table.Td>
                <Table.Td onClick={(e) => e.stopPropagation()}>
                  <Menu shadow="md" width={200}>
                    <Menu.Target>
                      <ActionIcon variant="subtle" color="gray">
                        <IconDots size={16} />
                      </ActionIcon>
                    </Menu.Target>

                    <Menu.Dropdown>
                      <Menu.Label>Quick Actions</Menu.Label>
                      <Menu.Item
                        leftSection={<IconCheck size={14} />}
                        onClick={() => handleApprove(application.id!)}
                      >
                        Approve
                      </Menu.Item>
                      <Menu.Item
                        leftSection={<IconCancel size={14} />}
                        onClick={() => handleDeny(application.id!)}
                      >
                        Deny
                      </Menu.Item>
                      <Menu.Item
                        leftSection={<IconClock size={14} />}
                        onClick={() => handlePutOnHold(application.id!)}
                      >
                        Put On Hold
                      </Menu.Item>
                      <Menu.Divider />
                      <Menu.Item
                        leftSection={<IconCalendar size={14} />}
                        onClick={() => handleScheduleInterview(application.id!)}
                      >
                        Schedule Interview
                      </Menu.Item>
                      <Menu.Item
                        leftSection={<IconMail size={14} />}
                        onClick={() => handleSendReminder(application.id!)}
                      >
                        Send Reminder
                      </Menu.Item>
                    </Menu.Dropdown>
                  </Menu>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>

        {/* Empty State */}
        {data?.items?.length === 0 && !isLoading && (
          <Box p="xl" ta="center">
            <Text c="dimmed" size="lg" mb="xs">
              {filters.searchQuery || (filters.statusFilters && filters.statusFilters.length > 0)
                ? '🔍 No applications match your filters'
                : '📋 No applications yet'
              }
            </Text>
            <Text c="dimmed" size="sm" mb="md">
              {filters.searchQuery || (filters.statusFilters && filters.statusFilters.length > 0)
                ? 'Try adjusting your filters or search terms.'
                : 'Applications will appear here once members submit them.'
              }
            </Text>
            {(filters.searchQuery || (filters.statusFilters && filters.statusFilters.length > 0)) && (
              <Button
                variant="light"
                onClick={handleClearFilters}
                leftSection={<IconX size={16} />}
              >
                Clear Filters
              </Button>
            )}
          </Box>
        )}

        {/* Loading Overlay */}
        {isLoading && data && (
          <Box p="md" ta="center">
            <Text c="dimmed" size="sm">Loading...</Text>
          </Box>
        )}
      </Paper>

      {/* Pagination */}
      {data && data.totalCount > filters.pageSize && (
        <Group justify="space-between" align="center">
          <Text size="sm" c="dimmed">
            Showing {((filters.page - 1) * filters.pageSize) + 1}-
            {Math.min(filters.page * filters.pageSize, data.totalCount)} of {data.totalCount}
          </Text>
          <Pagination
            total={Math.ceil(data.totalCount / filters.pageSize)}
            value={filters.page}
            onChange={handlePageChange}
            size="sm"
            styles={{
              control: {
                '&[data-active]': {
                  backgroundColor: '#880124',
                  borderColor: '#880124'
                }
              }
            }}
          />
        </Group>
      )}
    </Stack>
  );
};
