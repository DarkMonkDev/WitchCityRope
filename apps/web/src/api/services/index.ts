/**
 * API Services Index
 * Exports all API services for the application
 * @created 2025-09-06
 */

export { eventsManagementService, EventsManagementService } from './eventsManagement.service';
export { legacyEventsApiService, LegacyEventsApiService } from './legacyEventsApi.service';

// Export types
export type { LegacyEventDto } from './legacyEventsApi.service';