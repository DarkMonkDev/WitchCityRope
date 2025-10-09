import React from 'react';
import { Group, Checkbox, SegmentedControl, TextInput, Box } from '@mantine/core';
import { IconSearch } from '@tabler/icons-react';

interface FilterBarProps {
  showPast: boolean;
  onShowPastChange: (show: boolean) => void;
  viewMode: 'grid' | 'table';
  onViewModeChange: (mode: 'grid' | 'table') => void;
  searchQuery: string;
  onSearchChange: (query: string) => void;
}

/**
 * Filter and view controls for user dashboard events
 * Features:
 * - Show Past Events checkbox
 * - View toggle (Grid/Table)
 * - Search input
 */
export const FilterBar: React.FC<FilterBarProps> = ({
  showPast,
  onShowPastChange,
  viewMode,
  onViewModeChange,
  searchQuery,
  onSearchChange,
}) => {
  return (
    <Box
      bg="var(--color-ivory)"
      style={{
        border: '1px solid var(--color-taupe)',
        borderRadius: '8px',
      }}
      p="md"
      mb="lg"
    >
      <Group justify="space-between" align="center" wrap="wrap" gap="md">
        <Group gap="md" align="center">
          {/* Show Past Events Checkbox */}
          <Checkbox
            label="Show Past Events"
            checked={showPast}
            onChange={(event) => onShowPastChange(event.currentTarget.checked)}
            color="burgundy"
            size="sm"
            styles={{
              label: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '14px',
                color: 'var(--color-burgundy)',
              },
            }}
          />

          {/* View Toggle */}
          <SegmentedControl
            value={viewMode}
            onChange={(value) => onViewModeChange(value as 'grid' | 'table')}
            data={[
              { label: 'Card View', value: 'grid' },
              { label: 'List View', value: 'table' },
            ]}
            size="sm"
            color="burgundy"
            styles={{
              root: {
                background: 'var(--color-cream)',
                borderRadius: '25px',
                padding: '4px',
              },
              control: {
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
              },
            }}
          />
        </Group>

        {/* Search Input */}
        <TextInput
          placeholder="Search events..."
          value={searchQuery}
          onChange={(event) => onSearchChange(event.currentTarget.value)}
          leftSection={<IconSearch size={16} style={{ color: 'var(--color-stone)' }} />}
          w={250}
          styles={{
            input: {
              border: '2px solid var(--color-taupe)',
              borderRadius: '25px',
              fontFamily: 'var(--font-body)',
              fontSize: '14px',
              '&:focus': {
                borderColor: 'var(--color-burgundy)',
                width: '300px',
                transition: 'all 0.3s ease',
              },
            },
          }}
        />
      </Group>
    </Box>
  );
};
