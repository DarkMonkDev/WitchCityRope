import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Table,
  Text,
  Badge,
  Group,
  TextInput,
  Select,
  MultiSelect,
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
  onSelectionChange?: (selectedIds: Set<string>, applicationsData: any[]) => void;
}

export const VettingApplicationsList: React.FC<VettingApplicationsListProps> = ({ onSelectionChange }) => {
  const navigate = useNavigate();
  const [filters, setFilters] = useState<ApplicationFilterRequest>({
    page: 1,
    pageSize: 25,
    statusFilters: ['UnderReview', 'InterviewApproved', 'PendingInterview'], // Default checked statuses
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

  // Notify parent component when selection changes
  React.useEffect(() => {
    if (onSelectionChange && data?.items) {
      const selectedData = data.items.filter(app => selectedApplications.has(app.id));
      onSelectionChange(selectedApplications, selectedData);
    }
  }, [selectedApplications, data?.items, onSelectionChange]);

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
    { value: 'UnderReview', label: 'Under Review' },
    { value: 'InterviewApproved', label: 'Approved for Interview' },
    { value: 'PendingInterview', label: 'Pending Interview' },
    { value: 'Approved', label: 'Approved' },
    { value: 'OnHold', label: 'On Hold' },
    { value: 'Denied', label: 'Denied' }
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

      {/* Filters - Matching wireframe exactly */}
      <Paper p="md" radius="md" style={{ background: '#FFF8F0' }}>
        <Group gap="md" wrap="wrap" justify="space-between">
          <TextInput
            data-testid="search-input"
            placeholder="Search by name, email, or scene name..."
            leftSection={<IconSearch size={16} />}
            value={filters.searchQuery || ''}
            onChange={(e) => handleFilterChange('searchQuery', e.currentTarget.value)}
            style={{
              minWidth: rem(400),
              flex: 1
            }}
            styles={{
              input: {
                fontSize: '16px',
                height: '42px'
              }
            }}
          />

          <Group gap="md">
            <MultiSelect
              data-testid="status-filter"
              placeholder="Select status filters"
              data={statusOptions}
              value={filters.statusFilters}
              onChange={(values) => handleFilterChange('statusFilters', values)}
              leftSection={<IconFilter size={16} />}
              clearable
              style={{ minWidth: rem(220) }}
              styles={{
                input: {
                  fontSize: '16px',
                  height: '42px'
                }
              }}
            />

          </Group>
        </Group>
      </Paper>

      {/* Results Summary - Removed as requested */}

      {/* Applications Table - Exactly matching wireframe */}
      <Paper shadow="sm" radius="md" data-testid="vetting-applications-table">
        <Table striped highlightOnHover data-testid="applications-table">
          <Table.Thead
            style={{
              backgroundColor: '#880124',
              color: 'white'
            }}
          >
            <Table.Tr>
              {/* Checkbox column */}
              <Table.Th w={50} style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Checkbox
                  checked={selectAll}
                  onChange={(event) => handleSelectAll(event.currentTarget.checked)}
                  aria-label="Select all applications"
                  styles={{
                    input: {
                      backgroundColor: 'white',
                      borderColor: 'white'
                    }
                  }}
                />
              </Table.Th>

              {/* Name column - sortable */}
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none', cursor: 'pointer' }} onClick={() => handleSort('RealName')}>
                <Group gap={4} justify="flex-start">
                  <Text
                    fw={600}
                    size="sm"
                    style={{
                      color: 'white',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px'
                    }}
                  >
                    NAME
                  </Text>
                  {getSortIcon('RealName')}
                </Group>
              </Table.Th>

              {/* FetLife Name column - sortable */}
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none', cursor: 'pointer' }} onClick={() => handleSort('FetLifeHandle')}>
                <Group gap={4} justify="flex-start">
                  <Text
                    fw={600}
                    size="sm"
                    style={{
                      color: 'white',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px'
                    }}
                  >
                    FETLIFE NAME
                  </Text>
                  {getSortIcon('FetLifeHandle')}
                </Group>
              </Table.Th>

              {/* Email column */}
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text
                  fw={600}
                  size="sm"
                  style={{
                    color: 'white',
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px'
                  }}
                >
                  EMAIL
                </Text>
              </Table.Th>

              {/* Application Date column - sortable */}
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none', cursor: 'pointer' }} onClick={() => handleSort('SubmittedAt')}>
                <Group gap={4} justify="center">
                  <Text
                    fw={600}
                    size="sm"
                    style={{
                      color: 'white',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px'
                    }}
                  >
                    APPLICATION DATE
                  </Text>
                  {getSortIcon('SubmittedAt')}
                </Group>
              </Table.Th>

              {/* Current Status column */}
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text
                  fw={600}
                  size="sm"
                  style={{
                    color: 'white',
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px'
                  }}
                >
                  CURRENT STATUS
                </Text>
              </Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {data?.items.map((application) => (
              <Table.Tr
                key={application.id}
                onClick={(event) => {
                  // Only navigate if clicking on the row itself, not the checkbox
                  if (!(event.target as HTMLElement).closest('input[type="checkbox"]')) {
                    handleRowClick(application.id);
                  }
                }}
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
                  <Text size="sm" fw={600} style={{ color: '#2B2B2B' }}>
                    {(application as any).realName || (application as any).fullName || application.sceneName || 'Name not provided'}
                  </Text>
                </Table.Td>

                {/* FetLife Name - Handle in bold */}
                <Table.Td>
                  <Text size="sm" fw={600} style={{ color: '#2B2B2B' }}>
                    {(application as any).fetLifeHandle || (application as any).fetLifeName || 'Not provided'}
                  </Text>
                </Table.Td>

                {/* Email */}
                <Table.Td>
                  <Text size="sm" style={{ color: '#2B2B2B' }}>
                    {(application as any).email || 'Not provided'}
                  </Text>
                </Table.Td>

                {/* Application Date - Center aligned */}
                <Table.Td ta="center">
                  <Text size="sm" style={{ color: '#2B2B2B' }}>
                    {formatDate(application.submittedAt)}
                  </Text>
                </Table.Td>

                {/* Current Status - Colored pill badges matching wireframe */}
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

      {/* Pagination - Matching wireframe style */}
      {data && data.totalCount > filters.pageSize && (
        <Group justify="space-between" align="center">
          <Text size="sm" c="dimmed">
            Showing {((filters.page - 1) * filters.pageSize) + 1}-{Math.min(filters.page * filters.pageSize, data.totalCount)} of {data.totalCount} applications
          </Text>
          <Pagination
            total={Math.ceil(data.totalCount / filters.pageSize)}
            value={filters.page}
            onChange={(page) => handleFilterChange('page', page)}
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