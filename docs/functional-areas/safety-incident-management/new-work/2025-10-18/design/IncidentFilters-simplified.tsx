import React, { useMemo, useCallback } from 'react';
import {
  Paper,
  Stack,
  Group,
  TextInput,
  Select,
  Button,
  Badge,
  rem
} from '@mantine/core';
import {
  IconSearch,
  IconFilter,
  IconX
} from '@tabler/icons-react';

interface IncidentFilterRequest {
  searchQuery?: string;
  statusFilters?: string[];
  assignedToFilters?: string[];
  sortBy?: 'UpdatedAt' | 'ReportedAt' | 'Status';
  sortDirection?: 'Asc' | 'Desc';
  page: number;
  pageSize: number;
}

interface IncidentFiltersProps {
  filters: IncidentFilterRequest;
  onFilterChange: (filters: IncidentFilterRequest) => void;
  onClearFilters: () => void;
  isLoading?: boolean;
}

/**
 * Incident Filters Component - Simplified Pattern
 *
 * REDESIGN (2025-10-18):
 * - Matches vetting applications filter pattern EXACTLY
 * - REMOVED: Date range filter (not in vetting page)
 * - KEPT: Search, Status, Clear Filters
 * - Horizontal layout with active filter badges
 *
 * Pattern Source: Vetting Applications Page
 * Design System: v7
 */
export const IncidentFilters: React.FC<IncidentFiltersProps> = ({
  filters,
  onFilterChange,
  onClearFilters,
  isLoading = false
}) => {
  // Status options (5-stage enum)
  const statusOptions = useMemo(() => [
    { value: '', label: 'All Statuses' },
    { value: 'ReportSubmitted', label: 'Report Submitted' },
    { value: 'InformationGathering', label: 'Information Gathering' },
    { value: 'ReviewingFinalReport', label: 'Reviewing Final Report' },
    { value: 'OnHold', label: 'On Hold' },
    { value: 'Closed', label: 'Closed' }
  ], []);

  // Calculate active filter count
  const activeFilterCount = useMemo(() => {
    let count = 0;
    if (filters.searchQuery) count++;
    if (filters.statusFilters && filters.statusFilters.length > 0) count++;
    return count;
  }, [filters]);

  // Search handler
  const handleSearchChange = useCallback((value: string) => {
    onFilterChange({
      ...filters,
      searchQuery: value,
      page: 1 // Reset to first page on search
    });
  }, [filters, onFilterChange]);

  // Status filter handler
  const handleStatusChange = useCallback((value: string | null) => {
    onFilterChange({
      ...filters,
      statusFilters: value && value !== '' ? [value] : [],
      page: 1 // Reset to first page on filter
    });
  }, [filters, onFilterChange]);

  return (
    <Paper p="md" radius="md" style={{ background: '#FAF6F2' }}>
      <Stack gap="md">
        {/* Primary Filters Row */}
        <Group gap="md" align="flex-end" wrap="wrap">
          {/* Search */}
          <TextInput
            placeholder="Search by reference number, location, or coordinator..."
            leftSection={<IconSearch size={16} />}
            value={filters.searchQuery || ''}
            onChange={(e) => handleSearchChange(e.currentTarget.value)}
            disabled={isLoading}
            style={{ flex: 1, minWidth: rem(300) }}
            styles={{
              input: {
                fontSize: '16px',
                height: '42px',
                borderRadius: '8px'
              }
            }}
          />

          {/* Status Filter */}
          <Select
            placeholder="Filter by status"
            data={statusOptions}
            value={filters.statusFilters?.[0] || ''}
            onChange={handleStatusChange}
            leftSection={<IconFilter size={16} />}
            disabled={isLoading}
            clearable
            style={{ minWidth: rem(200) }}
            styles={{
              input: {
                fontSize: '16px',
                height: '42px',
                borderRadius: '8px'
              }
            }}
          />

          {/* Clear Filters */}
          {activeFilterCount > 0 && (
            <Button
              variant="subtle"
              color="gray"
              onClick={onClearFilters}
              leftSection={<IconX size={16} />}
              disabled={isLoading}
            >
              Clear Filters ({activeFilterCount})
            </Button>
          )}
        </Group>

        {/* Active Filter Badges */}
        {activeFilterCount > 0 && (
          <Group gap="xs">
            {filters.searchQuery && (
              <Badge
                color="burgundy"
                rightSection={
                  <IconX
                    size={12}
                    style={{ cursor: 'pointer' }}
                    onClick={() => handleSearchChange('')}
                  />
                }
              >
                Search: {filters.searchQuery}
              </Badge>
            )}
            {filters.statusFilters && filters.statusFilters.length > 0 && (
              <Badge
                color="burgundy"
                rightSection={
                  <IconX
                    size={12}
                    style={{ cursor: 'pointer' }}
                    onClick={() => handleStatusChange(null)}
                  />
                }
              >
                Status: {filters.statusFilters[0]}
              </Badge>
            )}
          </Group>
        )}
      </Stack>
    </Paper>
  );
};
