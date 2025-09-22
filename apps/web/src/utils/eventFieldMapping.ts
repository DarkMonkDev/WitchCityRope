/**
 * Event Field Mapping Utility
 *
 * This utility ensures consistency between:
 * - API Response: uses `startDate`, `endDate`
 * - TypeScript Types: also use `startDate`, `endDate`
 *
 * This provides a central place to handle any field transformations
 * and ensure type safety across the application.
 */

import type { EventDto } from '@witchcityrope/shared-types';

/**
 * Raw API response format (what actually comes from the server)
 */
interface ApiEventResponse {
  id?: string;
  title?: string;
  description?: string;
  startDate?: string;  // API uses this
  endDate?: string;    // API uses this
  location?: string;
  capacity?: number;
  currentAttendees?: number;
  currentRSVPs?: number;
  currentTickets?: number;
  status?: string;
  eventType?: string;
  isPublished?: boolean;
  sessions?: any[];
  ticketTypes?: any[];
  teacherIds?: string[];
  volunteerPositions?: any[];
  [key: string]: any;
}

/**
 * Maps API response fields to match TypeScript types
 * Ensures all required fields are present with proper defaults
 *
 * @param apiEvent - Raw event data from API
 * @returns EventDto with properly mapped field names
 */
export function mapApiEventToDto(apiEvent: ApiEventResponse): EventDto {
  // Create mapped event ensuring all fields are properly typed
  const mappedEvent: EventDto = {
    id: apiEvent.id || '',
    title: apiEvent.title || '',
    description: apiEvent.description || '',
    startDate: apiEvent.startDate || '',
    endDate: apiEvent.endDate || null,
    location: apiEvent.location || '',
    eventType: apiEvent.eventType,
    capacity: apiEvent.capacity,
    // Preserve individual count fields from API - critical for RSVP/ticket display
    currentAttendees: apiEvent.currentAttendees,
    currentRSVPs: apiEvent.currentRSVPs,
    currentTickets: apiEvent.currentTickets,
    isPublished: apiEvent.isPublished,
    sessions: apiEvent.sessions,
    ticketTypes: apiEvent.ticketTypes,
    teacherIds: apiEvent.teacherIds,
    volunteerPositions: apiEvent.volunteerPositions
  };

  return mappedEvent;
}

/**
 * Maps an array of API events to DTOs
 * 
 * @param apiEvents - Array of raw events from API
 * @returns Array of mapped EventDto objects
 */
export function mapApiEventsToDto(apiEvents: ApiEventResponse[]): EventDto[] {
  return apiEvents.map(mapApiEventToDto);
}

/**
 * Development helper to log field mapping issues
 */
export function logFieldMappingIssues(events: any[], context: string = 'Events') {
  if (import.meta.env.DEV && events.length > 0) {
    const firstEvent = events[0];
    const hasRequiredFields = !!firstEvent.startDate && !!firstEvent.id;

    if (!hasRequiredFields) {
      console.warn(`⚠️ Field Mapping Issue in ${context}:`, {
        issue: 'Missing required fields in event data',
        solution: 'Use mapApiEventToDto() to ensure all required fields are present',
        example: firstEvent,
      });
    }
  }
}

/**
 * Auto-detect and ensure proper field mapping in event arrays
 *
 * @param events - Array of events from API
 * @returns Array with consistent field names and all required fields
 */
export function autoFixEventFieldNames<T extends any[]>(events: T): EventDto[] {
  if (!Array.isArray(events)) {
    return [];
  }

  logFieldMappingIssues(events, 'Auto-Fix');

  return events.map((event): EventDto => {
    // Always use mapApiEventToDto to ensure consistency
    return mapApiEventToDto(event);
  });
}

/**
 * Type guard to check if an event has API field names
 */
export function hasApiFieldNames(event: any): event is ApiEventResponse {
  return event && typeof event.startDate === 'string';
}

/**
 * Type guard to check if an event has all required EventDto fields
 */
export function hasValidEventDtoFields(event: any): event is EventDto {
  return (
    event &&
    typeof event.id === 'string' &&
    typeof event.title === 'string' &&
    typeof event.description === 'string' &&
    typeof event.startDate === 'string' &&
    typeof event.location === 'string'
  );
}