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
 * - Accept and Decline action buttons
 * - Accept opens TicketAcceptanceModal
 * - Decline opens TicketDeclineModal
 * - Responsive: stacked cards on mobile, full-width on desktop
 */

import React, { useState } from 'react'
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
} from '@mantine/core'
import { useMediaQuery } from '@mantine/hooks'
import { usePendingAssignments } from '../api/queries'
import { TicketAcceptanceModal } from './TicketAcceptanceModal'
import { TicketDeclineModal } from './TicketDeclineModal'
import type { PendingAssignmentDto } from '../types/ticketAssignment.types'
import { useEventTimeZone } from '../../../hooks/useEventTimeZone'
import { formatUtcToLocalDate, formatUtcToLocalTime } from '../../../utils/eventUtils'

export const PendingTicketsCard: React.FC = () => {
  const isMobile = useMediaQuery('(max-width: 991px)')
  const eventTimeZone = useEventTimeZone()

  const { data: pendingAssignments, isLoading } = usePendingAssignments()

  // Modal state
  const [acceptModalOpen, setAcceptModalOpen] = useState(false)
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

  const handleAcceptClick = (assignment: PendingAssignmentDto) => {
    setSelectedAssignment(assignment)
    setAcceptModalOpen(true)
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
              onAccept={() => handleAcceptClick(assignment)}
              onDecline={() => handleDeclineClick(assignment)}
            />
          ))}
        </Stack>
      </Paper>

      {/* Acceptance Modal */}
      <TicketAcceptanceModal
        opened={acceptModalOpen}
        onClose={() => {
          setAcceptModalOpen(false)
          setSelectedAssignment(null)
        }}
        assignment={selectedAssignment}
      />

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
  onAccept: () => void
  onDecline: () => void
}

const PendingAssignmentItem: React.FC<PendingAssignmentItemProps> = ({
  assignment,
  isMobile,
  eventTimeZone,
  onAccept,
  onDecline,
}) => {
  const isTicket = assignment.attendanceType === 'Ticket'

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
          {formatUtcToLocalDate(assignment.eventDate, eventTimeZone, {
            weekday: 'short',
            month: 'short',
            day: 'numeric',
            year: 'numeric',
          })}
          {' at '}
          {formatUtcToLocalTime(assignment.eventDate, eventTimeZone)}
        </Text>

        {/* Sessions (if multi-session) */}
        {assignment.sessionNames.length > 0 && (
          <Text size="sm" c="dimmed">
            {assignment.sessionNames.join(', ')}
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

        {/* Action Buttons */}
        <Group justify="flex-end" gap="sm" mt="xs">
          <Button
            variant="subtle"
            color="gray"
            size={isMobile ? 'sm' : 'md'}
            onClick={onDecline}
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
          <Button
            color="burgundy"
            size={isMobile ? 'sm' : 'md'}
            onClick={onAccept}
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
        </Group>
      </Stack>
    </Paper>
  )
}
