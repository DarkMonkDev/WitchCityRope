/**
 * PendingTicketsCard
 *
 * Dashboard section showing pending tickets and RSVPs awaiting acceptance.
 * Appears at the top of MyEventsPage, before the events list.
 * Only visible when there are pending assignments.
 *
 * Design reference: ui-design.md Screen 4 - Dashboard Pending Tickets/RSVPs
 *
 * Features:
 * - Attention-drawing style with brass left border
 * - Count badge showing number of pending items
 * - Each item shows event name, date, sessions, type, purchaser
 * - Inline waiver checkbox (no modal) following ParticipationCard pattern
 * - Accept button disabled until waiver checkbox is checked
 * - Decline opens TicketDeclineModal
 * - Vetting error handling shown inline via Alert
 * - Responsive: stacked cards on mobile, full-width on desktop
 */

import React, { useState, useEffect } from 'react'
import {
  Paper,
  Stack,
  Group,
  Title,
  Badge,
  Text,
  Button,
  Loader,
  Center,
  Checkbox,
  Alert,
  Box,
} from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { IconAlertCircle } from '@tabler/icons-react'
import { useMediaQuery } from '@mantine/hooks'
import { usePendingAssignments } from '../api/queries'
import { useAcceptAssignment } from '../api/mutations'
import { TicketDeclineModal } from './TicketDeclineModal'
import type { PendingAssignmentDto } from '../types/ticketAssignment.types'
import { useEventTimeZone } from '../../../hooks/useEventTimeZone'
import { formatUtcToLocalDate, formatUtcToLocalTime } from '../../../utils/eventUtils'

export const PendingTicketsCard: React.FC = () => {
  const isMobile = useMediaQuery('(max-width: 991px)')
  const eventTimeZone = useEventTimeZone()

  const { data: pendingAssignments, isLoading } = usePendingAssignments()

  // Decline modal state (accept no longer uses a modal)
  const [declineModalOpen, setDeclineModalOpen] = useState(false)
  const [selectedAssignment, setSelectedAssignment] = useState<PendingAssignmentDto | null>(null)

  // Don't render if loading or no pending items
  if (isLoading) {
    return (
      <Center py="md">
        <Loader size="sm" color="burgundy" />
      </Center>
    )
  }

  if (!pendingAssignments || pendingAssignments.length === 0) {
    return null
  }

  const handleDeclineClick = (assignment: PendingAssignmentDto) => {
    setSelectedAssignment(assignment)
    setDeclineModalOpen(true)
  }

  return (
    <>
      <Paper
        p="lg"
        radius="md"
        mb="lg"
        style={{
          borderLeft: '4px solid var(--color-brass, #B8860B)',
          backgroundColor: 'var(--color-ivory, #FAF6F2)',
        }}
        data-testid="pending-tickets-card"
      >
        {/* Section Header */}
        <Group justify="space-between" mb="md">
          <Title
            order={3}
            style={{
              fontFamily: 'var(--font-heading)',
              color: 'var(--color-burgundy)',
            }}
          >
            Pending Tickets & RSVPs
          </Title>
          <Badge color="yellow" variant="filled" size="lg">
            {pendingAssignments.length}
          </Badge>
        </Group>

        {/* Pending Items Stack */}
        <Stack gap="md">
          {pendingAssignments.map((assignment) => (
            <PendingAssignmentItem
              key={assignment.attendanceId}
              assignment={assignment}
              isMobile={isMobile || false}
              eventTimeZone={eventTimeZone}
              onDecline={() => handleDeclineClick(assignment)}
            />
          ))}
        </Stack>
      </Paper>

      {/* Decline Modal */}
      <TicketDeclineModal
        opened={declineModalOpen}
        onClose={() => {
          setDeclineModalOpen(false)
          setSelectedAssignment(null)
        }}
        assignment={selectedAssignment}
      />
    </>
  )
}

PendingTicketsCard.displayName = 'PendingTicketsCard'

// ---------------------------------------------------------------------------
// Sub-component: Individual pending assignment item
// ---------------------------------------------------------------------------

interface PendingAssignmentItemProps {
  assignment: PendingAssignmentDto
  isMobile: boolean
  eventTimeZone: string
  onDecline: () => void
}

const PendingAssignmentItem: React.FC<PendingAssignmentItemProps> = ({
  assignment,
  isMobile,
  eventTimeZone,
  onDecline,
}) => {
  const isTicket = assignment.attendanceType === 'Ticket'

  // Local waiver checkbox state per card
  const [waiverAccepted, setWaiverAccepted] = useState(false)
  const [vettingError, setVettingError] = useState<string | null>(null)

  // Accept mutation - each card manages its own
  const acceptMutation = useAcceptAssignment(
    assignment.eventId || '',
    assignment.attendanceId || ''
  )

  // Handle mutation success (project TanStack Query v5 pattern)
  useEffect(() => {
    if (acceptMutation.isSuccess) {
      notifications.show({
        title: 'Success',
        message: `${isTicket ? 'Ticket' : 'RSVP'} accepted! You're registered for ${assignment.eventTitle}`,
        color: 'green',
      })
    }
  }, [acceptMutation.isSuccess])

  // Handle mutation error (project TanStack Query v5 pattern)
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
    if (!waiverAccepted || vettingError) return

    acceptMutation.mutate({
      eventWaiverAccepted: true,
      termsOfServiceAccepted: true,
    })
  }

  // Unique checkbox ID for label association
  const checkboxId = `waiver-checkbox-${assignment.attendanceId}`

  return (
    <Paper
      p="md"
      radius="md"
      withBorder
      style={{
        backgroundColor: 'white',
        borderColor: 'rgba(0, 0, 0, 0.08)',
      }}
      data-testid="pending-assignment-item"
    >
      <Stack gap="xs">
        {/* Event Title */}
        <Text fw={700} size="md">
          {assignment.eventTitle}
        </Text>

        {/* Date and Time */}
        <Text size="sm" c="dimmed">
          {formatUtcToLocalDate(assignment.eventDate ?? '', eventTimeZone, {
            weekday: 'short',
            month: 'short',
            day: 'numeric',
            year: 'numeric',
          })}
          {' at '}
          {formatUtcToLocalTime(assignment.eventDate ?? '', eventTimeZone)}
        </Text>

        {/* Sessions (if multi-session) */}
        {(assignment.sessionNames ?? []).length > 0 && (
          <Text size="sm" c="dimmed">
            {(assignment.sessionNames ?? []).join(', ')}
          </Text>
        )}

        {/* Ticket type and purchaser info */}
        {isMobile ? (
          // Mobile: stacked layout
          <Stack gap={4}>
            <Text size="sm" c="dimmed">
              {isTicket ? `${assignment.ticketTypeName} ticket` : 'RSVP'}
            </Text>
            <Text size="sm" fw={500}>
              {isTicket ? 'Purchased' : 'Created'} by {assignment.assignedBySceneName}
            </Text>
          </Stack>
        ) : (
          // Desktop: inline
          <Text size="sm">
            {isTicket
              ? `${assignment.ticketTypeName} ticket -- `
              : 'RSVP -- '}
            <Text component="span" fw={500}>
              {isTicket ? 'Purchased' : 'Created'} by {assignment.assignedBySceneName}
            </Text>
          </Text>
        )}

        {/* Vetting Error Alert (AD-014) */}
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

        {/* Inline Waiver Checkbox - following ParticipationCard pattern */}
        {!vettingError && (
          <Box mt="xs">
            <Group gap="sm" align="center">
              <Checkbox
                id={checkboxId}
                checked={waiverAccepted}
                onChange={(event) => setWaiverAccepted(event.currentTarget.checked)}
                size="md"
                color="var(--color-burgundy)"
                data-testid="waiver-checkbox"
              />
              <Text
                component="label"
                htmlFor={checkboxId}
                size="sm"
                style={{
                  cursor: 'pointer',
                  color: '#000000',
                  fontWeight: 700,
                  lineHeight: 1.5,
                }}
              >
                I agree to the{' '}
                <a
                  href="/event-waiver"
                  target="_blank"
                  rel="noopener noreferrer"
                  style={{
                    color: 'var(--color-burgundy)',
                    textDecoration: 'underline',
                    fontWeight: 700,
                  }}
                  onClick={(e) => e.stopPropagation()}
                >
                  Event Waiver
                </a>
                {' '}and{' '}
                <a
                  href="/terms-of-service"
                  target="_blank"
                  rel="noopener noreferrer"
                  style={{
                    color: 'var(--color-burgundy)',
                    textDecoration: 'underline',
                    fontWeight: 700,
                  }}
                  onClick={(e) => e.stopPropagation()}
                >
                  Terms of Service
                </a>
              </Text>
            </Group>
          </Box>
        )}

        {/* Action Buttons */}
        <Group justify="flex-end" gap="sm" mt="xs">
          <Button
            variant="subtle"
            color="gray"
            size={isMobile ? 'sm' : 'md'}
            onClick={onDecline}
            disabled={acceptMutation.isPending}
            data-testid="decline-button"
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 16px',
              lineHeight: 1.4,
            }}
          >
            Decline
          </Button>
          {!vettingError && (
            <Button
              color="burgundy"
              size={isMobile ? 'sm' : 'md'}
              onClick={handleAccept}
              loading={acceptMutation.isPending}
              disabled={!waiverAccepted || acceptMutation.isPending}
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
              {isTicket ? 'Accept Ticket' : 'Accept RSVP'}
            </Button>
          )}
        </Group>
      </Stack>
    </Paper>
  )
}
