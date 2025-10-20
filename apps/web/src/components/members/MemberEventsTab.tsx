import React, { useState } from 'react'
import { Stack, Table, Text, Paper, Badge, Group, Pagination, Alert } from '@mantine/core'
import { IconAlertCircle, IconCheck, IconX } from '@tabler/icons-react'
import { useMemberEventHistory } from '../../lib/api/hooks/useMemberDetails'

interface MemberEventsTabProps {
  memberId: string
}

export const MemberEventsTab: React.FC<MemberEventsTabProps> = ({ memberId }) => {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)

  const { data: eventHistory, isLoading, error } = useMemberEventHistory(memberId, page, pageSize)

  if (isLoading) {
    return (
      <Paper p="xl">
        <Text ta="center" c="dimmed">
          Loading event history...
        </Text>
      </Paper>
    )
  }

  if (error) {
    return (
      <Alert icon={<IconAlertCircle size={16} />} color="red" title="Error">
        <Text>Failed to load event history: {(error as Error).message}</Text>
      </Alert>
    )
  }

  if (!eventHistory || eventHistory.events.length === 0) {
    return (
      <Alert icon={<IconAlertCircle size={16} />} color="blue" title="No Events">
        <Text>This member has no event participation history yet.</Text>
      </Alert>
    )
  }

  const registrationTypeColors: Record<string, string> = {
    RSVP: 'blue',
    Ticket: 'green',
    Volunteer: 'purple',
  }

  return (
    <Stack gap="md">
      <Text size="sm" c="dimmed">
        Showing {eventHistory.events.length} of {eventHistory.totalCount} events
      </Text>

      {/* Events Table */}
      <Table
        striped
        highlightOnHover
        withTableBorder
        style={{
          backgroundColor: 'white',
          borderRadius: '12px',
          overflow: 'hidden',
          boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
        }}
      >
        <Table.Thead style={{ backgroundColor: 'var(--mantine-color-burgundy-6)' }}>
          <Table.Tr>
            <Table.Th
              style={{
                color: 'white',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Event Title
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Date
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Type
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Registration Type
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Attended
            </Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {eventHistory.events.map((event) => (
            <Table.Tr key={event.eventId}>
              <Table.Td>
                <Text fw={500}>{event.eventTitle}</Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm">{new Date(event.eventDate).toLocaleDateString()}</Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm">{event.eventType}</Text>
              </Table.Td>
              <Table.Td>
                <Badge
                  color={registrationTypeColors[event.registrationType] || 'gray'}
                  variant="light"
                >
                  {event.registrationType}
                </Badge>
              </Table.Td>
              <Table.Td>
                <Group gap="xs" justify="center">
                  {event.attended ? (
                    <IconCheck size={20} color="green" stroke={2.5} />
                  ) : (
                    <IconX size={20} color="red" stroke={2.5} />
                  )}
                </Group>
              </Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>

      {/* Pagination */}
      {eventHistory.totalPages > 1 && (
        <Group justify="center" mt="md">
          <Pagination
            value={page}
            onChange={setPage}
            total={eventHistory.totalPages}
            color="burgundy"
          />
        </Group>
      )}
    </Stack>
  )
}
