// AttendeeSearch component for CheckIn system
// Mobile-first search interface with real-time filtering

import React, { useState, useCallback, useMemo } from 'react';
import {
  TextInput,
  ActionIcon,
  Group,
  Select,
  Box,
  Text,
  Stack
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconSearch, IconQrcode, IconFilter } from '@tabler/icons-react';
import type { AttendeeSearchFormData, RegistrationStatus } from '../types/checkin.types';
import { STATUS_CONFIGS, TOUCH_TARGETS } from '../types/checkin.types';

interface AttendeeSearchProps {
  onSearch: (searchTerm: string) => void;
  onStatusFilter: (status: RegistrationStatus | 'all') => void;
  onScanQR?: () => void;
  searchValue: string;
  statusFilter: RegistrationStatus | 'all';
  isLoading?: boolean;
  resultCount?: number;
  placeholder?: string;
  disabled?: boolean;
}

/**
 * Mobile-optimized search interface for finding attendees
 * Features floating labels, touch-friendly controls, and real-time search
 */
export function AttendeeSearch({
  onSearch,
  onStatusFilter,
  onScanQR,
  searchValue,
  statusFilter,
  isLoading = false,
  resultCount,
  placeholder = "Search name, email, or ticket #",
  disabled = false
}: AttendeeSearchProps) {
  const [searchInput, setSearchInput] = useState(searchValue);
  const [debouncedSearch] = useDebouncedValue(searchInput, 300);

  // Trigger search when debounced value changes
  React.useEffect(() => {
    if (debouncedSearch !== searchValue) {
      onSearch(debouncedSearch);
    }
  }, [debouncedSearch, searchValue, onSearch]);

  const handleSearchChange = useCallback((value: string) => {
    setSearchInput(value);
  }, []);

  const handleStatusChange = useCallback((value: string | null) => {
    onStatusFilter((value as RegistrationStatus | 'all') || 'all');
  }, [onStatusFilter]);

  // Status filter options
  const statusOptions = useMemo(() => [
    { value: 'all', label: 'All Attendees' },
    ...Object.entries(STATUS_CONFIGS).map(([status, config]) => ({
      value: status,
      label: `${config.icon} ${config.label}`
    }))
  ], []);

  // Result count text
  const resultText = useMemo(() => {
    if (resultCount === undefined) return '';
    if (isLoading) return 'Searching...';
    return `${resultCount} ${resultCount === 1 ? 'result' : 'results'}`;
  }, [resultCount, isLoading]);

  return (
    <Stack gap="md">
      {/* Search Input with QR Scanner */}
      <Box style={{ position: 'relative' }}>
        <TextInput
          value={searchInput}
          onChange={(event) => handleSearchChange(event.currentTarget.value)}
          placeholder={placeholder}
          size="lg"
          disabled={disabled}
          leftSection={
            <IconSearch size={20} style={{ color: 'var(--mantine-color-dimmed)' }} />
          }
          rightSection={
            onScanQR && (
              <ActionIcon
                size="lg"
                variant="subtle"
                onClick={onScanQR}
                disabled={disabled}
                aria-label="Scan QR code"
                style={{
                  minWidth: TOUCH_TARGETS.MINIMUM,
                  minHeight: TOUCH_TARGETS.MINIMUM
                }}
              >
                <IconQrcode size={20} />
              </ActionIcon>
            )
          }
          styles={{
            input: {
              minHeight: TOUCH_TARGETS.SEARCH_INPUT_HEIGHT,
              fontSize: '16px', // Prevents zoom on iOS
              borderRadius: '12px',
              borderColor: '#B76D75',
              transition: 'all 0.3s ease',
              '&:focus': {
                borderColor: '#880124',
                transform: 'translateY(-2px)',
                boxShadow: '0 0 0 3px rgba(183, 109, 117, 0.2)'
              }
            }
          }}
        />
      </Box>

      {/* Status Filter and Results */}
      <Group justify="space-between" align="center" wrap="nowrap">
        <Group align="center" gap="sm">
          <IconFilter size={16} style={{ color: 'var(--mantine-color-dimmed)' }} />
          <Select
            value={statusFilter}
            onChange={handleStatusChange}
            data={statusOptions}
            size="sm"
            disabled={disabled}
            placeholder="Filter by status"
            variant="filled"
            styles={{
              input: {
                minHeight: '36px',
                borderRadius: '8px',
                backgroundColor: 'var(--mantine-color-gray-1)',
                border: 'none',
                fontSize: '14px'
              },
              dropdown: {
                borderRadius: '8px'
              }
            }}
          />
        </Group>

        {/* Result Count */}
        {resultText && (
          <Text 
            size="sm" 
            c="dimmed" 
            style={{ 
              fontFamily: 'Source Sans 3, sans-serif',
              fontWeight: 500
            }}
          >
            {resultText}
          </Text>
        )}
      </Group>
    </Stack>
  );
}

/**
 * Compact search component for smaller spaces
 * Used in modal dialogs or secondary interfaces
 */
export function CompactAttendeeSearch({
  onSearch,
  searchValue,
  placeholder = "Quick search...",
  disabled = false
}: {
  onSearch: (searchTerm: string) => void;
  searchValue: string;
  placeholder?: string;
  disabled?: boolean;
}) {
  const [searchInput, setSearchInput] = useState(searchValue);
  const [debouncedSearch] = useDebouncedValue(searchInput, 300);

  React.useEffect(() => {
    if (debouncedSearch !== searchValue) {
      onSearch(debouncedSearch);
    }
  }, [debouncedSearch, searchValue, onSearch]);

  return (
    <TextInput
      value={searchInput}
      onChange={(event) => setSearchInput(event.currentTarget.value)}
      placeholder={placeholder}
      size="sm"
      disabled={disabled}
      leftSection={<IconSearch size={16} />}
      styles={{
        input: {
          borderRadius: '8px',
          fontSize: '14px',
          minHeight: '36px'
        }
      }}
    />
  );
}