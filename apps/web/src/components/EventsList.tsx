import React, { useState, useEffect } from 'react'
import { Event } from '../types/Event'
import { EventCard } from './EventCard'
import { LoadingSpinner } from './LoadingSpinner'

export const EventsList: React.FC = () => {
  const [events, setEvents] = useState<Event[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchEvents = async () => {
      try {
        // Using port 5655 as specified in the instructions
        const response = await fetch('http://localhost:5655/api/events')
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`)
        }
        const data = await response.json()
        setEvents(data)
      } catch (err) {
        console.error('Error fetching events:', err)
        setError(
          'Failed to load events. Please check that the API service is running on http://localhost:5655'
        )
      } finally {
        setLoading(false)
      }
    }

    fetchEvents()
  }, [])

  if (loading) {
    return <LoadingSpinner />
  }

  if (error) {
    return (
      <div
        className="text-red-600 p-4 border border-red-300 rounded-lg bg-red-50"
        data-testid="error-message"
      >
        <strong>Error:</strong> {error}
      </div>
    )
  }

  if (events.length === 0) {
    return (
      <div className="text-center py-8" data-testid="empty-state">
        <h3 className="text-xl font-semibold mb-2">No events available</h3>
        <p className="text-gray-600">
          The API returned no events. Check the database or hardcoded test data.
        </p>
      </div>
    )
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6" data-testid="events-grid">
      {events.map((event) => (
        <EventCard key={event.id} event={event} />
      ))}
    </div>
  )
}
