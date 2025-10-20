import React, { useState, useCallback, useMemo } from 'react';
import {
  Container,
  Stack,
  Title,
  Group,
  Text,
  Paper,
  Box,
  Button,
  TextInput,
  Select,
  MultiSelect,
  Table,
  Badge,
  Skeleton,
  Alert,
  Pagination,
  rem
} from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { IconSearch, IconFilter, IconX, IconRefresh, IconSortAscending, IconSortDescending } from '@tabler/icons-react';
import type { PaginatedResponse } from '../../../lib/api/types/api.types';
import type { IncidentSummaryDto } from '../../../features/safety/types/safety.types';
import { safetyApi } from '../../../features/safety/api/safetyApi';

interface IncidentFilterRequest {
  searchQuery?: string;
  statusFilters: string[]; // MultiSelect: array of selected statuses
  page: number;
  pageSize: number;
  sortBy: string;
  sortDirection: 'asc' | 'desc';
}

/**
 * Admin Incident Dashboard
 *
 * Matches VettingApplicationsList pattern exactly:
 * - Search by reference number or location
 * - MultiSelect filter by status (allows multiple selections)
 * - Clear filters button
 * - Incident table with pagination
 *
 * Uses generated types from @witchcityrope/shared-types (DTO Alignment Strategy)
 * Follows Mantine v7 patterns with proper spacing and typography
 */
export const AdminIncidentDashboard: React.FC = () => {
  const navigate = useNavigate();

  // Filter state - defaults to active statuses like vetting page
  const [filters, setFilters] = useState<IncidentFilterRequest>({
    page: 1,
    pageSize: 25,
    searchQuery: '',
    statusFilters: ['ReportSubmitted', 'InformationGathering', 'ReviewingFinalReport'],
    sortBy: 'reportedAt',
    sortDirection: 'desc'
  });

  // Fetch incidents with filters
  const { data, isLoading, error, refetch } = useQuery<PaginatedResponse<IncidentSummaryDto>>({
    queryKey: ['safety', 'incidents', filters],
    queryFn: async () => {
      const response = await safetyApi.searchIncidents({
        searchText: filters.searchQuery,
        // Backend expects comma-separated statuses in single string
        status: filters.statusFilters.length > 0 ? filters.statusFilters.join(',') : undefined,
        page: filters.page,
        pageSize: filters.pageSize,
        sortBy: filters.sortBy,
        sortDirection: filters.sortDirection,
      });
      return response;
    }
  });

  // Status filter options (no "All Statuses" option for MultiSelect)
  const statusOptions = useMemo(() => [
    { value: 'ReportSubmitted', label: 'Report Submitted' },
    { value: 'InformationGathering', label: 'Information Gathering' },
    { value: 'ReviewingFinalReport', label: 'Reviewing Final Report' },
    { value: 'OnHold', label: 'On Hold' },
    { value: 'Closed', label: 'Closed' }
  ], []);

  // Filter handlers
  const handleSearchChange = useCallback((value: string) => {
    setFilters(prev => ({
      ...prev,
      searchQuery: value,
      page: 1 // Reset to first page on search
    }));
  }, []);

  const handleStatusFilterChange = useCallback((values: string[]) => {
    setFilters(prev => ({
      ...prev,
      statusFilters: values,
      page: 1 // Reset to first page on filter
    }));
  }, []);

  const handleClearFilters = useCallback(() => {
    setFilters({
      page: 1,
      pageSize: 25,
      searchQuery: '',
      statusFilters: [],
      sortBy: 'reportedAt',
      sortDirection: 'desc'
    });
  }, []);

  const handlePageChange = useCallback((page: number) => {
    setFilters(prev => ({ ...prev, page }));
  }, []);

  const handleSort = useCallback((field: string) => {
    setFilters(prev => ({
      ...prev,
      sortBy: field,
      sortDirection: prev.sortBy === field && prev.sortDirection === 'asc' ? 'desc' : 'asc'
    }));
  }, []);

  // Row click handler
  const handleRowClick = useCallback((incidentId: string) => {
    navigate(`/admin/safety/incidents/${incidentId}`);
  }, [navigate]);

  // Date formatter
  const formatDate = useCallback((dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }, []);

  const getSortIcon = useCallback((field: string) => {
    if (filters.sortBy !== field) return null;
    return filters.sortDirection === 'asc' ?
      <IconSortAscending size={16} color="white" /> :
      <IconSortDescending size={16} color="white" />;
  }, [filters.sortBy, filters.sortDirection]);

  // Get status badge color
  const getStatusColor = (status: string): string => {
    switch (status) {
      case 'ReportSubmitted':
        return 'blue';
      case 'InformationGathering':
        return 'yellow';
      case 'ReviewingFinalReport':
        return 'orange';
      case 'OnHold':
        return 'gray';
      case 'Closed':
        return 'green';
      default:
        return 'gray';
    }
  };

  // Format status label
  const formatStatus = (status: string): string => {
    return status.replace(/([A-Z])/g, ' $1').trim();
  };

  // Loading state
  if (isLoading && !data) {
    return (
      <Container size="xl" py="xl">
        <Stack gap="md">
          <Title order={1}>Incident Dashboard</Title>
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
      </Container>
    );
  }

  // Error state
  if (error) {
    return (
      <Container size="xl" py="xl">
        <Stack gap="md">
          <Title order={1}>Incident Dashboard</Title>
          <Alert
            color="red"
            title="Error Loading Incidents"
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
        </Stack>
      </Container>
    );
  }

  return (
    <Container size="xl" py="xl">
      <Stack gap="md">
        {/* Header */}
        <Title order={1}>Incident Dashboard</Title>

        {/* Filters Section */}
        <Paper p="md" radius="md" style={{ background: '#FFF8F0' }}>
          <Group gap="md" align="flex-end" wrap="wrap">
            <TextInput
              placeholder="Search by title, reference number, or location..."
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

            <MultiSelect
              placeholder="Select status filters"
              data={statusOptions}
              value={filters.statusFilters}
              onChange={handleStatusFilterChange}
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
                {/* Title column - sortable */}
                <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none', cursor: 'pointer' }} onClick={() => handleSort('title')}>
                  <Group gap={4} justify="flex-start">
                    <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                      Incident
                    </Text>
                    {getSortIcon('title')}
                  </Group>
                </Table.Th>

                {/* Status column - sortable */}
                <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none', cursor: 'pointer' }} onClick={() => handleSort('status')}>
                  <Group gap={4} justify="flex-start">
                    <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                      Status
                    </Text>
                    {getSortIcon('status')}
                  </Group>
                </Table.Th>

                {/* Coordinator column - sortable */}
                <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none', cursor: 'pointer' }} onClick={() => handleSort('coordinatorName')}>
                  <Group gap={4} justify="flex-start">
                    <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                      Coordinator
                    </Text>
                    {getSortIcon('coordinatorName')}
                  </Group>
                </Table.Th>

                {/* Last Updated column - sortable */}
                <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none', cursor: 'pointer' }} onClick={() => handleSort('reportedAt')}>
                  <Group gap={4} justify="flex-start">
                    <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                      Last Updated
                    </Text>
                    {getSortIcon('reportedAt')}
                  </Group>
                </Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {data?.items?.map((incident) => (
                <Table.Tr
                  key={incident.id}
                  onClick={() => handleRowClick(incident.id!)}
                  style={{
                    cursor: 'pointer'
                  }}
                >
                  <Table.Td>
                    <Box>
                      <Text size="sm" fw={600} style={{ color: '#2B2B2B' }}>
                        {incident.title || 'Untitled Incident'}
                      </Text>
                      <Text size="xs" c="dimmed">
                        {incident.referenceNumber}
                      </Text>
                    </Box>
                  </Table.Td>
                  <Table.Td>
                    <Badge color={getStatusColor(incident.status || '')}>
                      {formatStatus(incident.status || 'Unknown')}
                    </Badge>
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm" style={{ color: '#2B2B2B' }}>
                      {incident.coordinatorName || 'Unassigned'}
                    </Text>
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm" style={{ color: '#2B2B2B' }}>
                      {formatDate(incident.reportedAt!)}
                    </Text>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>

          {/* Empty State */}
          {data?.items?.length === 0 && !isLoading && (
            <Box p="xl" ta="center">
              <Text c="dimmed" size="lg" mb="xs">
                {filters.searchQuery || filters.statusFilters.length > 0
                  ? '🔍 No incidents match your filters'
                  : '📋 No incidents yet'
                }
              </Text>
              <Text c="dimmed" size="sm" mb="md">
                {filters.searchQuery || filters.statusFilters.length > 0
                  ? 'Try adjusting your filters or search terms.'
                  : 'Incidents will appear here once they are reported.'
                }
              </Text>
              {(filters.searchQuery || filters.statusFilters.length > 0) && (
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
        {data && data.total > filters.pageSize && (
          <Group justify="space-between" align="center">
            <Text size="sm" c="dimmed">
              Showing {((filters.page - 1) * filters.pageSize) + 1}-
              {Math.min(filters.page * filters.pageSize, data.total)} of {data.total}
            </Text>
            <Pagination
              total={Math.ceil(data.total / filters.pageSize)}
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
    </Container>
  );
};
