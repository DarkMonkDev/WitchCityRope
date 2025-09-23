import React, { useState } from 'react';
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
  Title,
  rem,
  Checkbox
} from '@mantine/core';
import {
  IconSearch,
  IconFilter,
  IconRefresh,
  IconSortAscending,
  IconSortDescending
} from '@tabler/icons-react';
import { useVettingApplications } from '../hooks/useVettingApplications';
import { VettingStatusBadge } from './VettingStatusBadge';
import type { ApplicationSummaryDto, ApplicationFilterRequest } from '../types/vetting.types';

interface VettingApplicationsListProps {
  // No props needed - component handles navigation internally
}

export const VettingApplicationsList: React.FC<VettingApplicationsListProps> = () => {
  const navigate = useNavigate();
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

  // Bulk selection state
  const [selectedApplications, setSelectedApplications] = useState<Set<string>>(new Set());
  const [selectAll, setSelectAll] = useState(false);

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

  // Bulk selection handlers
  const handleSelectAll = (checked: boolean) => {
    setSelectAll(checked);
    if (checked) {
      const allIds = new Set(data?.items.map(app => app.id) || []);
      setSelectedApplications(allIds);
    } else {
      setSelectedApplications(new Set());
    }
  };

  const handleSelectApplication = (applicationId: string, checked: boolean) => {
    const newSelected = new Set(selectedApplications);
    if (checked) {
      newSelected.add(applicationId);
    } else {
      newSelected.delete(applicationId);
      setSelectAll(false); // Uncheck "select all" if individual item is unchecked
    }
    setSelectedApplications(newSelected);
  };

  const handleRowClick = (applicationId: string) => {
    // Navigate to detail page instead of calling onViewItem callback
    navigate(`/admin/vetting/applications/${applicationId}`);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
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

      {/* Applications Table - Following EXACT wireframe structure */}
      <Paper shadow="sm" radius="md">
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              {/* Checkbox column - 40px width */}
              <Table.Th w={40}>
                <Checkbox
                  checked={selectAll}
                  onChange={(event) => handleSelectAll(event.currentTarget.checked)}
                  aria-label="Select all applications"
                />
              </Table.Th>

              {/* Name column - sortable */}
              <Table.Th>
                <Button
                  variant="subtle"
                  size="compact-sm"
                  onClick={() => handleSort('RealName')}
                  rightSection={getSortIcon('RealName')}
                  styles={{
                    root: {
                      fontWeight: 600,
                      fontSize: 14
                    }
                  }}
                >
                  Name
                </Button>
              </Table.Th>

              {/* FetLife Name column - sortable */}
              <Table.Th>
                <Button
                  variant="subtle"
                  size="compact-sm"
                  onClick={() => handleSort('FetLifeHandle')}
                  rightSection={getSortIcon('FetLifeHandle')}
                  styles={{
                    root: {
                      fontWeight: 600,
                      fontSize: 14
                    }
                  }}
                >
                  FetLife Name
                </Button>
              </Table.Th>

              {/* Email column */}
              <Table.Th>
                <Text fw={600} size="sm">Email</Text>
              </Table.Th>

              {/* Application Date column - sortable, default desc */}
              <Table.Th>
                <Button
                  variant="subtle"
                  size="compact-sm"
                  onClick={() => handleSort('SubmittedAt')}
                  rightSection={getSortIcon('SubmittedAt')}
                  styles={{
                    root: {
                      fontWeight: 600,
                      fontSize: 14
                    }
                  }}
                >
                  Application Date {filters.sortBy === 'SubmittedAt' && filters.sortDirection === 'Desc' ? '↓' : ''}
                </Button>
              </Table.Th>

              {/* Current Status column */}
              <Table.Th>
                <Text fw={600} size="sm">Current Status</Text>
              </Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {data?.items.map((application) => (
              <Table.Tr
                key={application.id}
                onClick={() => handleRowClick(application.id)}
                style={{
                  cursor: 'pointer',
                  backgroundColor: selectedApplications.has(application.id) ? '#f0f8ff' : undefined
                }}
              >
                {/* Checkbox - stopPropagation to prevent row click */}
                <Table.Td>
                  <Checkbox
                    checked={selectedApplications.has(application.id)}
                    onChange={(event) => {
                      event.stopPropagation(); // Prevent row click
                      handleSelectApplication(application.id, event.currentTarget.checked);
                    }}
                    aria-label={`Select application for ${(application as any).realName || (application as any).fullName || application.sceneName}`}
                  />
                </Table.Td>

                {/* Name - Real name in bold */}
                <Table.Td>
                  <Text size="sm" fw={700}>
                    {(application as any).realName || (application as any).fullName || 'Name not provided'}
                  </Text>
                </Table.Td>

                {/* FetLife Name - Handle in bold */}
                <Table.Td>
                  <Text size="sm" fw={700}>
                    {(application as any).fetLifeHandle || (application as any).fetLifeName || 'Not provided'}
                  </Text>
                </Table.Td>

                {/* Email */}
                <Table.Td>
                  <Text size="sm">
                    {(application as any).email || 'Not provided'}
                  </Text>
                </Table.Td>

                {/* Application Date */}
                <Table.Td>
                  <Text size="sm">
                    {formatDate(application.submittedAt)}
                  </Text>
                </Table.Td>

                {/* Current Status */}
                <Table.Td>
                  <VettingStatusBadge status={application.status} />
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