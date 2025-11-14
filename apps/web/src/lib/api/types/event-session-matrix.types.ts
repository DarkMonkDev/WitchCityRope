/**
 * Event Session Matrix Types
 *
 * ✅ DTO ALIGNMENT STRATEGY COMPLIANT
 * - Core DTOs imported from @witchcityrope/shared-types (auto-generated)
 * - Frontend-only types (filters, analytics, query keys) defined here
 * - Clear separation between backend DTOs and frontend logic types
 */

import type { components } from '@witchcityrope/shared-types';

// ========================================
// AUTO-GENERATED TYPES (From Backend DTOs)
// ========================================

/**
 * Session Data Transfer Object
 * Source: C# SessionDto via NSwag generation
 */
export type SessionDto = components['schemas']['SessionDto'];

/**
 * Ticket Type Data Transfer Object
 * Source: C# TicketTypeDto via NSwag generation
 */
export type TicketTypeDto = components['schemas']['TicketTypeDto'];

// ========================================
// FRONTEND-ONLY TYPES (Not in Backend)
// ========================================
// These types are used for frontend logic only and are not auto-generated

// Extended Event DTO with sessions and ticket types
export interface EventWithSessionsDto {
  sessions: SessionDto[];
  ticketTypes: TicketTypeDto[];
}

// Create/Update DTOs (TODO: Add to backend and auto-generate)
export interface CreateEventSessionDto {
  eventId: string;
  sessionIdentifier: string;
  name: string;
  date: string;
  startTime: string;
  endTime: string;
  capacity: number;
  isRequired?: boolean;
}

export interface UpdateEventSessionDto {
  id: string;
  name?: string;
  date?: string;
  startTime?: string;
  endTime?: string;
  capacity?: number;
  isRequired?: boolean;
}

export interface CreateEventTicketTypeDto {
  eventId: string;
  name: string;
  description?: string;
  type: 'Single' | 'Couples';
  minPrice: number;
  maxPrice: number;
  quantityAvailable?: number;
  salesStartDate?: string;
  salesEndDate?: string;
  isRsvpMode?: boolean;
  sessionIdentifiers: string[];
}

export interface UpdateEventTicketTypeDto {
  id: string;
  name?: string;
  description?: string;
  type?: 'Single' | 'Couples';
  minPrice?: number;
  maxPrice?: number;
  quantityAvailable?: number;
  salesStartDate?: string;
  salesEndDate?: string;
  isRsvpMode?: boolean;
  sessionIdentifiers?: string[];
  isActive?: boolean;
}

// Registration with sessions (TODO: Check if this exists in backend)
export interface EventRegistrationDto {
  id: string;
  eventId: string;
  userId: string;
  ticketTypeId: string;
  sessionIdentifiers: string[];
  amountPaid: number;
  registrationDate: string;
  status: 'confirmed' | 'waitlist' | 'cancelled' | 'pending_payment';
  createdAt: string;
  updatedAt: string;
}

// Event capacity and availability calculations
export interface EventCapacityInfo {
  eventId: string;
  totalCapacityBySession: Record<string, number>; // sessionId -> capacity
  totalRegisteredBySession: Record<string, number>; // sessionId -> registered count
  availableBySession: Record<string, number>; // sessionId -> available spots
  ticketTypeAvailability: Record<string, number>; // ticketTypeId -> available spots (min across sessions)
}

// Bulk operations
export interface BulkCreateSessionsDto {
  eventId: string;
  sessions: Omit<CreateEventSessionDto, 'eventId'>[];
}

export interface BulkCreateTicketTypesDto {
  eventId: string;
  ticketTypes: Omit<CreateEventTicketTypeDto, 'eventId'>[];
}

// Session reordering (for S1, S2, S3 management)
export interface ReorderSessionsDto {
  eventId: string;
  sessionIds: string[]; // Array of session IDs in desired order
}

// Enhanced event filters for session-based events
export interface EventSessionFilters {
  eventId?: string;
  sessionIdentifiers?: string[];
  hasAvailability?: boolean;
  dateFrom?: string;
  dateTo?: string;
}

export interface EventTicketTypeFilters {
  eventId?: string;
  type?: 'Single' | 'Couples';
  isActive?: boolean;
  isRsvpMode?: boolean;
  hasAvailability?: boolean;
  salesEndBefore?: string;
  salesEndAfter?: string;
}

// Query keys for React Query
export type EventSessionQueryKey =
  | ['event-sessions']
  | ['event-sessions', 'list']
  | ['event-sessions', 'list', string] // eventId
  | ['event-sessions', 'detail']
  | ['event-sessions', 'detail', string] // sessionId
  | ['event-sessions', 'capacity', string]; // eventId

export type EventTicketTypeQueryKey =
  | ['event-ticket-types']
  | ['event-ticket-types', 'list']
  | ['event-ticket-types', 'list', string] // eventId
  | ['event-ticket-types', 'detail']
  | ['event-ticket-types', 'detail', string] // ticketTypeId
  | ['event-ticket-types', 'availability', string]; // eventId

// Event analytics for session-based events
export interface EventSessionAnalytics {
  eventId: string;
  sessionAnalytics: {
    sessionIdentifier: string;
    name: string;
    capacity: number;
    registered: number;
    waitlist: number;
    revenue: number;
    averageTicketPrice: number;
  }[];
  ticketTypeAnalytics: {
    ticketTypeId: string;
    name: string;
    type: 'Single' | 'Couples';
    sold: number;
    revenue: number;
    averagePrice: number;
    sessionsIncluded: string[];
  }[];
  totalRevenue: number;
  totalRegistrations: number;
  averageRevenuePerSession: number;
}
