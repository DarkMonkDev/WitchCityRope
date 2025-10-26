// Event Session Matrix Types - extends base event types with session functionality
import type { EventDto } from './events.types';

// Extended Event DTO with sessions and ticket types
export interface EventWithSessionsDto {
  sessions: EventSessionDto[];
  ticketTypes: EventTicketTypeDto[];
}

// Event Session Domain Model
export interface EventSessionDto {
  id: string;
  eventId: string;
  sessionIdentifier: string; // S1, S2, S3, etc.
  name: string;
  date: string; // ISO date string
  startTime: string; // HH:MM format
  endTime: string; // HH:MM format
  capacity: number;
  registeredCount: number;
  isRequired: boolean;
  createdAt: string;
  updatedAt: string;
}

// Event Ticket Type Domain Model
export interface EventTicketTypeDto {
  id: string;
  eventId: string;
  name: string;
  description?: string;
  pricingType: 'fixed' | 'sliding-scale';
  price?: number; // For fixed pricing
  minPrice?: number; // For sliding scale
  maxPrice?: number; // For sliding scale
  defaultPrice?: number; // Default/suggested price for sliding scale
  quantityAvailable?: number; // null = unlimited
  quantitySold: number;
  salesStartDate?: string; // ISO date string
  salesEndDate?: string; // ISO date string
  isRsvpMode: boolean; // true = no payment required
  isActive: boolean;
  sessionIdentifiers: string[]; // Sessions included in this ticket type
  createdAt: string;
  updatedAt: string;
}

// Create/Update DTOs
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

// Registration with sessions
export interface EventRegistrationDto {
  id: string;
  eventId: string;
  userId: string;
  ticketTypeId: string;
  sessionIdentifiers: string[]; // Which sessions the user is registered for
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