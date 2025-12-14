import React from 'react';
import { Group, TextInput, Switch, Text } from '@mantine/core';
import { IconSearch } from '@tabler/icons-react';
import type { AdminEventFiltersState } from '../../hooks/useAdminEventFilters';

interface EventsFilterBarProps {
  filterState: AdminEventFiltersState;
  rawSearchTerm: string; // For controlled input
  onFilterChange: (updates: Partial<AdminEventFiltersState>) => void;
}

export const EventsFilterBar: React.FC<EventsFilterBarProps> = ({
  filterState,
  rawSearchTerm,
  onFilterChange
}) => {
  return (
    <Group mb="lg" justify="space-between" align="center" wrap="nowrap">
      {/* Left side: Filter controls */}
      <Group align="center" gap="md">
        <Switch
          label="Show Past Events"
          labelPosition="left"
          checked={filterState.showPastEvents}
          onChange={(event) =>
            onFilterChange({ showPastEvents: event.currentTarget.checked })
          }
          data-testid="switch-show-past-events"
          styles={{
            label: {
              fontWeight: 500,
              fontSize: '14px',
              color: 'var(--mantine-color-gray-7)'
            }
          }}
        />
        {filterState.showPastEvents && (
          <Text size="xs" c="dimmed">(last 365 days)</Text>
        )}
      </Group>

      {/* Right side: Search */}
      <TextInput
        placeholder="Search events..."
        leftSection={<IconSearch size="1rem" />}
        value={rawSearchTerm}
        onChange={(event) => onFilterChange({ searchTerm: event.currentTarget.value })}
        data-testid="input-search-events"
        style={{ minWidth: 300 }}
        styles={{
          input: {
            backgroundColor: 'var(--mantine-color-gray-0)',
            borderColor: 'var(--mantine-color-wcr-4)',
            fontSize: '14px',
            '&:focus': {
              borderColor: 'var(--mantine-color-wcr-7)',
              boxShadow: '0 0 0 3px rgba(136, 1, 36, 0.15)'
            }
          }
        }}
      />
    </Group>
  );
};