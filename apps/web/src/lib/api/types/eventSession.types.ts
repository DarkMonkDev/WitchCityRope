// TypeScript types for Event Session Matrix - matching frontend component interfaces
import type { EventSession } from '../../../components/events/EventSessionsGrid'
import type { EventTicketType } from '../../../components/events/EventTicketTypesGrid'

// Re-export component types for consistency
export type { EventSession, EventTicketType }

// Extended types for API integration
export interface EventSessionWithBackendData extends EventSession {
  eventId: string
  createdAt: string
  updatedAt: string
}

export interface EventTicketTypeWithBackendData extends EventTicketType {
  eventId: string
  sessionIds: string[] // Backend uses session IDs rather than identifiers
  createdAt: string
  updatedAt: string
}

// Query keys for Event Session Matrix data
export const eventSessionKeys = {
  all: ['eventSessions'] as const,
  lists: () => [...eventSessionKeys.all, 'list'] as const,
  list: (eventId: string) => [...eventSessionKeys.lists(), eventId] as const,
  details: () => [...eventSessionKeys.all, 'detail'] as const,
  detail: (id: string) => [...eventSessionKeys.details(), id] as const,
}

export const ticketTypeKeys = {
  all: ['ticketTypes'] as const,
  lists: () => [...ticketTypeKeys.all, 'list'] as const,
  list: (eventId: string) => [...ticketTypeKeys.lists(), eventId] as const,
  details: () => [...ticketTypeKeys.all, 'detail'] as const,
  detail: (id: string) => [...ticketTypeKeys.details(), id] as const,
}