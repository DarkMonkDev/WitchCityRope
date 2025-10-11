/**
 * Legacy Events API Service
 * Works with the current API endpoints while Events Management is being implemented
 * This demonstrates the integration pattern that will be used with the new DTOs
 * @created 2025-09-06
 */

import { apiClient } from '../../lib/api/client'

/**
 * Legacy Event DTO (current API format)
 */
export interface LegacyEventDto {
  id: string
  title: string
  description: string
  startDate: string
  location: string
}

/**
 * Legacy Events API Service
 * Temporary service to demonstrate API integration with current endpoints
 */
export class LegacyEventsApiService {
  /**
   * Get list of events using current API format
   * This will be replaced with EventsManagementService.getEvents() once fully implemented
   */
  async getEvents(): Promise<LegacyEventDto[]> {
    try {
      console.log('🔍 LegacyEventsApiService.getEvents() called')
      const response = await apiClient.get('/api/events')
      console.log(
        '🔍 API response received:',
        response.status,
        response.data?.data?.length,
        'events'
      )
      return response.data?.data || []
    } catch (error) {
      console.error('🔍 Failed to fetch events:', error)
      throw new Error('Failed to fetch events')
    }
  }

  /**
   * Get event details (simulated for demo purposes)
   * This will be replaced with EventsManagementService.getEventDetails() once fully implemented
   */
  async getEventDetails(eventId: string): Promise<LegacyEventDto | null> {
    try {
      const events = await this.getEvents()
      const event = events.find((e) => e.id === eventId)
      if (!event) {
        throw new Error('Event not found')
      }
      return event
    } catch (error: unknown) {
      if (error instanceof Error && error.message === 'Event not found') {
        throw error
      }
      console.error('Failed to fetch event details:', error)
      throw new Error('Failed to fetch event details')
    }
  }
}

// Export singleton instance
export const legacyEventsApiService = new LegacyEventsApiService()
export default legacyEventsApiService
