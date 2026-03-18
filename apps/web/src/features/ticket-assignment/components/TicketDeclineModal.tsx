/**
 * TicketDeclineModal
 *
 * Confirmation modal for declining an assigned ticket or proxy RSVP.
 * Shows a warning message with the purchaser's scene name and an
 * optional reason text field.
 *
 * Design reference: ui-design.md Screen 4 - Decline interaction flow
 *
 * On decline, the ticket reverts to the purchaser who can reassign it.
 */

import React, { useState, useEffect } from 'react'
import {
  Modal,
  Stack,
  Text,
  Button,
  Group,
  Title,
  Textarea,
} from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { useDeclineAssignment } from '../api/mutations'
import type { PendingAssignmentDto } from '../types/ticketAssignment.types'

interface TicketDeclineModalProps {
  /** Whether the modal is open */
  opened: boolean
  /** Callback to close the modal */
  onClose: () => void
  /** The pending assignment to decline */
  assignment: PendingAssignmentDto | null
}

export const TicketDeclineModal: React.FC<TicketDeclineModalProps> = ({
  opened,
  onClose,
  assignment,
}) => {
  const [reason, setReason] = useState('')

  // Create mutation hook
  const declineMutation = useDeclineAssignment(
    assignment?.eventId || '',
    assignment?.attendanceId || ''
  )

  // Reset state when modal closes
  useEffect(() => {
    if (!opened) {
      setReason('')
    }
  }, [opened])

  const isTicket = assignment?.attendanceType === 'Ticket'
  const modalTitle = isTicket ? 'Decline this ticket?' : 'Decline this RSVP?'

  // Handle mutation success/error via useEffect (project pattern for TanStack Query v5)
  useEffect(() => {
    if (declineMutation.isSuccess && assignment) {
      notifications.show({
        title: isTicket ? 'Ticket Declined' : 'RSVP Declined',
        message: `The ${isTicket ? 'ticket' : 'RSVP'} has been returned to ${assignment.assignedBySceneName}.`,
        color: 'blue',
      })
      onClose()
    }
  }, [declineMutation.isSuccess])

  useEffect(() => {
    if (declineMutation.isError) {
      notifications.show({
        title: 'Error',
        message: declineMutation.error?.message || 'Failed to decline. Please try again.',
        color: 'red',
      })
    }
  }, [declineMutation.isError])

  const handleDecline = () => {
    if (!assignment) return

    const declineData = reason.trim() ? { reason: reason.trim() } : undefined

    declineMutation.mutate(declineData)
  }

  const handleClose = () => {
    if (!declineMutation.isPending) {
      onClose()
    }
  }

  if (!assignment) return null

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          {modalTitle}
        </Title>
      }
      centered
      size="md"
      data-testid="ticket-decline-modal"
    >
      <Stack gap="md">
        <Text size="sm">
          {isTicket
            ? `The ticket will be returned to ${assignment.assignedBySceneName} who can reassign it to someone else.`
            : `The RSVP will be cancelled. ${assignment.assignedBySceneName} can create a new proxy RSVP if needed.`}
        </Text>

        <Text size="sm" c="dimmed">
          Event: <Text component="span" fw={600} c="dark">{assignment.eventTitle}</Text>
        </Text>

        <Textarea
          label="Reason (optional)"
          placeholder="Let them know why you're declining..."
          value={reason}
          onChange={(event) => setReason(event.currentTarget.value)}
          maxLength={500}
          minRows={2}
          data-testid="decline-reason-textarea"
          styles={{
            label: {
              fontSize: '14px',
              fontWeight: 600,
              marginBottom: '8px',
            },
          }}
        />

        <Group justify="flex-end" gap="sm" mt="md">
          <Button
            variant="subtle"
            color="gray"
            onClick={handleClose}
            disabled={declineMutation.isPending}
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4,
            }}
          >
            Cancel
          </Button>
          <Button
            color="red"
            variant="light"
            onClick={handleDecline}
            loading={declineMutation.isPending}
            data-testid="decline-confirm-button"
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4,
              fontWeight: 600,
            }}
          >
            Decline
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

TicketDeclineModal.displayName = 'TicketDeclineModal'
