import React, { useState } from 'react'
import { Stack, Table, Text, Paper, Group, Pagination, Alert } from '@mantine/core'
import { IconAlertCircle, IconCheck, IconX } from '@tabler/icons-react'
import { useMemberVolunteerHistory } from '../../lib/api/hooks/useMemberDetails'

interface MemberVolunteerTabProps {
  memberId: string
}

export const MemberVolunteerTab: React.FC<MemberVolunteerTabProps> = ({ memberId }) => {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)

  const { data: volunteerHistory, isLoading, error } = useMemberVolunteerHistory(
    memberId,
    page,
    pageSize
  )

  if (isLoading) {
    return (
      <Paper p="xl">
        <Text ta="center" c="dimmed">
          Loading volunteer history...
        </Text>
      </Paper>
    )
  }

  if (error) {
    return (
      <Alert icon={<IconAlertCircle size={16} />} color="red" title="Error">
        <Text>Failed to load volunteer history: {(error as Error).message}</Text>
      </Alert>
    )
  }

  if (!volunteerHistory || volunteerHistory.volunteers.length === 0) {
    return (
      <Alert icon={<IconAlertCircle size={16} />} color="blue" title="No Volunteer Sessions">
        <Text>This member has no volunteer participation history yet.</Text>
      </Alert>
    )
  }

  return (
    <Stack gap="md">
      <Text size="sm" c="dimmed">
        Showing {volunteerHistory.volunteers.length} of {volunteerHistory.totalCount} volunteer
        sessions
      </Text>

      {/* Volunteer Sessions Table */}
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
              Role/Position
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Showed Up
            </Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {volunteerHistory.volunteers.map((volunteer) => (
            <Table.Tr key={`${volunteer.eventId}-${volunteer.eventDate}`}>
              <Table.Td>
                <Text fw={500}>{volunteer.eventTitle}</Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm">{new Date(volunteer.eventDate).toLocaleDateString()}</Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm">{volunteer.role}</Text>
              </Table.Td>
              <Table.Td>
                <Group gap="xs" justify="center">
                  {volunteer.showedUp ? (
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
      {volunteerHistory.totalPages > 1 && (
        <Group justify="center" mt="md">
          <Pagination
            value={page}
            onChange={setPage}
            total={volunteerHistory.totalPages}
            color="burgundy"
          />
        </Group>
      )}
    </Stack>
  )
}
