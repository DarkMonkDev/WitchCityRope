/**
 * PrincipalList - Read-only list of people who authorized the current user
 *
 * Shows the "People you can act for" section. This list is managed by others
 * and the current user cannot remove themselves from it.
 *
 * Each row shows the principal's scene name and the date they added
 * the current user as a delegate.
 */
import React from 'react'
import {
  Stack,
  Group,
  Paper,
  Text,
} from '@mantine/core'
import type { AuthorizedContactDto } from '../types/authorizedContact.types'

interface PrincipalListProps {
  /** Array of principal contacts (people who authorized the current user) */
  principals: AuthorizedContactDto[]
}

/**
 * Format a date string for display.
 * Shows abbreviated month and day on mobile, full format on desktop.
 * Example: "Mar 15, 2026"
 */
function formatDate(dateString: string): string {
  const date = new Date(dateString)
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

export const PrincipalList: React.FC<PrincipalListProps> = ({ principals }) => {
  // Empty state
  if (principals.length === 0) {
    return (
      <Text size="sm" c="dimmed" fs="italic">
        No one has authorized you yet.
      </Text>
    )
  }

  return (
    <Stack gap="xs">
      {principals.map((principal) => (
        <Paper
          key={principal.id}
          p="sm"
          withBorder
          style={{
            borderColor: 'var(--color-taupe)',
          }}
          data-testid={`principal-row-${principal.id}`}
        >
          <Group justify="space-between" align="center">
            <Text fw={500}>{principal.sceneName}</Text>
            <Text size="sm" c="dimmed">
              added {formatDate(principal.createdAt)}
            </Text>
          </Group>
        </Paper>
      ))}

      {/* Explanatory text per UI design */}
      <Text size="sm" c="dimmed" fs="italic" mt="xs">
        This list is managed by others. You cannot remove yourself.
      </Text>
    </Stack>
  )
}
