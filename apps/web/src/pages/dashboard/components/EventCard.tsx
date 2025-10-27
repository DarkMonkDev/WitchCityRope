import React from 'react'
import { Card, Text, Badge, Box, Stack, Group, Button } from '@mantine/core'
import { useNavigate } from 'react-router-dom'
import { IconHeart, IconShoppingCart } from '@tabler/icons-react'
import type { UserEventDto } from '../../../types/dashboard.types'
import type { VolunteerShiftWithEvent } from '../../../components/dashboard/UserVolunteerShifts'
import { useEvent } from '../../../lib/api/hooks/useEvents'

interface EventCardProps {
  event: UserEventDto
  className?: string
  volunteerShifts?: VolunteerShiftWithEvent[]
}

/**
 * Event card for user dashboard
 *
 * CRITICAL DIFFERENCES from Public Event Card:
 * - NO pricing information
 * - NO capacity/availability
 * - NO "Learn More" button
 * - ENTIRE CARD is clickable (no separate button)
 * - Shows registration status badge
 * - Shows volunteer shift information if user is volunteering
 */
export const EventCard: React.FC<EventCardProps> = ({ event, className, volunteerShifts = [] }) => {
  const navigate = useNavigate()

  // Find volunteer shifts for this specific event
  const eventVolunteerShifts = volunteerShifts.filter(shift =>
    // Match by eventId if available, otherwise try to match by event title as fallback
    shift.eventId === event.id || shift.eventTitle === event.title
  )
  const statusColors: Record<string, string> = {
    'RSVP Confirmed': 'blue',
    'Ticket Purchased': 'green',
    Attended: 'grape',
  }

  // Check if we should show Purchase Ticket button:
  // - User has RSVP but no ticket
  // - Event is a social event
  const shouldCheckForPaidTickets = !event.hasTicket &&
                                     event.registrationStatus === 'RSVP Confirmed' &&
                                     event.isSocialEvent

  // Fetch full event details only when we need to check for paid tickets
  const { data: fullEvent } = useEvent(event.id, shouldCheckForPaidTickets)

  // Check if event has paid tickets (maxPrice > 0)
  const hasPaidTickets = fullEvent?.ticketTypes?.some((tt: any) => (tt.maxPrice || 0) > 0) || false

  // Show purchase button if all conditions are met
  const showPurchaseButton = shouldCheckForPaidTickets && hasPaidTickets

  const formatEventDateTime = (startDate: string, endDate?: string) => {
    const start = new Date(startDate)

    // Format date with abbreviated month, no year
    const datePart = start.toLocaleDateString('en-US', {
      weekday: 'long',
      month: 'short',
      day: 'numeric'
    })

    // Format start time
    const startTime = start.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    }).toLowerCase()

    // If no end date, just return date + start time
    if (!endDate) {
      return `${datePart} - ${startTime}`
    }

    // Format end time
    const end = new Date(endDate)
    const endTime = end.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    }).toLowerCase()

    return `${datePart} - ${startTime} - ${endTime}`
  }

  const formatShiftTime = (timeString?: string) => {
    if (!timeString) return ''
    try {
      const date = new Date(timeString)
      return date.toLocaleTimeString('en-US', {
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
      }).toLowerCase()
    } catch {
      return timeString
    }
  }

  // Handle card click with setTimeout pattern (see react-developer-lessons-learned.md)
  const handleCardClick = () => {
    setTimeout(() => {
      navigate(`/events/${event.id}`)
    }, 0)
  }

  // Handle purchase ticket button click
  const handlePurchaseClick = (e: React.MouseEvent) => {
    e.stopPropagation() // Prevent card click
    setTimeout(() => {
      navigate(`/checkout/${event.id}`)
    }, 0)
  }

  return (
    <Card
      shadow="sm"
      padding="0"
      radius="md"
      withBorder
      className={className}
      data-testid="event-card"
      onClick={handleCardClick}
      style={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        background: 'var(--color-ivory)',
        borderColor: 'rgba(183, 109, 117, 0.1)',
        transition: 'all 0.3s ease',
        cursor: 'pointer',
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = 'translateY(-4px)'
        e.currentTarget.style.boxShadow = '0 8px 24px rgba(136, 1, 36, 0.15)'
        e.currentTarget.style.borderColor = 'var(--color-burgundy)'
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = 'translateY(0)'
        e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.05)'
        e.currentTarget.style.borderColor = 'rgba(183, 109, 117, 0.1)'
      }}
    >
      {/* Gradient Header */}
      <Box
        h={100}
        style={{
          background: 'linear-gradient(135deg, var(--color-plum) 0%, var(--color-burgundy) 100%)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: 'var(--space-md)',
        }}
      >
        <Text
          c="white"
          fw={700}
          size="lg"
          ta="center"
          px="md"
          data-testid="event-title"
          style={{
            fontFamily: 'var(--font-heading)',
            lineHeight: 1.3,
            textShadow: '0 2px 4px rgba(0, 0, 0, 0.3)',
          }}
        >
          {event.title}
        </Text>
      </Box>

      <Stack gap="sm" p="lg" style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {/* Date/Time - Split Layout */}
        <Group
          justify="space-between"
          style={{
            marginBottom: '4px',
          }}
        >
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
            {(() => {
              if (!event.startDate) return 'TBD'
              const start = new Date(event.startDate)
              return start.toLocaleDateString('en-US', {
                weekday: 'long',
                month: 'short',
                day: 'numeric'
              })
            })()}
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
            {(() => {
              if (!event.startDate) return ''
              const start = new Date(event.startDate)
              const startTime = start.toLocaleTimeString('en-US', {
                hour: 'numeric',
                minute: '2-digit',
                hour12: true
              }).toLowerCase()

              if (!event.endDate) return startTime

              const end = new Date(event.endDate)
              const endTime = end.toLocaleTimeString('en-US', {
                hour: 'numeric',
                minute: '2-digit',
                hour12: true
              }).toLowerCase()

              return `${startTime} - ${endTime}`
            })()}
          </Text>
        </Group>

        {/* Location */}
        <Text size="sm" c="dimmed">
          📍 {event.location}
        </Text>

        {/* Description */}
        {event.description && (
          <Text size="sm" c="dimmed">
            {event.description}
          </Text>
        )}

        {/* Volunteer Shift Section - Show if user is volunteering */}
        {eventVolunteerShifts.length > 0 && (
          <Box
            style={{
              background: 'linear-gradient(135deg, rgba(155, 74, 117, 0.1) 0%, rgba(136, 1, 36, 0.1) 100%)',
              borderRadius: '8px',
              padding: 'var(--space-xs)',
              border: '1px solid rgba(155, 74, 117, 0.2)',
            }}
          >
            <Group gap="xs" mb={4}>
              <IconHeart size={16} color="var(--color-plum)" />
              <Text fw={600} size="sm" c="var(--color-plum)">
                Volunteering
              </Text>
            </Group>
            <Stack gap={4}>
              {eventVolunteerShifts.map((shift) => {
                // Only show session name if event has multiple sessions (multi-day events)
                const showSessionName = shift.sessionName && !shift.sessionName.includes('Main Session');

                return (
                  <Group key={shift.id} justify="space-between">
                    <Text size="sm" fw={500} c="var(--color-charcoal)">
                      {shift.positionTitle}
                      {showSessionName && ` • ${shift.sessionName}`}
                    </Text>
                    {shift.sessionStartTime && shift.sessionEndTime && (
                      <Text size="sm" fw={500} c="var(--color-charcoal)">
                        {formatShiftTime(shift.sessionStartTime)} - {formatShiftTime(shift.sessionEndTime)}
                      </Text>
                    )}
                  </Group>
                );
              })}
            </Stack>
          </Box>
        )}

        {/* Status Badges - Can show multiple badges when user has both ticket and RSVP */}
        <Box style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
          {/* Show Ticket Purchased badge if user has a ticket */}
          {event.hasTicket && event.registrationStatus !== 'Attended' && (
            <Badge color="green" variant="light">
              Ticket Purchased
            </Badge>
          )}

          {/*
            Show RSVP badge if registration status contains "(Social Event)"
            This indicates user has both paid AND RSVPed to a social event
            Backend sets this when: hasTicket=true AND isSocialEvent=true AND user has RSVP
          */}
          {event.registrationStatus.includes('(Social Event)') && (
            <Badge color="blue" variant="light">
              RSVP
            </Badge>
          )}

          {/* Show RSVP Confirmed badge if user only has RSVP (no ticket) */}
          {!event.hasTicket && event.registrationStatus === 'RSVP Confirmed' && (
            <Badge color="blue" variant="light">
              RSVP Confirmed
            </Badge>
          )}

          {/* Show Attended badge for past events */}
          {event.registrationStatus === 'Attended' && (
            <Badge color="grape" variant="light">
              Attended
            </Badge>
          )}
        </Box>

        {/* Purchase Ticket Button - Show for social events with RSVP but no ticket purchased */}
        {showPurchaseButton && (
          <Button
            onClick={handlePurchaseClick}
            leftSection={<IconShoppingCart size={16} />}
            color="burgundy"
            fullWidth
            style={{
              marginTop: '8px',
              fontFamily: 'var(--font-heading)',
              fontWeight: 700,
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
            }}
          >
            Purchase Ticket
          </Button>
        )}
      </Stack>
    </Card>
  )
}
