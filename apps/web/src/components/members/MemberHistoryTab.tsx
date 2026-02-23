import React from 'react'
import { Stack, Paper, Text, Group, Alert, Box } from '@mantine/core'
import { IconAlertCircle, IconArrowRight } from '@tabler/icons-react'
import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../lib/api/client'
import type { components } from '@witchcityrope/shared-types'

// Use auto-generated type from API schema
type ProfileChangeHistoryItem = components['schemas']['ProfileChangeHistoryDto']

interface MemberHistoryTabProps {
  memberId: string
}

export const MemberHistoryTab: React.FC<MemberHistoryTabProps> = ({ memberId }) => {
  const { data: history, isLoading, error } = useQuery<ProfileChangeHistoryItem[]>({
    queryKey: ['member-profile-history', memberId],
    queryFn: async () => {
      const response = await apiClient.get(`/api/users/${memberId}/profile-change-history`)
      return response.data
    },
    enabled: !!memberId,
  })

  if (isLoading) {
    return (
      <Box p="xl" style={{ textAlign: 'center' }}>
        <Text c="dimmed">Loading profile change history...</Text>
      </Box>
    )
  }

  if (error) {
    return (
      <Alert icon={<IconAlertCircle size={16} />} color="red" title="Error Loading History">
        Failed to load profile change history. Please try again later.
      </Alert>
    )
  }

  if (!history || history.length === 0) {
    return (
      <Box p="xl" style={{ textAlign: 'center' }}>
        <Text c="dimmed" size="lg">
          No profile changes recorded yet.
        </Text>
        <Text c="dimmed" size="sm" mt="xs">
          Profile changes will appear here when the member updates their profile.
        </Text>
      </Box>
    )
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    })
  }

  return (
    <Stack gap="md">
      {history.map((change, index) => (
        <Paper
          key={index}
          shadow="sm"
          px="xl"
          py="md"
          radius="md"
          withBorder
        >
          {/* Date/Time */}
          <Text size="sm" c="dimmed" mb="xs">
            {formatDate(change.changedAt || '')}
          </Text>

          {/* Field Name and Change */}
          <Group gap="md" align="center">
            <Text fw={600} size="sm" style={{ color: '#2B2B2B', minWidth: '120px' }}>
              {change.fieldName}:
            </Text>
            <Group gap="xs" align="center" wrap="nowrap">
              <Text
                size="sm"
                c="dimmed"
                style={{
                  fontStyle: 'italic',
                  maxWidth: '300px',
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                }}
              >
                {change.oldValue || '(empty)'}
              </Text>
              <IconArrowRight size={16} color="#880124" />
              <Text
                size="sm"
                fw={500}
                style={{
                  color: '#880124',
                  maxWidth: '300px',
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                }}
              >
                {change.newValue || '(empty)'}
              </Text>
            </Group>
          </Group>
        </Paper>
      ))}
    </Stack>
  )
}
