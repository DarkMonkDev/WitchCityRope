/**
 * TicketAcceptanceModal
 *
 * Modal for accepting an assigned ticket or proxy RSVP.
 * Displays event details, waiver text, and required checkboxes.
 * The Accept button is disabled until all required checkboxes are checked.
 *
 * Design reference: ui-design.md Screen 6 - Ticket/RSVP Acceptance Modal
 *
 * Features:
 * - Shows event details (name, date, sessions, ticket type, purchaser)
 * - Scrollable waiver text area (max-height 200px)
 * - Required "I accept the Event Waiver" checkbox
 * - Conditional "I accept the Terms of Service" checkbox (only if user hasn't accepted ToS)
 * - Vetting error handling (AD-014 edge case)
 * - Loading state during submission
 */

import React, { useState, useEffect } from 'react'
import {
  Modal,
  Stack,
  Text,
  Button,
  Group,
  Title,
  Divider,
  Checkbox,
  ScrollArea,
  Alert,
} from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { IconAlertCircle } from '@tabler/icons-react'
import { useAcceptAssignment } from '../api/mutations'
import { useUser } from '../../../stores/authStore'
import type { PendingAssignmentDto } from '../types/ticketAssignment.types'
import { useEventTimeZone } from '../../../hooks/useEventTimeZone'
import { formatUtcToLocalDate, formatUtcToLocalTime } from '../../../utils/eventUtils'

interface TicketAcceptanceModalProps {
  /** Whether the modal is open */
  opened: boolean
  /** Callback to close the modal */
  onClose: () => void
  /** The pending assignment to accept */
  assignment: PendingAssignmentDto | null
  /** Optional event waiver text to display */
  eventWaiverText?: string
}

export const TicketAcceptanceModal: React.FC<TicketAcceptanceModalProps> = ({
  opened,
  onClose,
  assignment,
  eventWaiverText,
}) => {
  const user = useUser()
  const eventTimeZone = useEventTimeZone()

  const [waiverAccepted, setWaiverAccepted] = useState(false)
  const [tosAccepted, setTosAccepted] = useState(false)
  const [vettingError, setVettingError] = useState<string | null>(null)

  // Determine if ToS checkbox needs to be shown
  // Only show if user hasn't already accepted platform Terms of Service
  const userDto = user as any
  const needsToS = !userDto?.termsOfServiceAccepted

  // Create mutation hook - use empty strings when no assignment is selected
  const acceptMutation = useAcceptAssignment(
    assignment?.eventId || '',
    assignment?.attendanceId || ''
  )

  // Reset state when modal opens/closes
  useEffect(() => {
    if (!opened) {
      setWaiverAccepted(false)
      setTosAccepted(false)
      setVettingError(null)
    }
  }, [opened])

  // Determine if Accept button should be enabled
  const canAccept = waiverAccepted && (!needsToS || tosAccepted) && !vettingError

  const isTicket = assignment?.attendanceType === 'Ticket'
  const acceptLabel = isTicket ? 'Accept Ticket' : 'Accept RSVP'
  const modalTitle = isTicket ? 'Accept Your Ticket' : 'Accept Your RSVP'

  // Handle mutation success/error via useEffect (project pattern for TanStack Query v5)
  useEffect(() => {
    if (acceptMutation.isSuccess && assignment) {
      notifications.show({
        title: 'Success',
        message: `${isTicket ? 'Ticket' : 'RSVP'} accepted! You're registered for ${assignment.eventTitle}`,
        color: 'green',
      })
      onClose()
    }
  }, [acceptMutation.isSuccess])

  useEffect(() => {
    if (acceptMutation.isError) {
      const message = acceptMutation.error?.message || ''

      // Check for vetting-related errors (AD-014 edge case)
      if (
        message.toLowerCase().includes('vetting') ||
        message.toLowerCase().includes('vetted')
      ) {
        setVettingError(
          'This event requires vetted membership. Your vetting status has changed since this ticket was assigned. Please contact an admin for assistance.'
        )
        return
      }

      // Check for event-passed errors
      if (
        message.toLowerCase().includes('started') ||
        message.toLowerCase().includes('passed')
      ) {
        setVettingError(
          'This event has already started. Contact an admin for assistance.'
        )
        return
      }

      // Generic error - show notification
      notifications.show({
        title: 'Error',
        message: message || 'Failed to accept. Please try again.',
        color: 'red',
      })
    }
  }, [acceptMutation.isError])

  const handleAccept = () => {
    if (!assignment || !canAccept) return

    acceptMutation.mutate({
      eventWaiverAccepted: true,
      termsOfServiceAccepted: tosAccepted || false,
    })
  }

  const handleClose = () => {
    if (!acceptMutation.isPending) {
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
      size="lg"
      data-testid="ticket-acceptance-modal"
    >
      <Stack gap="md">
        {/* Event Details */}
        <Stack gap="xs">
          <Text size="sm">
            Event: <Text component="span" fw={600}>{assignment.eventTitle}</Text>
          </Text>
          <Text size="sm">
            Date:{' '}
            <Text component="span" fw={600}>
              {formatUtcToLocalDate(assignment.eventDate ?? '', eventTimeZone, {
                weekday: 'long',
                month: 'long',
                day: 'numeric',
                year: 'numeric',
              })}
              {' at '}
              {formatUtcToLocalTime(assignment.eventDate ?? '', eventTimeZone)}
            </Text>
          </Text>
          {(assignment.sessionNames ?? []).length > 0 && (
            <Text size="sm">
              Sessions:{' '}
              <Text component="span" fw={600}>
                {(assignment.sessionNames ?? []).join(', ')}
              </Text>
            </Text>
          )}
          {isTicket && (
            <Text size="sm">
              Ticket type:{' '}
              <Text component="span" fw={600}>{assignment.ticketTypeName}</Text>
            </Text>
          )}
          <Text size="sm">
            {isTicket ? 'Purchased' : 'Created'} by:{' '}
            <Text component="span" fw={600}>{assignment.assignedBySceneName}</Text>
          </Text>
        </Stack>

        <Divider my="xs" />

        {/* Vetting Error State (AD-014) */}
        {vettingError && (
          <Alert
            icon={<IconAlertCircle size={16} />}
            color="red"
            title="Unable to Accept"
            variant="light"
            data-testid="vetting-error-alert"
          >
            <Text size="sm">{vettingError}</Text>
          </Alert>
        )}

        {/* Waiver and Checkboxes - hidden if vetting error */}
        {!vettingError && (
          <>
            {/* Event Waiver */}
            <Text fw={600} size="sm">
              Event Waiver
            </Text>
            <ScrollArea h={200} type="auto" offsetScrollbars>
              <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                {eventWaiverText ||
                  'By accepting this ticket/RSVP, you agree to follow all event rules and safety guidelines. You acknowledge the inherent risks associated with participation and agree to conduct yourself responsibly throughout the event.'}
              </Text>
            </ScrollArea>

            <Checkbox
              checked={waiverAccepted}
              onChange={(event) => setWaiverAccepted(event.currentTarget.checked)}
              label="I have read and accept the Event Waiver"
              color="burgundy"
              data-testid="waiver-checkbox"
              styles={{
                label: {
                  fontSize: '14px',
                  fontWeight: 500,
                  color: '#2B2B2B',
                },
              }}
            />

            {needsToS && (
              <Checkbox
                checked={tosAccepted}
                onChange={(event) => setTosAccepted(event.currentTarget.checked)}
                label="I accept the Terms of Service"
                color="burgundy"
                data-testid="tos-checkbox"
                styles={{
                  label: {
                    fontSize: '14px',
                    fontWeight: 500,
                    color: '#2B2B2B',
                  },
                }}
              />
            )}
          </>
        )}

        <Divider my="xs" />

        {/* Action Buttons */}
        <Group justify="flex-end" gap="sm" mt="sm">
          <Button
            variant="subtle"
            color="gray"
            onClick={handleClose}
            disabled={acceptMutation.isPending}
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4,
            }}
          >
            {vettingError ? 'Close' : 'Cancel'}
          </Button>
          {!vettingError && (
            <Button
              color="burgundy"
              onClick={handleAccept}
              loading={acceptMutation.isPending}
              disabled={!canAccept}
              data-testid="accept-button"
              styles={{
                root: {
                  borderRadius: '12px 6px 12px 6px',
                  fontFamily: 'var(--font-heading)',
                  fontWeight: 600,
                  textTransform: 'uppercase',
                  letterSpacing: '1px',
                  transition: 'all 0.3s ease',
                  height: 'auto',
                  minHeight: 40,
                  paddingTop: '10px',
                  paddingBottom: '10px',
                  paddingLeft: '20px',
                  paddingRight: '20px',
                  lineHeight: '1.2',
                },
              }}
            >
              {acceptLabel}
            </Button>
          )}
        </Group>
      </Stack>
    </Modal>
  )
}

TicketAcceptanceModal.displayName = 'TicketAcceptanceModal'
