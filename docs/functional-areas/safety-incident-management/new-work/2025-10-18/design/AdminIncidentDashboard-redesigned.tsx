import React, { useState, useCallback } from 'react';
import {
  Container,
  Stack,
  Title,
  Card,
  Group,
  Text,
  SimpleGrid,
  Badge,
  Box,
  Button,
  Skeleton
} from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { IconFileAlert } from '@tabler/icons-react';
import type { PaginatedResponse } from '../../../lib/api/types/api.types';
import type { IncidentSummaryDto } from '../../../features/safety/types/safety.types';
import { IncidentFilters } from '../../../features/safety/components/IncidentFilters';
import { safetyApi } from '../../../features/safety/api/safetyApi';

interface IncidentFilterRequest {
  searchQuery?: string;
  statusFilters?: string[];
  assignedToFilters?: string[];
  sortBy?: 'UpdatedAt' | 'ReportedAt' | 'Status';
  sortDirection?: 'Asc' | 'Desc';
  page: number;
  pageSize: number;
}

/**
 * Admin Incident Dashboard - Card-Based Grid Pattern
 *
 * REDESIGN (2025-10-18):
 * - Matches vetting applications page pattern EXACTLY
 * - Card-based grid view (NOT table)
 * - Simplified filters (removed date range)
 * - Removed statistics row
 * - Removed recent incidents section
 * - Clean, focused layout
 *
 * Pattern Source: Vetting Applications Page
 * Design System: v7
 * UI Framework: Mantine v7
 */
export const AdminIncidentDashboard: React.FC = () => {
  const navigate = useNavigate();

  // Filter state (simplified - no dateRange)
  const [filters, setFilters] = useState<IncidentFilterRequest>({
    page: 1,
    pageSize: 12, // 3 columns × 4 rows on desktop
    sortBy: 'UpdatedAt',
    sortDirection: 'Desc'
  });

  // Fetch incidents with filters
  const { data: incidentsData, isLoading: isLoadingIncidents } = useQuery<PaginatedResponse<IncidentSummaryDto>>({
    queryKey: ['safety', 'incidents', filters],
    queryFn: async () => {
      const response = await safetyApi.searchIncidents({
        searchText: filters.searchQuery,
        status: filters.statusFilters?.[0] as any,
        page: filters.page,
        pageSize: filters.pageSize,
      });
      return response;
    },
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  // Handlers
  const handleFilterChange = useCallback((newFilters: IncidentFilterRequest) => {
    setFilters(newFilters);
  }, []);

  const handleClearFilters = useCallback(() => {
    setFilters({
      page: 1,
      pageSize: 12,
      sortBy: 'UpdatedAt',
      sortDirection: 'Desc'
    });
  }, []);

  const handleIncidentClick = useCallback((incidentId: string) => {
    // TODO: Navigate to incident detail page when implemented in Phase 2
    console.log('Navigate to incident:', incidentId);
    // navigate(`/admin/safety/incidents/${incidentId}`);
  }, []);

  const handlePageChange = useCallback((newPage: number) => {
    setFilters(prev => ({ ...prev, page: newPage }));
  }, []);

  // Helper functions
  const getStatusColor = (status: string): string => {
    const statusMap: Record<string, string> = {
      'ReportSubmitted': 'blue',
      'InformationGathering': 'yellow',
      'ReviewingFinalReport': 'orange',
      'OnHold': 'gray',
      'Closed': 'green'
    };
    return statusMap[status] || 'gray';
  };

  const formatStatus = (status: string): string => {
    return status.replace(/([A-Z])/g, ' $1').trim();
  };

  const formatDate = (date: Date | string | null | undefined): string => {
    if (!date) return 'N/A';
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  };

  // Data
  const incidents = incidentsData?.items || [];
  const total = incidentsData?.total || 0;
  const totalPages = incidentsData?.totalPages || 1;
  const hasActiveFilters = !!(filters.searchQuery || (filters.statusFilters && filters.statusFilters.length > 0));

  return (
    <Container size="xl" py="xl">
      <Stack gap="lg">
        {/* Header - Simple title only */}
        <Title order={1}>Incident Dashboard</Title>

        {/* Filters - Simplified (no date range) */}
        <IncidentFilters
          filters={filters}
          onFilterChange={handleFilterChange}
          onClearFilters={handleClearFilters}
          isLoading={isLoadingIncidents}
        />

        {/* Incident Card Grid */}
        {isLoadingIncidents ? (
          // Loading State - Skeleton Cards
          <SimpleGrid
            cols={{ base: 1, sm: 2, lg: 3 }}
            spacing="md"
            data-testid="incidents-grid-loading"
          >
            {Array.from({ length: 6 }).map((_, index) => (
              <Card key={index} padding="lg" radius="md" withBorder>
                <Stack gap="sm">
                  <Skeleton height={24} width="60%" />
                  <Skeleton height={16} width="80%" />
                  <Skeleton height={16} width="40%" />
                  <Group justify="space-between" mt="xs">
                    <Skeleton height={14} width="30%" />
                    <Skeleton height={20} width="25%" />
                  </Group>
                </Stack>
              </Card>
            ))}
          </SimpleGrid>
        ) : incidents.length === 0 ? (
          // Empty State
          <Box ta="center" py="xl" data-testid="incidents-empty-state">
            <IconFileAlert size={64} color="#B76D75" style={{ opacity: 0.5 }} />
            <Text size="lg" fw={600} mt="md" c="dimmed">
              No incidents found
            </Text>
            <Text size="sm" c="dimmed" mt="xs">
              {hasActiveFilters
                ? 'Try adjusting your filters or search query.'
                : 'No incidents have been reported yet.'}
            </Text>
            {hasActiveFilters && (
              <Button
                variant="outline"
                color="wcr"
                mt="md"
                onClick={handleClearFilters}
              >
                Clear All Filters
              </Button>
            )}
          </Box>
        ) : (
          // Incident Cards
          <SimpleGrid
            cols={{ base: 1, sm: 2, lg: 3 }}
            spacing="md"
            data-testid="incidents-grid"
          >
            {incidents.map((incident) => (
              <Card
                key={incident.id}
                shadow="sm"
                padding="lg"
                radius="md"
                withBorder
                role="article"
                aria-label={`Incident ${incident.referenceNumber} at ${incident.location}`}
                tabIndex={0}
                style={{
                  cursor: 'pointer',
                  transition: 'all 0.2s ease',
                  background: '#FAF6F2'
                }}
                styles={{
                  root: {
                    '&:hover': {
                      transform: 'translateY(-2px)',
                      boxShadow: '0 8px 16px rgba(0,0,0,0.08)'
                    }
                  }
                }}
                onClick={() => handleIncidentClick(incident.id!)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    handleIncidentClick(incident.id!);
                  }
                }}
              >
                <Stack gap="sm">
                  {/* Reference Number & Status */}
                  <Group justify="space-between" align="flex-start">
                    <Text
                      size="lg"
                      fw={600}
                      c="wcr.7"
                      style={{ fontFamily: 'Source Sans 3, sans-serif' }}
                    >
                      {incident.referenceNumber}
                    </Text>
                    <Badge color={getStatusColor(incident.status!)} size="sm">
                      {formatStatus(incident.status!)}
                    </Badge>
                  </Group>

                  {/* Location */}
                  <Text size="sm" c="dimmed" lineClamp={2}>
                    {incident.location || 'No location specified'}
                  </Text>

                  {/* Metadata Row */}
                  <Group justify="space-between" mt="xs">
                    <Text size="xs" c="dimmed">
                      {formatDate(incident.reportedAt)}
                    </Text>
                    {incident.assignedUserName ? (
                      <Text size="xs" fw={500} c="dark">
                        {incident.assignedUserName}
                      </Text>
                    ) : (
                      <Badge color="gray" size="xs">
                        Unassigned
                      </Badge>
                    )}
                  </Group>
                </Stack>
              </Card>
            ))}
          </SimpleGrid>
        )}

        {/* Pagination */}
        {!isLoadingIncidents && incidents.length > 0 && (
          <Group justify="space-between" mt="lg">
            <Text size="sm" c="dimmed">
              Showing {incidents.length} of {total} incidents
            </Text>
            <Group gap="xs">
              <Button
                variant="subtle"
                size="sm"
                disabled={filters.page === 1}
                onClick={() => handlePageChange(filters.page - 1)}
              >
                Previous
              </Button>
              <Text size="sm" c="dimmed">
                Page {filters.page} of {totalPages}
              </Text>
              <Button
                variant="subtle"
                size="sm"
                disabled={filters.page >= totalPages}
                onClick={() => handlePageChange(filters.page + 1)}
              >
                Next
              </Button>
            </Group>
          </Group>
        )}
      </Stack>
    </Container>
  );
};
