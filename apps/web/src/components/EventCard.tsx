import React from 'react'
import { Event } from '../types/Event'
import { calculateEventPriceRange } from '../utils/eventUtils'
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
    const start = new Date(startDate)

    // Format date with abbreviated month, no year
    const datePart = start.toLocaleDateString('en-US', {
      weekday: 'long',
      month: 'short',
      day: 'numeric',
      timeZone: eventTimeZone
    })

    // Format start time using getUTCHours/getUTCMinutes (user-entered times stored as naive UTC)
    const startHours = start.getUTCHours();
    const startMinutes = start.getUTCMinutes();
    const startPeriod = startHours >= 12 ? 'pm' : 'am';
    const startHour12 = startHours % 12 || 12;
    const startMinuteStr = startMinutes.toString().padStart(2, '0');
    const startTime = `${startHour12}:${startMinuteStr} ${startPeriod}`;

    // If no end date, just return date + start time
    if (!endDate) {
      return `${datePart} - ${startTime}`
    }

    // Format end time using getUTCHours/getUTCMinutes
    const end = new Date(endDate)
    const endHours = end.getUTCHours();
    const endMinutes = end.getUTCMinutes();
    const endPeriod = endHours >= 12 ? 'pm' : 'am';
    const endHour12 = endHours % 12 || 12;
    const endMinuteStr = endMinutes.toString().padStart(2, '0');
    const endTime = `${endHour12}:${endMinuteStr} ${endPeriod}`;

    return `${datePart} - ${startTime} - ${endTime}`
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
            {(() => {
              const start = new Date(event.startDate)
              // Use getUTCHours/getUTCMinutes for user-entered times stored as naive UTC
              const startHours = start.getUTCHours();
              const startMinutes = start.getUTCMinutes();
              const startPeriod = startHours >= 12 ? 'pm' : 'am';
              const startHour12 = startHours % 12 || 12;
              const startMinuteStr = startMinutes.toString().padStart(2, '0');
              const startTime = `${startHour12}:${startMinuteStr} ${startPeriod}`;

              const endDate = (event as any).endDate
              if (!endDate) return startTime

              const end = new Date(endDate)
              const endHours = end.getUTCHours();
              const endMinutes = end.getUTCMinutes();
              const endPeriod = endHours >= 12 ? 'pm' : 'am';
              const endHour12 = endHours % 12 || 12;
              const endMinuteStr = endMinutes.toString().padStart(2, '0');
              const endTime = `${endHour12}:${endMinuteStr} ${endPeriod}`;

              return `${startTime} - ${endTime}`
            })()}
          </span>
        </div>
        <p>📍 {event.location}</p>
        <p className="font-semibold text-burgundy mt-2">{displayPrice}</p>
      </div>
    </div>
  )
}
