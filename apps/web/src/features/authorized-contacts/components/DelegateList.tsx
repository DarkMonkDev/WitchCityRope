/**
 * DelegateList - Shows people the current user has authorized
 *
 * Displays a list of delegates (people who can act on the current user's behalf).
 * Each row shows the delegate's scene name and a "Remove" button that triggers
 * a confirmation modal before removing the relationship.
 *
 * This is the "People who can act on your behalf" section.
 */
import React, { useState, useRef, useEffect } from 'react'
import {
  Stack,
  Group,
  Paper,
  Text,
  Button,
  Modal,
} from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { IconCheck, IconAlertCircle } from '@tabler/icons-react'
import { useRemoveAuthorizedContact } from '../api/mutations'
import type { AuthorizedContactDto } from '../types/authorizedContact.types'

interface DelegateListProps {
  /** Array of delegate contacts (people authorized to act on user's behalf) */
  delegates: AuthorizedContactDto[]
}

export const DelegateList: React.FC<DelegateListProps> = ({ delegates }) => {
  // Track which contact is being confirmed for removal
  const [contactToRemove, setContactToRemove] = useState<AuthorizedContactDto | null>(null)

  // Track the scene name for notification after removal completes
  const removedSceneNameRef = useRef<string>('')

  const removeContactMutation = useRemoveAuthorizedContact()

  // Handle mutation success - show notification and close modal
  useEffect(() => {
    if (removeContactMutation.isSuccess) {
      const sceneName = removedSceneNameRef.current
      notifications.show({
        title: 'Contact Removed',
        message: `${sceneName} removed`,
        color: 'green',
        icon: <IconCheck />,
      })
      setContactToRemove(null)
    }
  }, [removeContactMutation.isSuccess])

  // Handle mutation error - show notification and close modal
  useEffect(() => {
    if (removeContactMutation.isError) {
      const errorMessage =
        removeContactMutation.error instanceof Error
          ? removeContactMutation.error.message
          : 'Failed to remove contact'
      notifications.show({
        title: 'Error',
        message: errorMessage,
        color: 'red',
        icon: <IconAlertCircle />,
      })
      setContactToRemove(null)
    }
  }, [removeContactMutation.isError, removeContactMutation.error])

  /**
   * Handle confirmed removal of a delegate contact.
   * Stores scene name for post-removal notification and calls mutation.
   */
  const handleConfirmRemove = () => {
    if (!contactToRemove) return

    removedSceneNameRef.current = contactToRemove.sceneName ?? ''
    removeContactMutation.mutate(contactToRemove.id ?? '')
  }

  // Empty state
  if (delegates.length === 0) {
    return (
      <Text size="sm" c="dimmed" fs="italic">
        You haven&apos;t authorized anyone yet.
      </Text>
    )
  }

  return (
    <>
      <Stack gap="xs">
        {delegates.map((delegate) => (
          <Paper
            key={delegate.id}
            p="sm"
            withBorder
            style={{
              borderColor: 'var(--color-taupe)',
              transition: 'background-color 0.15s ease',
            }}
            styles={{
              root: {
                '&:hover': {
                  backgroundColor: 'var(--color-ivory)',
                },
              },
            }}
            data-testid={`delegate-row-${delegate.id}`}
          >
            <Group justify="space-between" align="center">
              <Text fw={500}>{delegate.sceneName}</Text>
              <Button
                variant="subtle"
                color="red"
                size="compact-sm"
                onClick={() => setContactToRemove(delegate)}
                data-testid={`remove-delegate-${delegate.id}`}
              >
                Remove
              </Button>
            </Group>
          </Paper>
        ))}
      </Stack>

      {/* Confirmation modal for removing a delegate */}
      <Modal
        opened={contactToRemove !== null}
        onClose={() => setContactToRemove(null)}
        title={
          <Text fw={600} style={{ fontFamily: 'var(--font-heading)', color: 'var(--color-burgundy)' }}>
            Remove {contactToRemove?.sceneName}?
          </Text>
        }
        centered
        size="sm"
      >
        <Stack gap="md">
          <Text size="sm">
            They will no longer be able to purchase tickets or RSVP on your behalf.
            This does not affect any tickets already purchased.
          </Text>

          <Group justify="flex-end" gap="sm">
            <Button
              variant="default"
              onClick={() => setContactToRemove(null)}
              disabled={removeContactMutation.isPending}
            >
              Cancel
            </Button>
            <Button
              color="red"
              onClick={handleConfirmRemove}
              loading={removeContactMutation.isPending}
              styles={{
                root: {
                  borderRadius: '12px 6px 12px 6px',
                  fontFamily: 'var(--font-heading)',
                  fontWeight: 600,
                  textTransform: 'uppercase',
                  height: 'auto',
                  paddingTop: '10px',
                  paddingBottom: '10px',
                  paddingLeft: '20px',
                  paddingRight: '20px',
                  lineHeight: '1.2',
                  display: 'flex',
                  alignItems: 'center',
                },
              }}
            >
              Remove
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  )
}
