import React from 'react'
import { Box, Text, Group, Stack, Paper, Title, Badge, Button } from '@mantine/core'
import { useNavigate } from 'react-router-dom'
import type { components } from '@witchcityrope/shared-types'
import { calculateEventPriceRange, formatUtcToLocalDate, formatUtcTimeRange } from '../../utils/eventUtils'
import { useEventTimeZone } from '../../hooks/useEventTimeZone'
import { useCurrentUser } from '../../lib/api/hooks/useAuth'
import { useParticipation } from '../../hooks/useParticipation'

export type EventDto = components['schemas']['EventDto']

interface PublicEventCardProps {
  event: EventDto
  variant?: 'homepage' | 'list'
  showParticipationStatus?: boolean
  onClick?: () => void
  'data-testid'?: string
}

export const PublicEventCard: React.FC<PublicEventCardProps> = ({
  event,
  variant = 'homepage',
  showParticipationStatus = variant === 'list',
  onClick,
  'data-testid': testId,
}) => {
  const navigate = useNavigate()
  const eventTimeZone = useEventTimeZone()
  const { data: currentUser } = useCurrentUser()
  const isAuthenticated = !!currentUser

  // Fetch participation status for authenticated users (only for list variant)
  const { data: participation } = useParticipation(
    event.id || '',
    isAuthenticated && showParticipationStatus
  )

  // Calculate price from ticket types
  const displayPrice = calculateEventPriceRange((event as any).ticketTypes || [])

  // Determine participation status for badges (list variant only)
  const hasTicket = showParticipationStatus && (participation?.hasTicket || false)
  const hasRSVP = showParticipationStatus && (participation?.hasRSVP || false)

  // Check if event has paid tickets (maxPrice > 0)
  const hasPaidTickets = (event as any).ticketTypes?.some((tt: any) => (tt.maxPrice || 0) > 0)

  // Show Purchase Ticket button only if user has RSVPed but not purchased ticket and event has paid tickets
  const shouldShowPurchaseButton = showParticipationStatus && hasRSVP && !hasTicket && hasPaidTickets

  // Calculate available spots and status
  const capacity = event.capacity || 20
  // Determine event type from boolean flags
  const allowRsvps = (event as any)?.allowRsvps ?? false
  const requireTicketPurchase = (event as any)?.requireTicketPurchase ?? true
  const isSocialEvent = allowRsvps && !requireTicketPurchase
  const currentCount = isSocialEvent
    ? (event as any).currentRSVPs || event.registrationCount || 0
    : (event as any).currentTickets || event.registrationCount || 0
  const availableSpots = capacity - currentCount

  const getSpotColor = () => {
    if (availableSpots > 10) return 'var(--color-success)'
    if (availableSpots > 3) return 'var(--color-warning)'
    return 'var(--color-error)'
  }

  // Calculate status text - thresholds match getSpotColor()
  const getStatusText = () => {
    if (isSocialEvent) {
      if (availableSpots <= 0) return 'RSVPs Full'
      if (availableSpots <= 3) return `Only ${availableSpots} left!`
      return `${currentCount} RSVPs, ${availableSpots} left`
    } else {
      if (availableSpots <= 0) return 'Sold Out'
      if (availableSpots <= 3) return `Only ${availableSpots} left!`
      return `${currentCount} sold, ${availableSpots} left`
    }
  }

  // Determine wrapper component based on variant
  const WrapperComponent = variant === 'list' ? Paper : Box

  return (
    <WrapperComponent
      className="event-card"
      data-testid={testId || 'event-card'}
      data-event-id={event.id}
      style={{
        display: variant === 'list' ? 'flex' : 'block',
        flexDirection: variant === 'list' ? 'column' : undefined,
        background: variant === 'list' ? 'var(--color-ivory)' : 'white',
        borderRadius: '16px',
        overflow: 'hidden',
        boxShadow: '0 4px 12px rgba(0,0,0,0.05)',
        transition: 'all 0.3s ease',
        textDecoration: 'none',
        cursor: onClick ? 'pointer' : 'default',
        border: variant === 'list' ? '1px solid rgba(183, 109, 117, 0.1)' : undefined,
        position: 'relative',
        height: variant === 'list' ? '100%' : undefined,
      }}
      onClick={onClick}
      onMouseEnter={(e: React.MouseEvent<HTMLDivElement>) => {
        if (onClick) {
          e.currentTarget.style.transform = 'translateY(-4px)'
          e.currentTarget.style.boxShadow =
            variant === 'list'
              ? '0 8px 24px rgba(0,0,0,0.1)'
              : '0 8px 24px rgba(136, 1, 36, 0.15)'
          if (variant === 'list') {
            e.currentTarget.style.borderColor = 'var(--color-rose-gold)'
          }
        }
      }}
      onMouseLeave={(e: React.MouseEvent<HTMLDivElement>) => {
        if (onClick) {
          e.currentTarget.style.transform = 'translateY(0)'
          e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.05)'
          if (variant === 'list') {
            e.currentTarget.style.borderColor = 'rgba(183, 109, 117, 0.1)'
          }
        }
      }}
    >
      {/* Hidden link for E2E test accessibility - list variant only */}
      {variant === 'list' && (
        <a href={`/events/${event.id}`} style={{ display: 'none' }} aria-hidden="true" tabIndex={-1}>
          {event.title}
        </a>
      )}

      {/* Gradient Header with Event Title */}
      <Box
        style={{
          height: '80px',
          background: 'linear-gradient(135deg, var(--color-plum) 0%, var(--color-burgundy) 100%)',
          position: 'relative',
          overflow: 'hidden',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '18px',
        }}
      >
        {/* Pattern overlay (homepage only) */}
        {variant === 'homepage' && (
          <Box
            style={{
              position: 'absolute',
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              backgroundImage:
                "url(\"data:image/svg+xml,%3Csvg width='100' height='100' viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M10,50 Q30,20 50,50 T90,50' stroke='%23FFFFFF' stroke-width='0.5' fill='none' opacity='0.2'/%3E%3C/svg%3E\")",
              backgroundSize: '200px 200px',
            }}
          />
        )}

        {variant === 'homepage' ? (
          <Text
            mb={0}
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '22px',
              fontWeight: 700,
              color: 'var(--color-ivory)',
              lineHeight: 1.3,
              textAlign: 'center',
              position: 'relative',
              zIndex: 1,
              textShadow: '0 2px 4px rgba(0, 0, 0, 0.3)',
            }}
            data-testid="event-title"
          >
            {event.title || 'Untitled Event'}
          </Text>
        ) : (
          <Title
            data-testid="event-title"
            order={3}
            size="lg"
            fw={700}
            ta="center"
            style={{
              fontFamily: 'var(--font-heading)',
              color: 'var(--color-ivory)',
              lineHeight: 1.3,
              textShadow: '0 2px 4px rgba(0, 0, 0, 0.3)',
              position: 'relative',
              zIndex: 1,
            }}
          >
            {event.title}
          </Title>
        )}
      </Box>

      {/* Event Content */}
      <Box
        style={{
          padding: variant === 'list' ? 'var(--space-md) var(--space-lg)' : 'var(--space-lg)',
          flex: variant === 'list' ? 1 : undefined,
          display: variant === 'list' ? 'flex' : undefined,
          flexDirection: variant === 'list' ? 'column' : undefined,
        }}
      >
        {/* Date and Time - Multi-session support */}
        {(() => {
          const sessions = ((event as any)?.sessions || []).slice().sort((a: any, b: any) =>
            new Date(a.startTime || '').getTime() - new Date(b.startTime || '').getTime()
          )

          if (sessions.length === 0) {
            return (
              <Text
                size={variant === 'list' ? 'sm' : undefined}
                fw={variant === 'list' ? 700 : 600}
                mb={variant === 'list' ? 'sm' : undefined}
                style={{
                  fontFamily: 'var(--font-heading)',
                  fontSize: variant === 'homepage' ? '14px' : undefined,
                  color: 'var(--color-burgundy)',
                  textTransform: 'uppercase',
                  letterSpacing: '0.5px',
                  marginBottom: variant === 'homepage' ? 'var(--space-xs)' : undefined,
                }}
              >
                Date and Time coming soon
              </Text>
            )
          }

          if (sessions.length === 1) {
            const session = sessions[0]
            const showSessionName = session.name && !session.name.includes('Main Session')
            return (
              <Stack
                gap={2}
                style={{ marginBottom: variant === 'homepage' ? 'var(--space-xs)' : undefined }}
                mb={variant === 'list' ? 'sm' : undefined}
              >
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
                    data-testid="event-date"
                    size={variant === 'list' ? 'sm' : undefined}
                    fw={variant === 'list' ? 700 : 600}
                    style={{
                      fontFamily: 'var(--font-heading)',
                      fontSize: variant === 'homepage' ? '14px' : undefined,
                      color: 'var(--color-burgundy)',
                      textTransform: 'uppercase',
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
                    data-testid="event-time"
                    size={variant === 'list' ? 'sm' : undefined}
                    fw={variant === 'list' ? 700 : 600}
                    style={{
                      fontFamily: 'var(--font-heading)',
                      fontSize: variant === 'homepage' ? '14px' : undefined,
                      color: 'var(--color-burgundy)',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                    }}
                  >
                    {formatUtcTimeRange(session.startTime, session.endTime, eventTimeZone)
                      .toLowerCase()}
                  </Text>
                </Group>
              </Stack>
            )
          }

          // Multiple sessions
          return (
            <Stack
              gap={8}
              style={{ marginBottom: variant === 'homepage' ? 'var(--space-xs)' : undefined }}
              mb={variant === 'list' ? 'sm' : undefined}
            >
              {sessions.map((session: any, index: number) => {
                const showSessionName = session.name && !session.name.includes('Main Session')
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
                        data-testid={variant === 'list' && index === 0 ? 'event-date' : undefined}
                        size={variant === 'list' ? 'sm' : undefined}
                        fw={variant === 'list' ? 700 : 600}
                        style={{
                          fontFamily: 'var(--font-heading)',
                          fontSize: variant === 'homepage' ? '14px' : undefined,
                          color: 'var(--color-burgundy)',
                          textTransform: 'uppercase',
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
                        data-testid={variant === 'list' && index === 0 ? 'event-time' : undefined}
                        size={variant === 'list' ? 'sm' : undefined}
                        fw={variant === 'list' ? 700 : 600}
                        style={{
                          fontFamily: 'var(--font-heading)',
                          fontSize: variant === 'homepage' ? '14px' : undefined,
                          color: 'var(--color-burgundy)',
                          textTransform: 'uppercase',
                          letterSpacing: '0.5px',
                        }}
                      >
                        {formatUtcTimeRange(session.startTime, session.endTime, eventTimeZone)
                          .toLowerCase()}
                      </Text>
                    </Group>
                  </Stack>
                )
              })}
            </Stack>
          )
        })()}

        {/* Description */}
        <Text
          mb={variant === 'homepage' ? { base: 0, sm: 'var(--space-md)' } : undefined}
          size={variant === 'list' ? 'sm' : undefined}
          style={{
            color: 'var(--color-stone)',
            fontSize: variant === 'homepage' ? '15px' : undefined,
            lineHeight: 1.6,
            marginBottom: variant === 'list' ? 'var(--space-md)' : undefined,
            flex: variant === 'list' ? 1 : undefined,
          }}
          data-testid="event-description"
        >
          {event.shortDescription || ''}
        </Text>

        {/* Event Footer */}
        <Box
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            paddingTop: variant === 'homepage' ? 'var(--space-md)' : 'var(--space-sm)',
            marginTop: variant === 'list' ? 'auto' : undefined,
            borderTop: '1px solid var(--color-taupe)',
          }}
        >
          {/* Price or Ticket Purchased Badge */}
          {hasTicket ? (
            <Badge
              color="green"
              variant="light"
              size="lg"
              styles={{
                root: {
                  fontFamily: 'var(--font-heading)',
                  fontWeight: 700,
                  fontSize: '14px',
                  textTransform: 'uppercase',
                  letterSpacing: '0.5px',
                },
              }}
            >
              Ticket Purchased
            </Badge>
          ) : (
            <Group gap="xs" align="center">
              <Text
                fw={700}
                style={{
                  fontFamily: 'var(--font-heading)',
                  fontSize: '18px',
                  color: 'var(--color-burgundy)',
                }}
              >
                {displayPrice}
              </Text>
              {hasRSVP && (
                <Badge
                  color="blue"
                  variant="light"
                  size="md"
                  styles={{
                    root: {
                      fontFamily: 'var(--font-heading)',
                      fontWeight: 600,
                      fontSize: '12px',
                      textTransform: 'uppercase',
                    },
                  }}
                >
                  RSVPed
                </Badge>
              )}
            </Group>
          )}

          {/* Capacity/Status display */}
          <Text
            fw={600}
            ta="center"
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '14px',
              color: getSpotColor(),
            }}
          >
            {getStatusText()}
          </Text>
        </Box>

        {/* Purchase Ticket Button - List variant only, for RSVPed users without tickets */}
        {shouldShowPurchaseButton && (
          <Group justify="center" mt="md" pb="xs">
            <Button
              data-testid="button-purchase-ticket"
              className="btn btn-primary"
              size="sm"
              onClick={(e) => {
                e.stopPropagation()
                // Navigate directly to checkout page with event tickets loaded
                setTimeout(() => {
                  navigate(`/checkout/${event.id}`)
                }, 0)
              }}
              styles={{
                root: {
                  fontWeight: 600,
                  height: '44px',
                  paddingTop: '12px',
                  paddingBottom: '12px',
                  fontSize: '14px',
                  lineHeight: '1.2',
                },
              }}
            >
              Purchase Ticket
            </Button>
          </Group>
        )}
      </Box>
    </WrapperComponent>
  )
}
