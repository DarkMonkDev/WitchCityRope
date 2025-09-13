// Safety Dashboard Component
// Admin dashboard for managing safety incidents

import React, { useState } from 'react';
import {
  Box,
  Paper,
  Title,
  Text,
  Group,
  Stack,
  Grid,
  Card,
  Badge,
  Button,
  TextInput,
  Select,
  Alert,
  Loader,
  ActionIcon,
  Tooltip,
  Modal
} from '@mantine/core';
import { 
  IconSearch, 
  IconAlertTriangle, 
  IconShieldCheck, 
  IconClock,
  IconUsers,
  IconEye,
  IconFilter,
  IconRefresh
} from '@tabler/icons-react';
import { useDisclosure } from '@mantine/hooks';
import { useSafetyAdminData, useSearchIncidents } from '../hooks/useSafetyIncidents';
import { IncidentList } from './IncidentList';
import { IncidentDetails } from './IncidentDetails';
import { 
  IncidentSeverity, 
  IncidentStatus, 
  SEVERITY_CONFIGS, 
  STATUS_CONFIGS,
  SearchIncidentsRequest 
} from '../types/safety.types';

export function SafetyDashboard() {
  const [selectedIncidentId, setSelectedIncidentId] = useState<string | null>(null);
  const [detailsOpened, { open: openDetails, close: closeDetails }] = useDisclosure(false);
  
  // Search and filter state
  const [searchFilters, setSearchFilters] = useState<SearchIncidentsRequest>({
    page: 1,
    pageSize: 25
  });
  const [searchText, setSearchText] = useState('');
  
  // Fetch dashboard data
  const { 
    dashboard, 
    recentIncidents, 
    totalIncidents, 
    isLoading, 
    error, 
    refetch 
  } = useSafetyAdminData();
  
  // Search results (separate from dashboard recent incidents)
  const searchQuery = useSearchIncidents(searchFilters, true);
  
  // Handle incident selection
  const handleIncidentSelect = (incidentId: string) => {
    setSelectedIncidentId(incidentId);
    openDetails();
  };
  
  // Handle search
  const handleSearch = () => {
    setSearchFilters(prev => ({
      ...prev,
      searchText: searchText.trim() || undefined,
      page: 1 // Reset to first page on new search
    }));
  };
  
  // Handle filter changes
  const handleFilterChange = (field: keyof SearchIncidentsRequest, value: any) => {
    setSearchFilters(prev => ({
      ...prev,
      [field]: value,
      page: 1 // Reset to first page on filter change
    }));
  };
  
  // Clear filters
  const clearFilters = () => {
    setSearchFilters({ page: 1, pageSize: 25 });
    setSearchText('');
  };
  
  if (error) {
    return (
      <Alert variant="light" color="red" icon={<IconAlertTriangle size={16} />}>
        <Text size="sm">
          {error instanceof Error && error.message.includes('403') 
            ? 'Access denied - Safety team role required'
            : 'Failed to load safety dashboard. Please try again.'
          }
        </Text>
      </Alert>
    );
  }
  
  return (
    <Box>
      {/* Header */}
      <Group justify="space-between" align="center" mb="xl">
        <Box>
          <Title order={1} size="h2">Safety Incident Management</Title>
          <Text c="dimmed">Manage and track safety incidents</Text>
        </Box>
        <Group>
          <Tooltip label="Refresh dashboard">
            <ActionIcon variant="light" onClick={() => refetch()} loading={isLoading}>
              <IconRefresh size={16} />
            </ActionIcon>
          </Tooltip>
        </Group>
      </Group>
      
      {/* Statistics Cards */}
      {dashboard && (
        <Grid gutter="md" mb="xl">
          <Grid.Col span={{ base: 12, sm: 6, lg: 3 }}>
            <Card padding="lg" radius="md" withBorder>
              <Group justify="space-between">
                <Box>
                  <Text size="xs" tt="uppercase" c="dimmed" fw={700}>
                    Critical
                  </Text>
                  <Text size="xl" fw={700} c="red">
                    {dashboard.statistics.criticalCount}
                  </Text>
                  <Text size="xs" c="dimmed">
                    Requires Action
                  </Text>
                </Box>
                <IconAlertTriangle size={24} color="#DC143C" />
              </Group>
            </Card>
          </Grid.Col>
          
          <Grid.Col span={{ base: 12, sm: 6, lg: 3 }}>
            <Card padding="lg" radius="md" withBorder>
              <Group justify="space-between">
                <Box>
                  <Text size="xs" tt="uppercase" c="dimmed" fw={700}>
                    High Priority
                  </Text>
                  <Text size="xl" fw={700} c="orange">
                    {dashboard.statistics.highCount}
                  </Text>
                  <Text size="xs" c="dimmed">
                    Needs Review
                  </Text>
                </Box>
                <IconClock size={24} color="#DAA520" />
              </Group>
            </Card>
          </Grid.Col>
          
          <Grid.Col span={{ base: 12, sm: 6, lg: 3 }}>
            <Card padding="lg" radius="md" withBorder>
              <Group justify="space-between">
                <Box>
                  <Text size="xs" tt="uppercase" c="dimmed" fw={700}>
                    This Month
                  </Text>
                  <Text size="xl" fw={700} c="blue">
                    {dashboard.statistics.thisMonth}
                  </Text>
                  <Text size="xs" c="dimmed">
                    New Reports
                  </Text>
                </Box>
                <IconUsers size={24} color="#0066CC" />
              </Group>
            </Card>
          </Grid.Col>
          
          <Grid.Col span={{ base: 12, sm: 6, lg: 3 }}>
            <Card padding="lg" radius="md" withBorder>
              <Group justify="space-between">
                <Box>
                  <Text size="xs" tt="uppercase" c="dimmed" fw={700}>
                    Total Reports
                  </Text>
                  <Text size="xl" fw={700} c="green">
                    {dashboard.statistics.totalCount}
                  </Text>
                  <Text size="xs" c="dimmed">
                    All Time
                  </Text>
                </Box>
                <IconShieldCheck size={24} color="#22C55E" />
              </Group>
            </Card>
          </Grid.Col>
        </Grid>
      )}
      
      {/* Filters and Search */}
      <Paper shadow="sm" p="md" radius="md" mb="lg">
        <Group mb="md" justify="space-between" align="center">
          <Title order={3} size="h4">
            <IconFilter size={16} style={{ marginRight: '8px' }} />
            Filters & Search
          </Title>
          <Button variant="subtle" size="xs" onClick={clearFilters}>
            Clear All
          </Button>
        </Group>
        
        <Grid gutter="md" align="end">
          {/* Severity Filter */}
          <Grid.Col span={{ base: 12, sm: 6, md: 3 }}>
            <Select
              label="Minimum Severity"
              placeholder="All severities"
              data={Object.values(IncidentSeverity).map(severity => ({
                value: severity,
                label: `${SEVERITY_CONFIGS[severity].icon} ${SEVERITY_CONFIGS[severity].label}`
              }))}
              value={searchFilters.minSeverity || null}
              onChange={(value) => handleFilterChange('minSeverity', value as IncidentSeverity)}
              clearable
            />
          </Grid.Col>
          
          {/* Status Filter */}
          <Grid.Col span={{ base: 12, sm: 6, md: 3 }}>
            <Select
              label="Status"
              placeholder="All statuses"
              data={Object.values(IncidentStatus).map(status => ({
                value: status,
                label: STATUS_CONFIGS[status].label
              }))}
              value={searchFilters.status || null}
              onChange={(value) => handleFilterChange('status', value as IncidentStatus)}
              clearable
            />
          </Grid.Col>
          
          {/* Search Text */}
          <Grid.Col span={{ base: 12, md: 4 }}>
            <TextInput
              label="Search"
              placeholder="Reference number, location, keywords..."
              value={searchText}
              onChange={(event) => setSearchText(event.currentTarget.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') {
                  handleSearch();
                }
              }}
              leftSection={<IconSearch size={16} />}
            />
          </Grid.Col>
          
          {/* Search Button */}
          <Grid.Col span={{ base: 12, md: 2 }}>
            <Button 
              onClick={handleSearch}
              loading={searchQuery.isLoading}
              fullWidth
            >
              Search
            </Button>
          </Grid.Col>
        </Grid>
      </Paper>
      
      {/* Incident List */}
      <Paper shadow="sm" p="md" radius="md">
        <Group justify="space-between" align="center" mb="md">
          <Title order={3} size="h4">
            Safety Incidents
          </Title>
          <Group>
            <Text size="sm" c="dimmed">
              {searchQuery.data ? `${searchQuery.data.total} incidents found` : ''}
            </Text>
          </Group>
        </Group>
        
        {searchQuery.isLoading ? (
          <Group justify="center" p="xl">
            <Loader size="lg" />
          </Group>
        ) : searchQuery.error ? (
          <Alert variant="light" color="red">
            <Text size="sm">Failed to load incidents. Please try again.</Text>
          </Alert>
        ) : (
          <IncidentList
            incidents={searchQuery.data?.items || []}
            totalCount={searchQuery.data?.total || 0}
            currentPage={searchFilters.page || 1}
            pageSize={searchFilters.pageSize || 25}
            onPageChange={(page) => handleFilterChange('page', page)}
            onIncidentSelect={handleIncidentSelect}
            loading={searchQuery.isLoading}
          />
        )}
      </Paper>
      
      {/* Incident Details Modal */}
      <Modal
        opened={detailsOpened}
        onClose={closeDetails}
        title="Incident Details"
        size="xl"
        scrollAreaComponent={Modal.CloseButton}
      >
        {selectedIncidentId && (
          <IncidentDetails
            incidentId={selectedIncidentId}
            onClose={closeDetails}
          />
        )}
      </Modal>
    </Box>
  );
}