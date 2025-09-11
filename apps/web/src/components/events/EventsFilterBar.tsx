import React from 'react';
import { Stack, Group, Text, Chip, TextInput, Switch } from '@mantine/core';
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
        <Text size="sm" fw={500} c="dimmed">Filter:</Text>
        <Chip.Group
          multiple
          value={filterState.activeTypes}
          onChange={(types) => onFilterChange({ activeTypes: types as string[] })}
        >
          <Group gap="xs">
            <Chip 
              value="social" 
              variant="filled" 
              color="wcr"
              data-testid="filter-social"
              styles={{
                label: {
                  fontWeight: 600,
                  fontSize: '14px'
                }
              }}
            >
              Social
            </Chip>
            <Chip 
              value="class" 
              variant="filled" 
              color="wcr"
              data-testid="filter-class"
              styles={{
                label: {
                  fontWeight: 600,
                  fontSize: '14px'
                }
              }}
            >
              Class
            </Chip>
          </Group>
        </Chip.Group>
        
        <Switch
          label="Show Past Events"
          labelPosition="left"
          checked={filterState.showPastEvents}
          onChange={(event) =>
            onFilterChange({ showPastEvents: event.currentTarget.checked })
          }
          styles={{
            label: {
              fontWeight: 500,
              fontSize: '14px',
              color: 'var(--mantine-color-gray-7)'
            }
          }}
        />
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