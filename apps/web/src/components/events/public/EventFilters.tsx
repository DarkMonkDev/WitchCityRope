import React from 'react';
import { Paper, Group, Chip, Select, Text, Stack, Anchor } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';

export interface EventFiltersState {
  eventType: 'all' | 'classes' | 'member-only' | 'social';
  instructor?: string | null;
  dateRange: 'week' | 'month' | 'custom';
}

interface EventFiltersProps {
  filters: EventFiltersState;
  instructors: string[];
  onFiltersChange: (filters: EventFiltersState) => void;
  resultCount: number;
  isLoading?: boolean;
}

export const EventFilters: React.FC<EventFiltersProps> = ({
  filters,
  instructors,
  onFiltersChange,
  resultCount,
  isLoading = false
}) => {
  const isMobile = useMediaQuery('(max-width: 767px)');

  const handleEventTypeChange = (value: string | null) => {
    if (value && (value === 'all' || value === 'classes' || value === 'member-only' || value === 'social')) {
      onFiltersChange({
        ...filters,
        eventType: value
      });
    }
  };

  const handleInstructorChange = (value: string | null) => {
    onFiltersChange({
      ...filters,
      instructor: value
    });
  };

  const instructorOptions = instructors.map(instructor => ({
    value: instructor,
    label: instructor
  }));

  if (isMobile) {
    return (
      <Paper withBorder shadow="sm" p="md" radius="md">
        <Stack gap="md">
          <Group justify="space-between" align="center">
            <Text fw={600} size="lg">Filter Events</Text>
            <Text size="sm" c="dimmed">
              {isLoading ? 'Loading...' : `${resultCount} events found`}
            </Text>
          </Group>

          {/* Mobile Layout - Vertical Stack */}
          <Stack gap="sm">
            {/* Event Type Filter */}
            <Stack gap="xs">
              <Text size="sm" fw={500}>Event Type</Text>
              <Chip.Group
                value={filters.eventType}
                onChange={handleEventTypeChange}
              >
                <Stack gap="xs">
                  <Group gap="xs">
                    <Chip value="all" color="burgundy">All Events</Chip>
                    <Chip value="classes" color="burgundy">Classes Only</Chip>
                  </Group>
                  <Group gap="xs">
                    <Chip value="social" color="burgundy">Social Events</Chip>
                    <Chip value="member-only" color="burgundy">Member Events Only</Chip>
                  </Group>
                </Stack>
              </Chip.Group>
            </Stack>

            {/* Instructor Filter */}
            <Select
              label="Filter by instructor"
              placeholder="All instructors"
              data={instructorOptions}
              value={filters.instructor}
              onChange={handleInstructorChange}
              clearable
            />

            {/* Past Events Link */}
            <Anchor href="/events/past" c="burgundy.7" size="sm">
              View Past Events →
            </Anchor>
          </Stack>
        </Stack>
      </Paper>
    );
  }

  return (
    <Paper withBorder shadow="sm" p="md" radius="md">
      <Stack gap="md">
        <Group justify="space-between" align="center">
          <Text fw={600} size="lg">Filter Events</Text>
          <Text size="sm" c="dimmed">
            {isLoading ? 'Loading...' : `${resultCount} events found`}
          </Text>
        </Group>

        {/* Desktop Layout - Horizontal */}
        <Group gap="md" wrap="wrap">
          {/* Event Type Filter */}
          <Chip.Group
            value={filters.eventType}
            onChange={handleEventTypeChange}
          >
            <Group gap="xs">
              <Chip value="all" color="burgundy">All Events</Chip>
              <Chip value="classes" color="burgundy">Classes Only</Chip>
              <Chip value="social" color="burgundy">Social Events</Chip>
              <Chip value="member-only" color="burgundy">Member Events Only</Chip>
            </Group>
          </Chip.Group>

          {/* Instructor Filter */}
          <Select
            placeholder="Filter by instructor"
            data={instructorOptions}
            value={filters.instructor}
            onChange={handleInstructorChange}
            clearable
            w={200}
          />

          {/* Past Events Link */}
          <Anchor href="/events/past" c="burgundy.7" size="sm">
            View Past Events →
          </Anchor>
        </Group>
      </Stack>
    </Paper>
  );
};