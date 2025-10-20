/**
 * Admin Members List Component
 *
 * Pattern: Copied EXACTLY from VettingApplicationsList.tsx
 * - Same filter paper with background '#FFF8F0'
 * - Same search bar
 * - Same MultiSelect for Role filtering
 * - Same Clear Filters button
 * - Same Table with exactly styled headers
 * - Same Pagination component
 */

import React, { useState, useMemo, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Table,
  Text,
  Badge,
  Group,
  TextInput,
  MultiSelect,
  Button,
  Paper,
  Stack,
  Pagination,
  Box,
  rem,
} from '@mantine/core';
import {
  IconSearch,
  IconFilter,
  IconRefresh,
  IconSortAscending,
  IconSortDescending,
} from '@tabler/icons-react';
import { useMembers } from '../hooks/useMembers';
import type { UserDto, MemberFilterRequest } from '../types/members.types';

export const MembersList: React.FC = () => {
  const navigate = useNavigate();
  const [filters, setFilters] = useState<MemberFilterRequest>(() => ({
    page: 1,
    pageSize: 25,
    roleFilters: [], // Start with no filters - show all members
    searchQuery: '',
    sortBy: 'CreatedAt',
    sortDirection: 'Desc',
  }));

  const { data, isLoading, error, refetch } = useMembers(filters);

  const handleFilterChange = useCallback(
    (field: keyof MemberFilterRequest, value: any) => {
      setFilters((prev) => ({
        ...prev,
        [field]: value,
        page: field !== 'page' ? 1 : value, // Reset to page 1 when filters change
      }));
    },
    []
  );

  const handleSort = useCallback((field: string) => {
    setFilters((prev) => ({
      ...prev,
      sortBy: field,
      sortDirection:
        prev.sortBy === field && prev.sortDirection === 'Asc' ? 'Desc' : 'Asc',
    }));
  }, []);

  const handleRowClick = useCallback(
    (userId: string) => {
      // Use setTimeout to defer navigation to next tick
      // This allows Outlet to properly unmount old component and mount new one
      setTimeout(() => {
        navigate(`/admin/members/${userId}`);
      }, 0);
    },
    [navigate]
  );

  const formatDate = useCallback((dateString?: string | null) => {
    if (!dateString) return 'Never';
    return new Date(dateString).toLocaleDateString();
  }, []);

  const getSortIcon = useCallback(
    (field: string) => {
      if (filters.sortBy !== field) return null;
      return filters.sortDirection === 'Asc' ? (
        <IconSortAscending size={16} />
      ) : (
        <IconSortDescending size={16} />
      );
    },
    [filters.sortBy, filters.sortDirection]
  );

  // Role filter options matching the current valid roles
  const roleOptions = useMemo(
    () => [
      { value: 'Teacher', label: 'Teacher' },
      { value: 'Administrator', label: 'Administrator' },
      { value: 'SafetyTeam', label: 'Safety Team' },
    ],
    []
  );

  // Format role names for display (e.g., "SafetyTeam" -> "Safety Team")
  const formatRoleName = (role: string): string => {
    const roleMap: Record<string, string> = {
      'SafetyTeam': 'Safety Team',
      'Teacher': 'Teacher',
      'Administrator': 'Administrator',
    };
    return roleMap[role] || role;
  };

  // Get status badge color based on vetting status
  // VettingStatus enum: 0=UnderReview, 1=InterviewApproved, 2=FinalReview, 3=Approved, 4=Denied, 5=OnHold, 6=Withdrawn
  const getVettingStatusBadge = (user: UserDto) => {
    switch (user.vettingStatus) {
      case 3: // Approved
        return <Badge color="green">Vetted</Badge>;
      case 0: // UnderReview
        // Only show "Under Review" if they actually have an application
        // Otherwise they haven't applied yet
        return user.hasVettingApplication ? (
          <Badge color="yellow">Under Review</Badge>
        ) : (
          <Badge color="gray">Not Applied</Badge>
        );
      case 1: // InterviewApproved
        return <Badge color="blue">Interview Approved</Badge>;
      case 2: // FinalReview
        return <Badge color="cyan">Final Review</Badge>;
      case 4: // Denied
        return <Badge color="red">Denied</Badge>;
      case 5: // OnHold
        return <Badge color="orange">On Hold</Badge>;
      case 6: // Withdrawn
        return <Badge color="gray">Withdrawn</Badge>;
      default:
        return <Badge color="gray">Unknown</Badge>;
    }
  };

  if (error) {
    return (
      <Paper p="xl" radius="md">
        <Text c="red" ta="center">
          Error loading members: {error.message}
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
            placeholder="Search by scene name, email, or Discord name..."
            leftSection={<IconSearch size={16} />}
            value={filters.searchQuery || ''}
            onChange={(e) => handleFilterChange('searchQuery', e.currentTarget.value)}
            style={{
              minWidth: rem(400),
              flex: 1,
            }}
            styles={{
              input: {
                fontSize: '16px',
                height: '42px',
              },
            }}
          />

          <Group gap="md">
            <MultiSelect
              data-testid="role-filter"
              placeholder="Filter by role"
              data={roleOptions}
              value={filters.roleFilters}
              onChange={(values) => handleFilterChange('roleFilters', values)}
              leftSection={<IconFilter size={16} />}
              clearable
              style={{ minWidth: rem(220) }}
              styles={{
                input: {
                  fontSize: '16px',
                  height: '42px',
                },
              }}
            />
          </Group>
        </Group>
      </Paper>

      {/* Members Table - Exactly matching wireframe */}
      <Paper shadow="sm" radius="md" data-testid="members-table">
        <Table striped highlightOnHover data-testid="members-table-inner">
          <Table.Thead
            style={{
              backgroundColor: '#880124',
              color: 'white',
            }}
          >
            <Table.Tr>
              {/* Scene Name column - sortable */}
              <Table.Th
                style={{
                  backgroundColor: '#880124',
                  borderBottom: 'none',
                  cursor: 'pointer',
                }}
                onClick={() => handleSort('SceneName')}
              >
                <Group gap={4} justify="flex-start">
                  <Text
                    fw={600}
                    size="sm"
                    style={{
                      color: 'white',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                    }}
                  >
                    SCENE NAME
                  </Text>
                  {getSortIcon('SceneName')}
                </Group>
              </Table.Th>

              {/* Email column - sortable */}
              <Table.Th
                style={{
                  backgroundColor: '#880124',
                  borderBottom: 'none',
                  cursor: 'pointer',
                }}
                onClick={() => handleSort('Email')}
              >
                <Group gap={4} justify="flex-start">
                  <Text
                    fw={600}
                    size="sm"
                    style={{
                      color: 'white',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                    }}
                  >
                    EMAIL
                  </Text>
                  {getSortIcon('Email')}
                </Group>
              </Table.Th>

              {/* Discord Name column - sortable */}
              <Table.Th
                style={{
                  backgroundColor: '#880124',
                  borderBottom: 'none',
                  cursor: 'pointer',
                }}
                onClick={() => handleSort('DiscordName')}
              >
                <Group gap={4} justify="flex-start">
                  <Text
                    fw={600}
                    size="sm"
                    style={{
                      color: 'white',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                    }}
                  >
                    DISCORD NAME
                  </Text>
                  {getSortIcon('DiscordName')}
                </Group>
              </Table.Th>

              {/* Roles column */}
              <Table.Th
                style={{ backgroundColor: '#880124', borderBottom: 'none' }}
              >
                <Text
                  fw={600}
                  size="sm"
                  style={{
                    color: 'white',
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                  }}
                >
                  ROLES
                </Text>
              </Table.Th>

              {/* Vetting Status column - sortable and center-aligned */}
              <Table.Th
                style={{
                  backgroundColor: '#880124',
                  borderBottom: 'none',
                  cursor: 'pointer',
                  textAlign: 'center',
                }}
                onClick={() => handleSort('VettingStatus')}
              >
                <Group gap={4} justify="center">
                  <Text
                    fw={600}
                    size="sm"
                    style={{
                      color: 'white',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                    }}
                  >
                    VETTING STATUS
                  </Text>
                  {getSortIcon('VettingStatus')}
                </Group>
              </Table.Th>

              {/* Active Status column - sortable and center-aligned */}
              <Table.Th
                style={{
                  backgroundColor: '#880124',
                  borderBottom: 'none',
                  cursor: 'pointer',
                  textAlign: 'center',
                }}
                onClick={() => handleSort('IsActive')}
              >
                <Group gap={4} justify="center">
                  <Text
                    fw={600}
                    size="sm"
                    style={{
                      color: 'white',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                    }}
                  >
                    ACTIVE STATUS
                  </Text>
                  {getSortIcon('IsActive')}
                </Group>
              </Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {data?.users?.map((member) => (
              <Table.Tr
                key={member.id}
                onClick={() => handleRowClick(member.id!)}
                style={{
                  cursor: 'pointer',
                }}
              >
                {/* Scene Name */}
                <Table.Td>
                  <Text size="sm" fw={600} style={{ color: '#2B2B2B' }}>
                    {member.sceneName || 'No scene name'}
                  </Text>
                </Table.Td>

                {/* Email */}
                <Table.Td>
                  <Text size="sm" style={{ color: '#2B2B2B' }}>
                    {member.email}
                  </Text>
                </Table.Td>

                {/* Discord Name */}
                <Table.Td>
                  <Text size="sm" style={{ color: '#2B2B2B' }}>
                    {member.discordName || '-'}
                  </Text>
                </Table.Td>

                {/* Roles */}
                <Table.Td>
                  {member.role ? (
                    <Group gap="xs">
                      {member.role.split(',').map((role, idx) => (
                        <Badge key={idx} color="blue" size="sm">
                          {formatRoleName(role.trim())}
                        </Badge>
                      ))}
                    </Group>
                  ) : (
                    <Text size="sm" c="dimmed">-</Text>
                  )}
                </Table.Td>

                {/* Vetting Status - center-aligned */}
                <Table.Td style={{ textAlign: 'center' }}>
                  {getVettingStatusBadge(member)}
                </Table.Td>

                {/* Active Status - center-aligned */}
                <Table.Td style={{ textAlign: 'center' }}>
                  <Badge color={member.isActive ? 'green' : 'gray'}>
                    {member.isActive ? 'Active' : 'Inactive'}
                  </Badge>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>

        {/* Empty State */}
        {data?.users?.length === 0 && !isLoading && (
          <Box p="xl" ta="center">
            <Text c="dimmed" size="lg">
              {filters.searchQuery || filters.roleFilters.length > 0
                ? 'No members match your filters'
                : 'No members found'}
            </Text>
            {(filters.searchQuery || filters.roleFilters.length > 0) && (
              <Button
                variant="light"
                mt="md"
                onClick={() =>
                  setFilters({
                    page: 1,
                    pageSize: 25,
                    roleFilters: [],
                    searchQuery: '',
                    sortBy: 'CreatedAt',
                    sortDirection: 'Desc',
                  })
                }
              >
                Clear Filters
              </Button>
            )}
          </Box>
        )}

        {/* Loading State */}
        {isLoading && (
          <Box p="xl" ta="center">
            <Text c="dimmed">Loading members...</Text>
          </Box>
        )}
      </Paper>

      {/* Pagination - Matching wireframe style */}
      {data && data.totalCount && data.totalCount > filters.pageSize && (
        <Group justify="space-between" align="center">
          <Text size="sm" c="dimmed">
            Showing {(filters.page - 1) * filters.pageSize + 1}-
            {Math.min(filters.page * filters.pageSize, data.totalCount)} of{' '}
            {data.totalCount} members
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
                  borderColor: '#880124',
                },
              },
            }}
          />
        </Group>
      )}
    </Stack>
  );
};
