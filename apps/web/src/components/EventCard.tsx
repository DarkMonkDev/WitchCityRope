import React from 'react'
import { Event } from '../types/Event'
import { calculateEventPriceRange, formatUtcTimeRange, formatUtcToLocalDate } from '../utils/eventUtils'
import { useEventTimeZone } from '../hooks/useEventTimeZone'

interface EventCardProps {
  event: Event
}

export const EventCard: React.FC<EventCardProps> = ({ event }) => {
  // Get configured event timezone
  const eventTimeZone = useEventTimeZone();

  // Calculate price from ticket types
  const displayPrice = calculateEventPriceRange((event as any).ticketTypes || []);
  const formatDateTime = (startDate: string, endDate?: string) => {
    // Format date using UTC to local conversion
    const datePart = formatUtcToLocalDate(startDate, eventTimeZone, {
      weekday: 'long',
      month: 'short',
      day: 'numeric'
    })

    // Format time range using TRUE UTC to local conversion
    // See: /docs/guides-setup/datetime-handling-guide.md
    const timeRange = formatUtcTimeRange(startDate, endDate || undefined, eventTimeZone)

    return `${datePart} - ${timeRange}`
  }

  return (
    <div
      className="border border-gray-300 rounded-lg p-4 shadow-md hover:shadow-lg transition-shadow"
      data-testid="event-card"
    >
      <h3 className="text-lg font-semibold mb-2" data-testid="event-title">
        {event.title}
      </h3>
      <p className="text-gray-600 mb-4" data-testid="event-description">
        {event.shortDescription || ''}
      </p>
      <div className="text-sm text-gray-500" data-testid="event-meta">
        {/* Date and Time - Split Layout */}
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
          <span>
            📅 {(() => {
              const start = new Date(event.startDate)
              return start.toLocaleDateString('en-US', {
                weekday: 'long',
                month: 'short',
                day: 'numeric',
                timeZone: eventTimeZone
              })
            })()}
          </span>
          <span>
            {formatUtcTimeRange(event.startDate, (event as any).endDate || undefined, eventTimeZone)}
          </span>
        </div>
        <p>📍 {event.location}</p>
        <p className="font-semibold text-burgundy mt-2">{displayPrice}</p>
      </div>
    </div>
  )
}
