import React from 'react'
import { Stack, Table, Text, Paper, Badge, Alert } from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import { useMemberIncidents } from '../../lib/api/hooks/useMemberDetails'
import { useNavigate } from 'react-router-dom'

interface MemberIncidentsTabProps {
  memberId: string
}

export const MemberIncidentsTab: React.FC<MemberIncidentsTabProps> = ({ memberId }) => {
  const navigate = useNavigate()
  const { data: incidentsData, isLoading, error } = useMemberIncidents(memberId)

  if (isLoading) {
    return (
      <Paper p="xl">
        <Text ta="center" c="dimmed">
          Loading incidents...
        </Text>
      </Paper>
    )
  }

  if (error) {
    return (
      <Alert icon={<IconAlertCircle size={16} />} color="red" title="Error">
        <Text>Failed to load incidents: {(error as Error).message}</Text>
      </Alert>
    )
  }

  if (!incidentsData || incidentsData.incidents.length === 0) {
    return (
      <Alert icon={<IconAlertCircle size={16} />} color="blue" title="No Incidents">
        <Text>This member has no incident involvement on record.</Text>
      </Alert>
    )
  }

  const statusColors: Record<string, string> = {
    Draft: 'gray',
    Submitted: 'blue',
    UnderReview: 'orange',
    Resolved: 'green',
    Closed: 'gray',
  }

  const involvementColors: Record<string, string> = {
    Reporter: 'blue',
    Subject: 'red',
    Witness: 'purple',
  }

  const handleRowClick = (incidentId: string) => {
    // Navigate to incident detail page
    navigate(`/admin/safety/incidents/${incidentId}`)
  }

  return (
    <Stack gap="md">
      <Table
        striped
        highlightOnHover
        withTableBorder
        style={{
          backgroundColor: 'white',
          borderRadius: '12px',
          overflow: 'hidden',
          boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
          cursor: 'pointer',
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
              Title
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              People Involved
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
              Role
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Status
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Location
            </Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {incidentsData.incidents.map((incident) => (
            <Table.Tr
              key={incident.incidentId}
              onClick={() => handleRowClick(incident.incidentId)}
              style={{ cursor: 'pointer' }}
            >
              <Table.Td>
                <Text size="sm" fw={600}>
                  {incident.title || incident.referenceNumber}
                </Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                  {incident.involvedParties || '-'}
                </Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm">{new Date(incident.incidentDate).toLocaleDateString()}</Text>
              </Table.Td>
              <Table.Td>
                <Badge
                  color={involvementColors[incident.userInvolvementType] || 'gray'}
                  variant="light"
                >
                  {incident.userInvolvementType}
                </Badge>
              </Table.Td>
              <Table.Td>
                <Badge color={statusColors[incident.status] || 'gray'} variant="light">
                  {incident.status}
                </Badge>
              </Table.Td>
              <Table.Td>
                <Text size="sm">{incident.location}</Text>
              </Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>
    </Stack>
  )
}
