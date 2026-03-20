/**
 * PendingTicketsCard
 *
 * Dashboard section showing pending tickets and RSVPs awaiting acceptance.
 * Appears at the top of MyEventsPage, before the events list.
 * Only visible when there are pending assignments.
 *
 * Each pending item renders as a card matching the EventCard styling
 * (gradient header, same fonts/layout) so the dashboard has a consistent look.
 *
 * Features:
 * - Section title with count badge inline
 * - Individual event-styled cards for each pending item
 * - Gradient header with event title (matching EventCard)
 * - Session date/time in burgundy uppercase (matching EventCard)
 * - Ticket/RSVP type and purchaser info
 * - Inline waiver checkbox (no modal)
 * - Accept button disabled until waiver is checked
 * - Decline uses secondary button style
 * - Vetting error handling shown inline via Alert
 */

import React, { useState, useEffect } from 'react'
import {
  Card,
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
  SimpleGrid,
} from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { IconAlertCircle, IconTicket } from '@tabler/icons-react'
import { useMediaQuery } from '@mantine/hooks'
import { usePendingAssignments } from '../api/queries'
import { useAcceptAssignment } from '../api/mutations'
import { TicketDeclineModal } from './TicketDeclineModal'
import type { PendingAssignmentDto } from '../types/ticketAssignment.types'
import { useEventTimeZone } from '../../../hooks/useEventTimeZone'
import { formatUtcToLocalDate, formatUtcTimeRange } from '../../../utils/eventUtils'

export const PendingTicketsCard: React.FC = () => {
  const isMobile = useMediaQuery('(max-width: 991px)')
  const eventTimeZone = useEventTimeZone()

  const { data: pendingAssignments, isLoading } = usePendingAssignments()

  // Decline modal state
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
      <Box mb="lg" data-testid="pending-tickets-card">
        {/* Section Header — title + count badge inline */}
        <Group gap="sm" align="center" mb="md">
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

        {/* Pending Items — individual event-styled cards */}
        <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="lg">
          {pendingAssignments.map((assignment) => (
            <PendingAssignmentCard
              key={assignment.attendanceId}
              assignment={assignment}
              isMobile={isMobile || false}
              eventTimeZone={eventTimeZone}
              onDecline={() => handleDeclineClick(assignment)}
            />
          ))}
        </SimpleGrid>
      </Box>

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
// Sub-component: Individual pending assignment card (matches EventCard styling)
// ---------------------------------------------------------------------------

interface PendingAssignmentCardProps {
  assignment: PendingAssignmentDto
  isMobile: boolean
  eventTimeZone: string
  onDecline: () => void
}

const PendingAssignmentCard: React.FC<PendingAssignmentCardProps> = ({
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

  // Handle mutation success
  useEffect(() => {
    if (acceptMutation.isSuccess) {
      notifications.show({
        title: 'Success',
        message: `${isTicket ? 'Ticket' : 'RSVP'} accepted! You're registered for ${assignment.eventTitle}`,
        color: 'green',
      })
    }
  }, [acceptMutation.isSuccess])

  // Handle mutation error
  useEffect(() => {
    if (acceptMutation.isError) {
      const message = acceptMutation.error?.message || ''

      if (
        message.toLowerCase().includes('vetting') ||
        message.toLowerCase().includes('vetted')
      ) {
        setVettingError(
          'This event requires vetted membership. Your vetting status has changed since this ticket was assigned. Please contact an admin for assistance.'
        )
        return
      }

      if (
        message.toLowerCase().includes('started') ||
        message.toLowerCase().includes('passed')
      ) {
        setVettingError(
          'This event has already started. Contact an admin for assistance.'
        )
        return
      }

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

  return (
    <Card
      shadow={isMobile ? undefined : 'sm'}
      padding="0"
      radius={isMobile ? 0 : 'md'}
      withBorder={!isMobile}
      data-testid="pending-assignment-item"
      style={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        background: 'var(--color-ivory)',
        borderColor: isMobile ? 'transparent' : 'rgba(183, 109, 117, 0.1)',
        border: isMobile ? 0 : undefined,
      }}
    >
      {/* Gradient Header — matches EventCard */}
      <Box
        h={80}
        style={{
          background: 'linear-gradient(135deg, var(--color-plum) 0%, var(--color-burgundy) 100%)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '18px',
        }}
      >
        <Text
          c="white"
          fw={700}
          size="lg"
          ta="center"
          px="md"
          style={{
            fontFamily: 'var(--font-heading)',
            lineHeight: 1.3,
            textShadow: '0 2px 4px rgba(0, 0, 0, 0.3)',
          }}
        >
          {assignment.eventTitle}
        </Text>
      </Box>

      <Stack gap="sm" p="lg" style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {/* Date/Time — matching EventCard burgundy uppercase format */}
        {assignment.eventDate && (
          <Group justify="space-between">
            <Text
              fw={700}
              c="burgundy"
              size="sm"
              tt="uppercase"
              style={{
                fontFamily: 'var(--font-heading)',
                letterSpacing: '0.5px',
              }}
            >
              {formatUtcToLocalDate(assignment.eventDate, eventTimeZone, {
                weekday: 'short',
                month: 'short',
                day: 'numeric',
              })}
            </Text>
            <Text
              fw={700}
              c="burgundy"
              size="sm"
              tt="uppercase"
              style={{
                fontFamily: 'var(--font-heading)',
                letterSpacing: '0.5px',
              }}
            >
              {formatUtcTimeRange(assignment.eventDate, undefined, eventTimeZone)}
            </Text>
          </Group>
        )}

        {/* Sessions */}
        {(assignment.sessionNames ?? []).length > 0 && (
          <Stack gap={2}>
            {(assignment.sessionNames ?? []).map((sessionName, idx) => (
              <Text
                key={idx}
                fw={600}
                c="dimmed"
                size="sm"
                style={{ fontFamily: 'var(--font-heading)' }}
              >
                {sessionName}
              </Text>
            ))}
          </Stack>
        )}

        {/* Ticket/RSVP info — matching EventCard green ticket box style */}
        <Box
          style={{
            background: 'linear-gradient(135deg, rgba(34, 139, 34, 0.08) 0%, rgba(46, 125, 50, 0.08) 100%)',
            borderRadius: '8px',
            padding: 'var(--space-xs)',
            border: '1px solid rgba(34, 139, 34, 0.2)',
          }}
        >
          <Group gap="xs" mb={4}>
            <IconTicket size={16} color="var(--mantine-color-green-7)" />
            <Text fw={600} size="sm" c="var(--mantine-color-green-7)">
              {isTicket ? `${assignment.ticketTypeName || 'Ticket'}` : 'RSVP'}
            </Text>
          </Group>
          <Text size="sm" fw={500}>
            {isTicket ? 'Purchased' : 'Created'} by {assignment.assignedBySceneName}
          </Text>
        </Box>

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

        {/* Inline Waiver Checkbox */}
        {!vettingError && (
          <Checkbox
            checked={waiverAccepted}
            onChange={(event) => setWaiverAccepted(event.currentTarget.checked)}
            size="md"
            color="var(--color-burgundy)"
            data-testid="waiver-checkbox"
            label={
              <Text
                size="sm"
                style={{ color: '#000000', fontWeight: 600, lineHeight: 1.4 }}
              >
                I agree to the{' '}
                <a
                  href="/event-waiver"
                  target="_blank"
                  rel="noopener noreferrer"
                  style={{ color: 'var(--color-burgundy)', textDecoration: 'underline', fontWeight: 700 }}
                  onClick={(e) => e.stopPropagation()}
                >
                  Event Waiver
                </a>
                {' '}and{' '}
                <a
                  href="/terms-of-service"
                  target="_blank"
                  rel="noopener noreferrer"
                  style={{ color: 'var(--color-burgundy)', textDecoration: 'underline', fontWeight: 700 }}
                  onClick={(e) => e.stopPropagation()}
                >
                  Terms of Service
                </a>
              </Text>
            }
          />
        )}

        {/* Action Buttons */}
        <Group justify="flex-end" gap="sm" mt="xs">
          <Button
            variant="outline"
            color="gray"
            size={isMobile ? 'sm' : 'md'}
            onClick={onDecline}
            disabled={acceptMutation.isPending}
            data-testid="decline-button"
            styles={{
              root: {
                minHeight: 40,
                height: 'auto',
                padding: '10px 16px',
                lineHeight: 1.4,
                borderColor: 'rgba(0, 0, 0, 0.2)',
                color: 'var(--color-charcoal, #2B2B2B)',
                transition: 'all 0.2s ease',
                '&:hover': {
                  backgroundColor: 'rgba(0, 0, 0, 0.04)',
                  borderColor: 'rgba(0, 0, 0, 0.4)',
                },
              },
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
    </Card>
  )
}
