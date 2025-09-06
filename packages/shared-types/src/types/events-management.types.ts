/**
 * TypeScript types for Events Management API
 * Generated from C# DTOs: EventSummaryDto, EventDetailsDto, EventAvailabilityDto
 * Following DTO alignment strategy - these types mirror the backend DTOs
 * @created 2025-09-06
 */

/**
 * Event summary information for list views
 * Maps to EventSummaryDto
 */
export interface EventSummaryDto {
  /** Unique identifier for the event */
  eventId: string;
  /** Event title */
  title: string;
  /** Short description of the event */
  shortDescription: string;
  /** Event type (Class, SocialEvent) */
  eventType: string;
  /** Event start date and time */
  startDate: string; // ISO string
  /** Event end date and time */
  endDate: string; // ISO string
  /** Event venue/location */
  venue: string;
  /** Total capacity across all sessions */
  totalCapacity: number;
  /** Number of available spots */
  availableSpots: number;
  /** Whether this event has multiple sessions */
  hasMultipleSessions: boolean;
  /** Number of sessions in this event */
  sessionCount: number;
  /** Lowest price across all ticket types */
  lowestPrice: number;
  /** Highest price across all ticket types */
  highestPrice: number;
  /** Whether the event is currently published */
  isPublished: boolean;
  /** Creation timestamp */
  createdAt: string; // ISO string
  /** Last update timestamp */
  updatedAt: string; // ISO string
}

/**
 * Complete event details including sessions and ticket types
 * Maps to EventDetailsDto
 */
export interface EventDetailsDto {
  /** Unique identifier for the event */
  eventId: string;
  /** Event title */
  title: string;
  /** Short description of the event */
  shortDescription: string;
  /** Full HTML description of the event */
  fullDescription: string;
  /** Event type (Class, SocialEvent) */
  eventType: string;
  /** Policies and procedures for the event */
  policiesProcedures: string;
  /** Venue information */
  venue: VenueDto;
  /** Event organizers */
  organizers: EventOrganizerDto[];
  /** Sessions within this event */
  sessions: EventSessionDto[];
  /** Ticket types available for this event */
  ticketTypes: TicketTypeDto[];
  /** Whether the event is published */
  isPublished: boolean;
  /** Creation timestamp */
  createdAt: string; // ISO string
  /** Last update timestamp */
  updatedAt: string; // ISO string
}

/**
 * Venue information
 * Maps to VenueDto
 */
export interface VenueDto {
  /** Venue name */
  name: string;
  /** Venue address */
  address: string;
  /** Maximum capacity of the venue */
  capacity: number;
}

/**
 * Event organizer information
 * Maps to EventOrganizerDto
 */
export interface EventOrganizerDto {
  /** User ID of the organizer */
  userId: string;
  /** Display name of the organizer */
  displayName: string;
  /** Role of the organizer (Primary, Secondary) */
  role: string;
}

/**
 * Session within an event
 * Maps to EventSessionDto
 */
export interface EventSessionDto {
  /** Unique identifier for this session */
  id: string;
  /** Session identifier (S1, S2, S3, etc.) */
  sessionIdentifier: string;
  /** Display name for the session */
  name: string;
  /** Date when this session occurs */
  date: string; // ISO string
  /** Start time of the session */
  startTime: string; // TimeSpan as string (HH:mm:ss)
  /** End time of the session */
  endTime: string; // TimeSpan as string (HH:mm:ss)
  /** Maximum number of attendees for this session */
  capacity: number;
  /** Current number of registered attendees */
  registeredCount: number;
  /** Number of available spots (Capacity - RegisteredCount) */
  availableSpots: number;
  /** Whether this session is required for the event */
  isRequired: boolean;
  /** Whether this session has available capacity for registration */
  hasAvailableCapacity: boolean;
  /** Full start datetime for the session (computed) */
  startDateTime?: string; // ISO string
  /** Full end datetime for the session (computed) */
  endDateTime?: string; // ISO string
}

/**
 * Ticket type information
 * Maps to TicketTypeDto
 */
export interface TicketTypeDto {
  /** Unique identifier for the ticket type */
  ticketTypeId: string;
  /** Name of the ticket type */
  name: string;
  /** Description of what this ticket type includes */
  description: string;
  /** List of session identifiers included in this ticket (S1, S2, etc.) */
  includedSessions: string[];
  /** Regular price for this ticket type */
  price: number;
  /** Member price (if applicable) */
  memberPrice?: number;
  /** Maximum quantity available for this ticket type */
  maxQuantity: number;
  /** Currently available quantity */
  availableQuantity: number;
  /** When sales end for this ticket type */
  salesEndDate: string; // ISO string
  /** Whether this ticket type is currently available for purchase */
  isAvailable: boolean;
  /** Reason why ticket type is not available (if applicable) */
  constraintReason?: string;
}

/**
 * Real-time event availability information
 * Maps to EventAvailabilityDto
 */
export interface EventAvailabilityDto {
  /** Event identifier */
  eventId: string;
  /** Event title for display */
  eventTitle: string;
  /** Whether this event has sessions configured */
  hasSessions: boolean;
  /** Overall event availability status */
  isAvailable: boolean;
  /** Total available spots across all ticket types (minimum constraint) */
  totalAvailableSpots: number;
  /** Session availability information */
  sessionAvailability: SessionAvailabilityDto[];
  /** Ticket type availability information */
  ticketTypeAvailability: TicketTypeAvailabilityDto[];
  /** Timestamp when this availability was calculated (for caching) */
  calculatedAt: string; // ISO string
}

/**
 * Availability information for a specific session
 * Maps to SessionAvailabilityDto
 */
export interface SessionAvailabilityDto {
  /** Session identifier (S1, S2, S3, etc.) */
  sessionIdentifier: string;
  /** Session display name */
  sessionName: string;
  /** Total capacity for this session */
  capacity: number;
  /** Current registered count */
  registeredCount: number;
  /** Available spots */
  availableSpots: number;
  /** Whether this session has available capacity */
  hasAvailableCapacity: boolean;
  /** Availability status for display (Available, Full, Limited) */
  availabilityStatus: string;
}

/**
 * Availability information for a specific ticket type
 * Maps to TicketTypeAvailabilityDto
 */
export interface TicketTypeAvailabilityDto {
  /** Ticket type identifier */
  ticketTypeId: string;
  /** Ticket type name for display */
  ticketTypeName: string;
  /** Session identifiers included in this ticket type */
  includedSessions: string[];
  /** Available spots for this ticket type (minimum across included sessions) */
  availableSpots: number;
  /** Whether this ticket type has available capacity */
  hasAvailableCapacity: boolean;
  /** Whether this ticket type is currently on sale */
  isCurrentlyOnSale: boolean;
  /** Limiting session (the session with the least availability) */
  limitingSession?: string;
  /** Availability status for display (Available, Full, Limited, Off Sale) */
  availabilityStatus: string;
}

/**
 * API response wrapper for events list
 */
export interface EventsListResponse {
  events: EventSummaryDto[];
}

/**
 * Events API filters for list queries
 */
export interface EventsFilters {
  eventType?: string;
  showPast?: boolean;
  organizerId?: string;
}