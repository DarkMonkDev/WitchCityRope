import React, { useState, useCallback, useEffect } from 'react'
import { Card, Text, Badge, Box, Stack, Group, Button } from '@mantine/core'
import { useMediaQuery } from '@mantine/hooks'
import { useNavigate } from 'react-router-dom'
import { IconHeart, IconTicket, IconUsers } from '@tabler/icons-react'
import type { UserEventDto } from '../../../types/dashboard.types'
import type { AssignedTicketStatusDto, AssignTicketRequest } from '../../../features/ticket-assignment/types/ticketAssignment.types'
import type { VolunteerShiftWithEvent } from '../../../components/dashboard/UserVolunteerShifts'
import { TicketStatusBadge } from '../../../features/ticket-assignment/components/TicketStatusBadge'
import { AssignTicketDropdown } from '../../../features/ticket-assignment/components/AssignTicketDropdown'
import { useAssignTicket, useReassignTicket } from '../../../features/ticket-assignment/api/mutations'
import { useEventTimeZone } from '../../../hooks/useEventTimeZone'
import { formatUtcTimeRange, formatUtcToLocalDate, formatUtcToLocalTime } from '../../../utils/eventUtils'

interface EventCardProps {
  event: UserEventDto
  className?: string
  volunteerShifts?: VolunteerShiftWithEvent[]
  /** Tickets the user purchased for this event (from useAssignedTickets) */
  assignedTickets?: AssignedTicketStatusDto[]
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
export const EventCard: React.FC<EventCardProps> = ({ event, className, volunteerShifts = [], assignedTickets = [] }) => {
  const navigate = useNavigate()
  const isMobile = useMediaQuery('(max-width: 991px)')
  const eventTimeZone = useEventTimeZone();

  // Find volunteer shifts for this specific event, sorted by earliest shift first
  const eventVolunteerShifts = volunteerShifts
    .filter(shift => shift.eventId === event.id)
    .sort((a, b) => {
      const aTime = a.sessionStartTime ? new Date(a.sessionStartTime).getTime() : Infinity;
      const bTime = b.sessionStartTime ? new Date(b.sessionStartTime).getTime() : Infinity;
      return aTime - bTime;
    })
  // Show "Purchase Ticket" button when:
  // - User has RSVP but no ticket purchased yet
  // - Event has ticket types available (paid or donation, from backend DTO)
  const showPurchaseButton = !event.hasTicket &&
                              event.registrationStatus === 'RSVP Confirmed' &&
                              (event as any).hasAvailableTickets === true

  const formatShiftTime = (timeString?: string) => {
    if (!timeString) return ''
    try {
      // Use TRUE UTC to local time conversion
      return formatUtcToLocalTime(timeString, eventTimeZone)
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
      shadow={isMobile ? undefined : "sm"}
      padding="0"
      radius={isMobile ? 0 : "md"}
      withBorder={!isMobile}
      className={className}
      data-testid="event-card"
      onClick={handleCardClick}
      style={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        background: 'var(--color-ivory)',
        borderColor: isMobile ? 'transparent' : 'rgba(183, 109, 117, 0.1)',
        border: isMobile ? 0 : undefined,
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
        h={80}
        style={{
          background: 'linear-gradient(135deg, var(--color-plum) 0%, var(--color-burgundy) 100%)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '18px',
          marginBottom: isMobile ? 0 : undefined,
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
        {/* Date/Time - Multi-session support */}
        {(() => {
          // Use eventSessions (ALL sessions for the event) so RSVP-only users still see dates/times.
          // registeredSessions only contains sessions with tickets, leaving RSVP users with no dates.
          const eventAny = event as any;
          const sessions = (eventAny.eventSessions || []).slice().sort((a: any, b: any) =>
            new Date(a.startTime || '').getTime() - new Date(b.startTime || '').getTime()
          );

          if (sessions.length === 0) {
            return (
              <Text
                fw={700}
                c="burgundy"
                size="sm"
                tt="uppercase"
                style={{
                  fontFamily: 'var(--font-heading)',
                  letterSpacing: '0.5px',
                  marginBottom: '4px',
                }}
              >
                Date and Time coming soon
              </Text>
            );
          }

          if (sessions.length === 1) {
            const session = sessions[0];
            const showSessionName = session.name && !session.name.includes('Main Session');
            return (
              <Stack gap={2} style={{ marginBottom: '4px' }}>
                {showSessionName && (
                  <Text
                    fw={600}
                    c="dimmed"
                    size="sm"
                    style={{
                      fontFamily: 'var(--font-heading)',
                    }}
                  >
                    {session.name}
                  </Text>
                )}
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
                    {formatUtcToLocalDate(session.startTime, eventTimeZone, {
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
                    {formatUtcTimeRange(session.startTime, session.endTime, eventTimeZone)}
                  </Text>
                </Group>
              </Stack>
            );
          }

          // Multiple sessions
          return (
            <Stack gap={8} style={{ marginBottom: '4px' }}>
              {sessions.map((session: any, index: number) => {
                const showSessionName = session.name && !session.name.includes('Main Session');
                return (
                  <Stack key={session.id || index} gap={2}>
                    {showSessionName && (
                      <Text
                        fw={600}
                        c="dimmed"
                        size="sm"
                        style={{
                          fontFamily: 'var(--font-heading)',
                        }}
                      >
                        {session.name}
                      </Text>
                    )}
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
                        {formatUtcToLocalDate(session.startTime, eventTimeZone, {
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
                        {formatUtcTimeRange(session.startTime, session.endTime, eventTimeZone)}
                      </Text>
                    </Group>
                  </Stack>
                );
              })}
            </Stack>
          );
        })()}

        {/* Additional sessions available badge - only show when user has tickets for SOME
            sessions but not all. Don't show for RSVP-only users (they see all sessions already). */}
        {event.hasTicket && (event as any).additionalSessionsAvailable > 0 && (
          <Badge
            color="blue"
            variant="light"
            size="sm"
            style={{ marginTop: '4px', alignSelf: 'flex-start' }}
          >
            {(event as any).additionalSessionsAvailable} additional {(event as any).additionalSessionsAvailable === 1 ? 'session' : 'sessions'} available
          </Badge>
        )}


        {/* Tickets Section - Show purchased tickets OR "no tickets" indicator */}
        {(() => {
          const tickets = (event as any).tickets as Array<{ ticketTypeName?: string; sessionName?: string | null }> | undefined;
          const hasTickets = tickets && tickets.length > 0;
          const hasAvailableTickets = (event as any).hasAvailableTickets === true;

          // User has purchased tickets — show green box with ticket details
          if (hasTickets) {
            return (
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
                    {tickets!.length === 1 ? 'Ticket' : 'Tickets'}
                  </Text>
                </Group>
                <Stack gap={4}>
                  {tickets!.map((ticket, index) => {
                    const showSession = ticket.sessionName && !ticket.sessionName.includes('Main Session');
                    return (
                      <Text key={index} size="sm" fw={500} c="var(--color-charcoal)">
                        {ticket.ticketTypeName || 'Ticket'}
                        {showSession && ` \u2022 ${ticket.sessionName}`}
                      </Text>
                    );
                  })}
                </Stack>
              </Box>
            );
          }

          // User has RSVP but no tickets, and event has ticket types — show "no tickets" indicator
          if (!event.hasTicket && hasAvailableTickets) {
            return (
              <Box
                style={{
                  background: 'linear-gradient(135deg, rgba(218, 165, 32, 0.08) 0%, rgba(184, 134, 11, 0.08) 100%)',
                  borderRadius: '8px',
                  padding: 'var(--space-xs)',
                  border: '1px solid rgba(218, 165, 32, 0.25)',
                }}
              >
                <Group gap="xs">
                  <IconTicket size={16} color="var(--mantine-color-yellow-8)" />
                  <Text fw={600} size="sm" c="var(--mantine-color-yellow-8)">
                    No tickets purchased
                  </Text>
                </Group>
                <Text size="xs" c="dimmed" mt={2}>
                  Tickets are available for this event
                </Text>
              </Box>
            );
          }

          return null;
        })()}

        {/* Your Tickets Section - Show when user has extra tickets (more than just their own) */}
        {assignedTickets.length > 1 && (
          <YourTicketsSection
            tickets={assignedTickets}
            eventId={event.id || ''}
            eventTitle={event.title || ''}
            isMobile={isMobile ?? false}
          />
        )}

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
                  <Group key={shift.id} justify="space-between" wrap="nowrap">
                    <Text size="sm" fw={500} c="var(--color-charcoal)">
                      {shift.positionTitle}
                      {showSessionName && ` • ${shift.sessionName}`}
                    </Text>
                    {shift.sessionStartTime && shift.sessionEndTime && (
                      <Text size="sm" fw={500} c="var(--color-charcoal)" style={{ flexShrink: 0 }}>
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
          {event.registrationStatus?.includes('(Social Event)') && (
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

        {/* Purchase Ticket Button - Show for RSVP users on events with paid tickets available */}
        {showPurchaseButton && (
          <Button
            onClick={handlePurchaseClick}
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

// ---------------------------------------------------------------------------
// Your Tickets Section - Sub-components for ticket assignment management
// ---------------------------------------------------------------------------

interface YourTicketsSectionProps {
  tickets: AssignedTicketStatusDto[]
  eventId: string
  eventTitle: string
  isMobile: boolean
}

/**
 * Displays a "Your Tickets" section within the EventCard showing all tickets
 * the user purchased for this event with assignment status and action buttons.
 *
 * Design reference: ui-design.md Screen 5 - Dashboard My Tickets
 */
const YourTicketsSection: React.FC<YourTicketsSectionProps> = ({
  tickets,
  eventId,
  eventTitle,
  isMobile,
}) => {
  const [assignModalTicket, setAssignModalTicket] = useState<AssignedTicketStatusDto | null>(null)
  const [assignMode, setAssignMode] = useState<'assign' | 'reassign'>('assign')

  const handleOpenAssign = useCallback((ticket: AssignedTicketStatusDto, mode: 'assign' | 'reassign') => {
    setAssignModalTicket(ticket)
    setAssignMode(mode)
  }, [])

  const handleCloseModal = useCallback(() => {
    setAssignModalTicket(null)
  }, [])

  return (
    <>
      <Box
        onClick={(e: React.MouseEvent) => e.stopPropagation()}
        style={{
          background: 'linear-gradient(135deg, rgba(155, 74, 117, 0.06) 0%, rgba(136, 1, 36, 0.06) 100%)',
          borderRadius: '8px',
          padding: 'var(--space-xs)',
          border: '1px solid rgba(155, 74, 117, 0.15)',
        }}
        data-testid="your-tickets-section"
      >
        <Group gap="xs" mb={8}>
          <IconUsers size={16} color="var(--color-plum)" />
          <Text
            fw={700}
            size="sm"
            c="var(--color-plum)"
            tt="uppercase"
            style={{ letterSpacing: '0.5px' }}
          >
            Your Tickets
          </Text>
        </Group>
        <Stack gap="xs">
          {tickets.map((ticket) => (
            <TicketAssignmentRow
              key={ticket.attendanceId || ticket.ticketPurchaseId}
              ticket={ticket}
              eventId={eventId}
              isMobile={isMobile}
              onAssign={() => handleOpenAssign(ticket, 'assign')}
              onReassign={() => handleOpenAssign(ticket, 'reassign')}
            />
          ))}
        </Stack>
      </Box>

      {/* Assign/Reassign Modal - renders outside the card click area */}
      {assignModalTicket && (
        <AssignTicketModal
          ticket={assignModalTicket}
          eventId={eventId}
          eventTitle={eventTitle}
          mode={assignMode}
          onClose={handleCloseModal}
        />
      )}
    </>
  )
}

YourTicketsSection.displayName = 'YourTicketsSection'

// ---------------------------------------------------------------------------

interface TicketAssignmentRowProps {
  ticket: AssignedTicketStatusDto
  eventId: string
  isMobile: boolean
  onAssign: () => void
  onReassign: () => void
}

/**
 * A single row within the "Your Tickets" section.
 * Shows ticket type name, status badge, and action button if applicable.
 *
 * Layout: Group with justify="space-between"
 * - Left: ticket type name + TicketStatusBadge
 * - Right: Assign or Reassign button (when applicable)
 */
const TicketAssignmentRow: React.FC<TicketAssignmentRowProps> = ({
  ticket,
  isMobile,
  onAssign,
  onReassign,
}) => {
  const status = ticket.status || 'Unassigned'
  const isOwnTicket = status.toLowerCase() === 'active' && !ticket.assignedToUserId && !ticket.isUnassigned
  const showAssignButton = ticket.isUnassigned === true
  const showReassignButton = ticket.canReassign === true && status.toLowerCase() === 'declined'

  return (
    <Box
      style={{
        background: 'rgba(255, 255, 255, 0.5)',
        borderRadius: '6px',
        padding: '8px 12px',
      }}
    >
      {isMobile ? (
        // Mobile: stacked layout
        <Stack gap={4}>
          <Text size="sm" fw={600} c="var(--color-charcoal)">
            {ticket.ticketTypeName || 'Ticket'}
          </Text>
          <Group justify="space-between" wrap="nowrap">
            <TicketStatusBadge
              status={status}
              isOwnTicket={isOwnTicket}
              assigneeSceneName={ticket.assignedToSceneName ?? undefined}
            />
            {showAssignButton && (
              <Button
                size="xs"
                variant="outline"
                color="burgundy"
                onClick={(e: React.MouseEvent) => {
                  e.stopPropagation()
                  onAssign()
                }}
                data-testid="assign-ticket-button"
                styles={{
                  root: {
                    height: 'auto',
                    minHeight: '30px',
                    padding: '4px 12px',
                    lineHeight: 1.2,
                    flexShrink: 0,
                  },
                }}
              >
                Assign
              </Button>
            )}
            {showReassignButton && (
              <Button
                size="xs"
                variant="outline"
                color="burgundy"
                onClick={(e: React.MouseEvent) => {
                  e.stopPropagation()
                  onReassign()
                }}
                data-testid="reassign-ticket-button"
                styles={{
                  root: {
                    height: 'auto',
                    minHeight: '30px',
                    padding: '4px 12px',
                    lineHeight: 1.2,
                    flexShrink: 0,
                  },
                }}
              >
                Reassign
              </Button>
            )}
          </Group>
        </Stack>
      ) : (
        // Desktop: inline layout
        <Group justify="space-between" wrap="nowrap">
          <Group gap="sm" wrap="nowrap">
            <Text size="sm" fw={600} c="var(--color-charcoal)" style={{ flexShrink: 0 }}>
              {ticket.ticketTypeName || 'Ticket'}
            </Text>
            <TicketStatusBadge
              status={status}
              isOwnTicket={isOwnTicket}
              assigneeSceneName={ticket.assignedToSceneName ?? undefined}
            />
          </Group>
          {showAssignButton && (
            <Button
              size="xs"
              variant="outline"
              color="burgundy"
              onClick={(e: React.MouseEvent) => {
                e.stopPropagation()
                onAssign()
              }}
              data-testid="assign-ticket-button"
              styles={{
                root: {
                  height: 'auto',
                  minHeight: '30px',
                  padding: '4px 12px',
                  lineHeight: 1.2,
                  flexShrink: 0,
                },
              }}
            >
              Assign
            </Button>
          )}
          {showReassignButton && (
            <Button
              size="xs"
              variant="outline"
              color="burgundy"
              onClick={(e: React.MouseEvent) => {
                e.stopPropagation()
                onReassign()
              }}
              data-testid="reassign-ticket-button"
              styles={{
                root: {
                  height: 'auto',
                  minHeight: '30px',
                  padding: '4px 12px',
                  lineHeight: 1.2,
                  flexShrink: 0,
                },
              }}
            >
              Reassign
            </Button>
          )}
        </Group>
      )}
    </Box>
  )
}

TicketAssignmentRow.displayName = 'TicketAssignmentRow'

// ---------------------------------------------------------------------------

interface AssignTicketModalProps {
  ticket: AssignedTicketStatusDto
  eventId: string
  eventTitle: string
  mode: 'assign' | 'reassign'
  onClose: () => void
}

/**
 * Wrapper component that manages the assign/reassign mutation for a specific ticket.
 * Uses the mutation hooks with the correct attendanceId from the ticket being acted upon.
 */
const AssignTicketModal: React.FC<AssignTicketModalProps> = ({
  ticket,
  eventId,
  eventTitle,
  mode,
  onClose,
}) => {
  const attendanceId = ticket.attendanceId || ''

  const assignMutation = useAssignTicket(eventId, attendanceId)
  const reassignMutation = useReassignTicket(eventId, attendanceId)

  const isPending = mode === 'reassign' ? reassignMutation.isPending : assignMutation.isPending
  const isSuccess = mode === 'reassign' ? reassignMutation.isSuccess : assignMutation.isSuccess

  // Close modal on successful mutation
  useEffect(() => {
    if (isSuccess) {
      onClose()
    }
  }, [isSuccess, onClose])

  const handleAssign = useCallback((userId: string) => {
    const request: AssignTicketRequest = { assignToUserId: userId }

    if (mode === 'reassign') {
      reassignMutation.mutate(request)
    } else {
      assignMutation.mutate(request)
    }
  }, [mode, assignMutation, reassignMutation])

  return (
    <AssignTicketDropdown
      opened={true}
      onClose={onClose}
      eventId={eventId}
      onAssign={handleAssign}
      isAssigning={isPending}
      title={mode === 'reassign' ? 'Reassign Ticket' : 'Assign Ticket'}
      ticketTypeName={ticket.ticketTypeName}
      eventTitle={eventTitle}
    />
  )
}

AssignTicketModal.displayName = 'AssignTicketModal'
