import React from 'react'
import { Event } from '../types/Event'
import { calculateEventPriceRange } from '../utils/eventUtils'

interface EventCardProps {
  event: Event
}

export const EventCard: React.FC<EventCardProps> = ({ event }) => {
  // Calculate price from ticket types
  const displayPrice = calculateEventPriceRange((event as any).ticketTypes || []);
  const formatDateTime = (startDate: string, endDate?: string) => {
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
                day: 'numeric'
              })
            })()}
          </span>
          <span>
            {(() => {
              const start = new Date(event.startDate)
              const startTime = start.toLocaleTimeString('en-US', {
                hour: 'numeric',
                minute: '2-digit',
                hour12: true
              }).toLowerCase()

              const endDate = (event as any).endDate
              if (!endDate) return startTime

              const end = new Date(endDate)
              const endTime = end.toLocaleTimeString('en-US', {
                hour: 'numeric',
                minute: '2-digit',
                hour12: true
              }).toLowerCase()

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
