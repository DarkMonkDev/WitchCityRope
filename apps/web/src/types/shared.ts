// Type re-exports from shared-types for easier consumption
import type { components, operations } from '@witchcityrope/shared-types';

// User-related types
export type UserDto = components['schemas']['UserDto'];
export type UserRole = components['schemas']['UserRole'];

// Auth-related types
export type LoginRequest = components['schemas']['LoginRequest'];
export type LoginResponse = components['schemas']['LoginResponse'];

// Event-related types
export type EventDto = components['schemas']['EventDto'];
export type EventType = components['schemas']['EventType'];
export type EventStatus = components['schemas']['EventStatus'];
export type CreateEventRequest = components['schemas']['CreateEventRequest'];
export type EventListResponse = components['schemas']['EventListResponse'];

// Operation types for API calls
export type GetCurrentUserOperation = operations['GetCurrentUser'];
export type LoginOperation = operations['Login'];
export type GetEventsOperation = operations['GetEvents'];
export type CreateEventOperation = operations['CreateEvent'];

// Export the full components and operations types for advanced usage
export type { components, operations };