// Type re-exports from shared-types for easier consumption
import type { components, operations } from '@witchcityrope/shared-types';

// User-related types
export type UserDto = components['schemas']['UserDto'];
// UserRole may not exist in generated types, fallback to string literal
export type UserRole = string; // components['schemas']['UserRole'] if it exists

// Auth-related types
export type LoginRequest = components['schemas']['LoginRequest'];
export type LoginResponse = components['schemas']['LoginResponse'];

// Event-related types
export type EventDto = components['schemas']['EventDto'];
// EventType, EventStatus, CreateEventRequest may not exist, use fallbacks
export type EventType = string; // components['schemas']['EventType'] if it exists
export type EventStatus = string; // components['schemas']['EventStatus'] if it exists
export type CreateEventRequest = any; // components['schemas']['CreateEventRequest'] if it exists
// Fix naming - use the correct generated type name
export type EventListResponse = components['schemas']['EventDtoListApiResponse'];

// Operation types for API calls
export type GetCurrentUserOperation = operations['GetCurrentUser'];
export type LoginOperation = operations['Login'];
export type GetEventsOperation = operations['GetEvents'];
// CreateEvent operation may not exist, use fallback
export type CreateEventOperation = any; // operations['CreateEvent'] if it exists

// Export the full components and operations types for advanced usage
export type { components, operations };