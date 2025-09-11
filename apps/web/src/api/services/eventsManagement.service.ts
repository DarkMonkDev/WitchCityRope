/**
 * Events Management API Service
 * Provides API client methods for the Events Management System
 * Using the new DTOs: EventSummaryDto, EventDetailsDto, EventAvailabilityDto
 * @created 2025-09-06
 */

import { apiClient } from '../../lib/api/client';
import type {
  EventSummaryDto,
  EventDetailsDto,
  EventAvailabilityDto,
  EventsListResponse,
  EventsFilters
} from '@witchcityrope/shared-types';

/**
 * Events Management API Service
 * Handles API calls to the new Events Management endpoints
 */
export class EventsManagementService {
  private readonly baseUrl = '/api/events';

  /**
   * Get list of published events with filters
   * Calls: GET /api/events
   */
  async getEvents(filters: EventsFilters = {}): Promise<EventSummaryDto[]> {
    const searchParams = new URLSearchParams();
    
    if (filters.eventType) searchParams.set('eventType', filters.eventType);
    if (filters.showPast !== undefined) searchParams.set('showPast', filters.showPast.toString());
    if (filters.organizerId) searchParams.set('organizerId', filters.organizerId);
    
    const query = searchParams.toString();
    const endpoint = query ? `${this.baseUrl}?${query}` : this.baseUrl;
    
    try {
      const response = await apiClient.get(endpoint);
      
      // Handle both direct array response and wrapped response
      if (Array.isArray(response.data)) {
        return response.data;
      } else if (response.data?.events) {
        return response.data.events;
      }
      
      console.warn('Unexpected events API response format:', response.data);
      return [];
    } catch (error) {
      console.error('Failed to fetch events:', error);
      throw new Error('Failed to fetch events');
    }
  }

  /**
   * Get complete event details including sessions and ticket types
   * Calls: GET /api/events/{eventId}
   */
  async getEventDetails(eventId: string): Promise<EventDetailsDto> {
    try {
      const response = await apiClient.get(`${this.baseUrl}/${eventId}`);
      
      if (!response.data) {
        throw new Error('Event not found');
      }
      
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 404) {
        throw new Error('Event not found');
      }
      console.error('Failed to fetch event details:', error);
      throw new Error('Failed to fetch event details');
    }
  }

  /**
   * Get real-time availability for an event
   * Calls: GET /api/events/{eventId}/availability
   */
  async getEventAvailability(eventId: string): Promise<EventAvailabilityDto> {
    try {
      const response = await apiClient.get(`${this.baseUrl}/${eventId}/availability`);
      
      if (!response.data) {
        throw new Error('Availability not found');
      }
      
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 404) {
        throw new Error('Event not found');
      }
      console.error('Failed to fetch event availability:', error);
      throw new Error('Failed to fetch event availability');
    }
  }
}

// Export singleton instance
export const eventsManagementService = new EventsManagementService();
export default eventsManagementService;