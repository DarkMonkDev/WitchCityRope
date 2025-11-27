// EventAttendeesTab - Shows attendees list for an event with check-in status
// Part of admin event details page improvements

import React, { useState, useMemo } from 'react';
import {
  Box,
  Table,
  Text,
  Badge,
  Group,
  TextInput,
  Select,
  Stack,
  Alert,
  Loader,
  Center
} from '@mantine/core';
import { IconSearch } from '@tabler/icons-react';
import { useEventTimeZone } from '../../../hooks/useEventTimeZone';

// TODO: Import proper types once check-in types are finalized
interface Attendee {
  id: string;
  sceneName: string;
  pronouns?: string;
  registrationStatus: 'Registered' | 'CheckedIn' | 'NoShow' | 'Waitlist';
  checkInTime?: string;
}

interface EventAttendeesTabProps {
  eventId: string;
  eventHasPassed: boolean;
}

/**
 * EventAttendeesTab Component
 *
 * Displays attendee list similar to check-in page but with additional data:
 * - Attendee name (scene name)
 * - Pronouns
 * - Registration status
 * - Check-in time (if checked in)
 * - "No Show" if event has passed and they didn't check in
 *
 * Features:
 * - Sortable table
 * - Searchable by name
 * - Filterable by status
 * - Status badges with clear visual distinction
 */
export const EventAttendeesTab: React.FC<EventAttendeesTabProps> = ({
  eventId,
  eventHasPassed
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const eventTimeZone = useEventTimeZone();

  // TODO: Replace with actual API call to fetch attendees
  // const { data: attendees, isLoading, error } = useEventAttendees(eventId);

  // Mock data for now
  const mockAttendees: Attendee[] = [];
  const isLoading = false;
  const error = null;

  // Filter attendees based on search and status
  const filteredAttendees = useMemo(() => {
    let filtered = mockAttendees;

    // Apply search filter
    if (searchTerm) {
      filtered = filtered.filter(attendee =>
        attendee.sceneName.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }

    // Apply status filter
    if (statusFilter !== 'all') {
      filtered = filtered.filter(attendee => attendee.registrationStatus === statusFilter);
    }

    return filtered;
  }, [mockAttendees, searchTerm, statusFilter]);

  // Calculate status counts for filter labels
  const statusCounts = useMemo(() => {
    return {
      all: mockAttendees.length,
      registered: mockAttendees.filter(a => a.registrationStatus === 'Registered').length,
      checkedIn: mockAttendees.filter(a => a.registrationStatus === 'CheckedIn').length,
      noShow: mockAttendees.filter(a => a.registrationStatus === 'NoShow').length,
      waitlist: mockAttendees.filter(a => a.registrationStatus === 'Waitlist').length
    };
  }, [mockAttendees]);

  // Get badge color based on status
  const getStatusBadge = (status: string, checkInTime?: string) => {
    const config = {
      Registered: { color: 'blue', label: 'Registered' },
      CheckedIn: { color: 'green', label: checkInTime ? `Checked In at ${new Date(checkInTime).toLocaleTimeString('en-US', { timeZone: eventTimeZone })}` : 'Checked In' },
      NoShow: { color: 'gray', label: 'No Show' },
      Waitlist: { color: 'orange', label: 'Waitlist' }
    };

    const { color, label } = config[status as keyof typeof config] || config.Registered;

    return (
      <Badge color={color} variant="light">
        {label}
      </Badge>
    );
  };

  // Determine final status (mark as "No Show" if event passed and not checked in)
  const getFinalStatus = (attendee: Attendee): string => {
    if (eventHasPassed && attendee.registrationStatus === 'Registered') {
      return 'NoShow';
    }
    return attendee.registrationStatus;
  };

  if (isLoading) {
    return (
      <Center p="xl">
        <Loader size="lg" />
      </Center>
    );
  }

  if (error) {
    return (
      <Alert color="red" title="Error Loading Attendees">
        <Text>Failed to load attendees. Please try again later.</Text>
      </Alert>
    );
  }

  return (
    <Stack gap="md">
      {/* Search and Filter Controls */}
      <Group grow align="flex-start">
        <TextInput
          placeholder="Search by name..."
          leftSection={<IconSearch size={16} />}
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />
        <Select
          placeholder="Filter by status"
          value={statusFilter}
          onChange={(value) => setStatusFilter(value || 'all')}
          data={[
            { value: 'all', label: `All (${statusCounts.all})` },
            { value: 'Registered', label: `Registered (${statusCounts.registered})` },
            { value: 'CheckedIn', label: `Checked In (${statusCounts.checkedIn})` },
            { value: 'NoShow', label: `No Show (${statusCounts.noShow})` },
            { value: 'Waitlist', label: `Waitlist (${statusCounts.waitlist})` }
          ]}
        />
      </Group>

      {/* Attendees Table */}
      {filteredAttendees.length === 0 ? (
        <Box p="xl" style={{ textAlign: 'center' }}>
          <Text c="dimmed" size="lg">
            {searchTerm || statusFilter !== 'all'
              ? 'No attendees match your filters'
              : 'No attendees registered yet'}
          </Text>
        </Box>
      ) : (
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Name</Table.Th>
              <Table.Th>Pronouns</Table.Th>
              <Table.Th>Status</Table.Th>
              <Table.Th>Check-In Time</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {filteredAttendees.map((attendee) => {
              const finalStatus = getFinalStatus(attendee);
              return (
                <Table.Tr key={attendee.id}>
                  <Table.Td>
                    <Text fw={600}>{attendee.sceneName}</Text>
                  </Table.Td>
                  <Table.Td>
                    <Text c="dimmed">{attendee.pronouns || '—'}</Text>
                  </Table.Td>
                  <Table.Td>
                    {getStatusBadge(finalStatus, attendee.checkInTime)}
                  </Table.Td>
                  <Table.Td>
                    {attendee.checkInTime ? (
                      <Text size="sm">
                        {new Date(attendee.checkInTime).toLocaleString('en-US', { timeZone: eventTimeZone })}
                      </Text>
                    ) : finalStatus === 'NoShow' ? (
                      <Text size="sm" c="dimmed">—</Text>
                    ) : (
                      <Text size="sm" c="dimmed">Not checked in</Text>
                    )}
                  </Table.Td>
                </Table.Tr>
              );
            })}
          </Table.Tbody>
        </Table>
      )}

      {/* Summary Stats */}
      <Group gap="md" justify="flex-start">
        <Text size="sm" c="dimmed">
          Total Attendees: <strong>{statusCounts.all}</strong>
        </Text>
        <Text size="sm" c="dimmed">
          Checked In: <strong>{statusCounts.checkedIn}</strong>
        </Text>
        {eventHasPassed && (
          <Text size="sm" c="dimmed">
            No Shows: <strong>{statusCounts.noShow}</strong>
          </Text>
        )}
      </Group>
    </Stack>
  );
};
