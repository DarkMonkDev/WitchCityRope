/**
 * Event Field Mapping Utility
 * 
 * This utility handles the field name mismatches between:
 * - API Response: uses `startDate`, `endDate` 
 * - Generated Types: expect `startDateTime`, `endDateTime`
 * 
 * This is a TEMPORARY solution until the backend API is aligned 
 * with the generated TypeScript types.
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
  status?: string;
  eventType?: string;
  [key: string]: any;
}

/**
 * Maps API response fields to match generated TypeScript types
 * 
 * @param apiEvent - Raw event data from API
 * @returns EventDto with properly mapped field names
 */
export function mapApiEventToDto(apiEvent: ApiEventResponse): EventDto {
  // Create mapped event with field name corrections
  const mappedEvent: EventDto = {
    ...apiEvent,
    // Map API field names to generated type field names
    startDateTime: apiEvent.startDate || apiEvent.startDateTime || '',
    endDateTime: apiEvent.endDate || apiEvent.endDateTime || '',
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
    const hasApiFields = !!firstEvent.startDate;
    const hasTypeFields = !!firstEvent.startDateTime;
    
    if (hasApiFields && !hasTypeFields) {
      console.warn(`⚠️ Field Mapping Issue in ${context}:`, {
        issue: 'API uses startDate/endDate but types expect startDateTime/endDateTime',
        solution: 'Use mapApiEventToDto() to handle field mapping',
        example: firstEvent,
      });
    } else if (hasTypeFields && !hasApiFields) {
      console.log(`✅ ${context}: Field names aligned with generated types`);
    }
  }
}

/**
 * TEMPORARY: Auto-detect and fix field naming in event arrays
 * This function will be removed once the API is properly aligned
 * 
 * @param events - Array of events (may have mixed field names)
 * @returns Array with consistent field names
 */
export function autoFixEventFieldNames<T extends any[]>(events: T): EventDto[] {
  if (!Array.isArray(events)) {
    return [];
  }

  logFieldMappingIssues(events, 'Auto-Fix');
  
  return events.map((event): EventDto => {
    // If event already has correct field names, return as-is
    if (event.startDateTime) {
      return event as EventDto;
    }
    
    // If event has API field names, map them
    if (event.startDate) {
      return mapApiEventToDto(event);
    }
    
    // Fallback: return as-is
    return event as EventDto;
  });
}

/**
 * Type guard to check if an event has API field names
 */
export function hasApiFieldNames(event: any): event is ApiEventResponse {
  return event && typeof event.startDate === 'string';
}

/**
 * Type guard to check if an event has generated type field names
 */
export function hasTypeFieldNames(event: any): event is EventDto {
  return event && typeof event.startDateTime === 'string';
}