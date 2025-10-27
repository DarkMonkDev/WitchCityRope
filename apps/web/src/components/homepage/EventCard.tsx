import React from 'react'
import { Box, Text, Group } from '@mantine/core'
import { EventDto } from '@witchcityrope/shared-types'
import { calculateEventPriceRange } from '../../utils/eventUtils'

interface EventCardProps {
  event: EventDto
  /** Availability status */
  status?: {
    type: 'available' | 'limited' | 'full'
    text: string
  }
  /** Additional event details */
  details?: {
    duration?: string
    level?: string
    spots?: string
  }
  /** Click handler for the card */
  onClick?: () => void
}

export const EventCard: React.FC<EventCardProps> = ({
  event,
  status,
  details = {
    duration: '2.5 hours',
    level: 'Beginner',
    spots: 'Salem Studio',
  },
  onClick,
}) => {
  // Calculate price from ticket types
  const displayPrice = calculateEventPriceRange((event as any).ticketTypes || []);
  // Calculate status dynamically based on event data
  const calculateStatus = () => {
    if (!status) {
      // Calculate based on event type and actual data
      const capacity = event.capacity || 0
      const isSocialEvent = event.eventType?.toLowerCase() === 'social'
      const currentCount = isSocialEvent ? event.currentRSVPs || 0 : event.currentTickets || 0
      const available = capacity - currentCount

      // Determine status type
      let statusType: 'available' | 'limited' | 'full' = 'available'
      if (available <= 0) {
        statusType = 'full'
      } else if (available <= 3) {
        statusType = 'limited'
      }

      // Generate status text
      let statusText = ''
      if (isSocialEvent) {
        if (available <= 0) {
          statusText = 'RSVPs Full'
        } else if (available <= 3) {
          statusText = `Only ${available} RSVP${available !== 1 ? 's' : ''} left!`
        } else {
          statusText = `${currentCount}/${capacity} RSVPs`
        }
      } else {
        if (available <= 0) {
          statusText = 'Sold Out'
        } else if (available <= 3) {
          statusText = `Only ${available} ticket${available !== 1 ? 's' : ''} left!`
        } else {
          statusText = `${available} of ${capacity} tickets`
        }
      }

      return { type: statusType, text: statusText }
    }
    return status
  }

  const finalStatus = calculateStatus()

  const formatDateTime = (startDate?: string, endDate?: string) => {
    if (!startDate) return 'TBD'
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

  const getStatusColor = (type: string) => {
    switch (type) {
      case 'available':
        return 'var(--color-success)'
      case 'limited':
        return 'var(--color-warning)'
      case 'full':
        return 'var(--color-error)'
      default:
        return 'var(--color-success)'
    }
  }

  return (
    <Box
      className="event-card"
      style={{
        display: 'block',
        background: 'white',
        borderRadius: '16px',
        overflow: 'hidden',
        boxShadow: '0 4px 12px rgba(0,0,0,0.05)',
        transition: 'all 0.3s ease',
        textDecoration: 'none',
        cursor: onClick ? 'pointer' : 'default',
      }}
      onClick={onClick}
      data-testid="event-card"
      onMouseEnter={(e) => {
        if (onClick) {
          e.currentTarget.style.transform = 'translateY(-4px)';
          e.currentTarget.style.boxShadow = '0 8px 24px rgba(136, 1, 36, 0.15)';
        }
      }}
      onMouseLeave={(e) => {
        if (onClick) {
          e.currentTarget.style.transform = 'translateY(0)';
          e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.05)';
        }
      }}
    >
      {/* Event Image Header */}
      <Box
        style={{
          height: '100px',
          background: 'linear-gradient(135deg, var(--color-plum) 0%, var(--color-burgundy) 100%)',
          position: 'relative',
          overflow: 'hidden',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: 'var(--space-md)',
        }}
      >
        {/* Pattern overlay */}
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

        <Text
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
      </Box>

      {/* Event Content */}
      <Box style={{ padding: 'var(--space-lg)' }}>
        {/* Date and Time - Split Layout */}
        <Group
          justify="space-between"
          style={{
            marginBottom: 'var(--space-xs)',
          }}
        >
          <Text
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '14px',
              fontWeight: 600,
              color: 'var(--color-burgundy)',
              textTransform: 'uppercase',
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
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '14px',
              fontWeight: 600,
              color: 'var(--color-burgundy)',
              textTransform: 'uppercase',
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

        <Text
          style={{
            color: 'var(--color-stone)',
            fontSize: '15px',
            lineHeight: 1.6,
            marginBottom: 'var(--space-md)',
          }}
          data-testid="event-description"
        >
          {event.shortDescription || ''}
        </Text>

        {/* Event Footer */}
        <Group
          justify="space-between"
          align="center"
          style={{
            paddingTop: 'var(--space-md)',
            borderTop: '1px solid var(--color-taupe)',
          }}
        >
          <Text
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '18px',
              fontWeight: 700,
              color: 'var(--color-burgundy)',
            }}
          >
            {displayPrice}
          </Text>

          <Text
            style={{
              fontSize: '14px',
              fontWeight: 600,
              color: getStatusColor(finalStatus.type),
            }}
          >
            {finalStatus.text}
          </Text>
        </Group>
      </Box>
    </Box>
  )
}
