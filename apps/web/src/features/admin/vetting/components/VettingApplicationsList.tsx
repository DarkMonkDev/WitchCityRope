import React, { useState } from 'react';
import {
  Table,
  Text,
  Badge,
  ActionIcon,
  Group,
  TextInput,
  Select,
  Button,
  Paper,
  Stack,
  Pagination,
  Box,
  Title,
  Tooltip,
  rem
} from '@mantine/core';
import {
  IconSearch,
  IconEye,
  IconFilter,
  IconRefresh,
  IconSortAscending,
  IconSortDescending
} from '@tabler/icons-react';
import { useVettingApplications } from '../hooks/useVettingApplications';
import { VettingStatusBadge } from './VettingStatusBadge';
import type { ApplicationSummaryDto, ApplicationFilterRequest } from '../types/vetting.types';

interface VettingApplicationsListProps {
  onViewApplication: (applicationId: string) => void;
}

export const VettingApplicationsList: React.FC<VettingApplicationsListProps> = ({
  onViewApplication
}) => {
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

  const { data, isLoading, error, refetch } = useVettingApplications(filters);

  const handleFilterChange = (field: keyof ApplicationFilterRequest, value: any) => {
    setFilters(prev => ({
      ...prev,
      [field]: value,
      page: field !== 'page' ? 1 : value // Reset to page 1 when filters change
    }));
  };

  const handleSort = (field: string) => {
    setFilters(prev => ({
      ...prev,
      sortBy: field,
      sortDirection: prev.sortBy === field && prev.sortDirection === 'Asc' ? 'Desc' : 'Asc'
    }));
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  const getDaysInStatus = (application: ApplicationSummaryDto) => {
    const days = application.daysInCurrentStatus;
    if (days === 0) return 'Today';
    if (days === 1) return '1 day';
    return `${days} days`;
  };

  const getSortIcon = (field: string) => {
    if (filters.sortBy !== field) return null;
    return filters.sortDirection === 'Asc' ?
      <IconSortAscending size={16} /> :
      <IconSortDescending size={16} />;
  };

  const statusOptions = [
    { value: '', label: 'All Statuses' },
    { value: 'New', label: 'New Applications' },
    { value: 'InReview', label: 'In Review' },
    { value: 'PendingReferences', label: 'Pending References' },
    { value: 'InterviewScheduled', label: 'Interview Scheduled' },
    { value: 'Approved', label: 'Approved' },
    { value: 'Rejected', label: 'Rejected' }
  ];

  if (error) {
    return (
      <Paper p="xl" radius="md">
        <Text c="red" ta="center">
          Error loading applications: {error.message}
        </Text>
        <Group justify="center" mt="md">
          <Button onClick={() => refetch()} leftSection={<IconRefresh size={16} />}>
            Try Again
          </Button>
        </Group>
      </Paper>
    );
  }

  return (
    <Stack gap="md">
      {/* Header */}
      <Group justify="space-between" align="center">
        <Title order={2} style={{ color: '#880124' }}>
          Vetting Applications
        </Title>
        <Button
          leftSection={<IconRefresh size={16} />}
          variant="light"
          onClick={() => refetch()}
          loading={isLoading}
        >
          Refresh
        </Button>
      </Group>

      {/* Filters */}
      <Paper p="md" radius="md" style={{ background: '#FFF8F0' }}>
        <Group gap="md" wrap="wrap">
          <TextInput
            placeholder="Search by scene name or application #"
            leftSection={<IconSearch size={16} />}
            value={filters.searchQuery || ''}
            onChange={(e) => handleFilterChange('searchQuery', e.currentTarget.value)}
            style={{ minWidth: rem(250) }}
          />

          <Select
            placeholder="Filter by status"
            data={statusOptions}
            value={filters.statusFilters[0] || ''}
            onChange={(value) => handleFilterChange('statusFilters', value ? [value] : [])}
            leftSection={<IconFilter size={16} />}
            clearable
            style={{ minWidth: rem(200) }}
          />

          <Select
            placeholder="Page size"
            data={[
              { value: '10', label: '10 per page' },
              { value: '25', label: '25 per page' },
              { value: '50', label: '50 per page' }
            ]}
            value={filters.pageSize.toString()}
            onChange={(value) => handleFilterChange('pageSize', parseInt(value || '25'))}
            style={{ minWidth: rem(120) }}
          />
        </Group>
      </Paper>

      {/* Results Summary */}
      {data && (
        <Text size="sm" c="dimmed">
          Showing {data.items.length} of {data.totalCount} applications
          {filters.searchQuery && ` matching "${filters.searchQuery}"`}
        </Text>
      )}

      {/* Applications Table */}
      <Paper shadow="sm" radius="md">
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>
                <Button
                  variant="subtle"
                  size="compact-sm"
                  onClick={() => handleSort('ApplicationNumber')}
                  rightSection={getSortIcon('ApplicationNumber')}
                >
                  Application #
                </Button>
              </Table.Th>
              <Table.Th>
                <Button
                  variant="subtle"
                  size="compact-sm"
                  onClick={() => handleSort('SceneName')}
                  rightSection={getSortIcon('SceneName')}
                >
                  Scene Name
                </Button>
              </Table.Th>
              <Table.Th>Status</Table.Th>
              <Table.Th>
                <Button
                  variant="subtle"
                  size="compact-sm"
                  onClick={() => handleSort('SubmittedAt')}
                  rightSection={getSortIcon('SubmittedAt')}
                >
                  Submitted
                </Button>
              </Table.Th>
              <Table.Th>Time in Status</Table.Th>
              <Table.Th>Experience</Table.Th>
              <Table.Th>Reviewer</Table.Th>
              <Table.Th>Actions</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {data?.items.map((application) => (
              <Table.Tr key={application.id}>
                <Table.Td>
                  <Text size="sm" fw={600} style={{ color: '#880124' }}>
                    {application.applicationNumber}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">
                    {application.sceneName}
                  </Text>
                  {application.isAnonymous && (
                    <Badge size="xs" color="gray" variant="dot">
                      Anonymous
                    </Badge>
                  )}
                </Table.Td>
                <Table.Td>
                  <VettingStatusBadge status={application.status} />
                </Table.Td>
                <Table.Td>
                  <Text size="sm">
                    {formatDate(application.submittedAt)}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm" c={application.daysInCurrentStatus > 7 ? 'red' : 'dimmed'}>
                    {getDaysInStatus(application)}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Stack gap={4}>
                    <Text size="sm">{application.experienceLevel}</Text>
                    <Text size="xs" c="dimmed">
                      {application.yearsExperience} years
                    </Text>
                  </Stack>
                </Table.Td>
                <Table.Td>
                  <Text size="sm" c={application.assignedReviewerName ? undefined : 'dimmed'}>
                    {application.assignedReviewerName || 'Unassigned'}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Group gap="xs">
                    <Tooltip label="View Application Details">
                      <ActionIcon
                        variant="light"
                        color="blue"
                        onClick={() => onViewApplication(application.id)}
                      >
                        <IconEye size={16} />
                      </ActionIcon>
                    </Tooltip>
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>

        {/* Empty State */}
        {data?.items.length === 0 && !isLoading && (
          <Box p="xl" ta="center">
            <Text c="dimmed" size="lg">
              {filters.searchQuery || filters.statusFilters.length > 0
                ? 'No applications match your filters'
                : 'No vetting applications found'
              }
            </Text>
            {(filters.searchQuery || filters.statusFilters.length > 0) && (
              <Button
                variant="light"
                mt="md"
                onClick={() => setFilters({
                  page: 1,
                  pageSize: 25,
                  statusFilters: [],
                  priorityFilters: [],
                  experienceLevelFilters: [],
                  skillsFilters: [],
                  searchQuery: '',
                  sortBy: 'SubmittedAt',
                  sortDirection: 'Desc'
                })}
              >
                Clear Filters
              </Button>
            )}
          </Box>
        )}

        {/* Loading State */}
        {isLoading && (
          <Box p="xl" ta="center">
            <Text c="dimmed">Loading applications...</Text>
          </Box>
        )}
      </Paper>

      {/* Pagination */}
      {data && data.totalCount > filters.pageSize && (
        <Group justify="center">
          <Pagination
            total={Math.ceil(data.totalCount / filters.pageSize)}
            value={filters.page}
            onChange={(page) => handleFilterChange('page', page)}
            size="sm"
          />
        </Group>
      )}
    </Stack>
  );
};