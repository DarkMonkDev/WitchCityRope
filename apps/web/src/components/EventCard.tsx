import React from 'react'
import { Event } from '../types/Event'

interface EventCardProps {
  event: Event
}

export const EventCard: React.FC<EventCardProps> = ({ event }) => {
  const formatDate = (isoString: string) => {
    return new Date(isoString).toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    })
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
        {event.shortDescription || event.description}
      </p>
      <div className="text-sm text-gray-500" data-testid="event-meta">
        <p>📅 {formatDate(event.startDate)}</p>
        <p>📍 {event.location}</p>
      </div>
    </div>
  )
}
