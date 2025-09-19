import type { PaginationParams } from './api.types'

// Event models based on existing API structure
export interface EventDto {
  id: string
  title: string
  description: string
  startDate: string // ISO date string
  endDate?: string | null
  location: string
  eventType?: string
  capacity?: number
  registrationCount?: number
  createdAt: string
  updatedAt: string
  isPublished?: boolean
  // New fields from API
  sessions?: EventSessionDto[]
  ticketTypes?: EventTicketTypeDto[]
  teacherIds?: string[]
  volunteerPositions?: VolunteerPositionDto[]
}

export interface CreateEventDto {
  title: string
  description: string
  startDate: string
  endDate?: string
  location: string
  capacity?: number
}

export interface UpdateEventDto {
  id: string
  title?: string
  description?: string
  startDate?: string    // Backend expects StartDate field
  endDate?: string      // Backend expects EndDate field
  location?: string
  capacity?: number
  price?: number
  isPublished?: boolean
  eventType?: 'Class' | 'Social'
  policies?: string
  teacherIds?: string[]
  sessions?: EventSessionDto[]
  ticketTypes?: EventTicketTypeDto[]
  volunteerPositions?: VolunteerPositionDto[]
}

// Supporting interfaces for complex fields
export interface EventSessionDto {
  id: string
  sessionIdentifier: string  // Required by API (e.g., "S1", "S2")
  name: string
  date: string              // Required by API (date portion for display)
  startTime: string
  endTime: string
  capacity: number
  registeredCount?: number  // Current number of registered attendees
  description?: string
}

export interface EventTicketTypeDto {
  id: string
  name: string
  type: string
  price: number
  maxPrice?: number
  quantityAvailable: number
  sessionIdentifiers?: string[]
  salesEndDate?: string
}

export interface VolunteerPositionDto {
  id: string
  title: string
  description: string
  slotsNeeded: number
  slotsFilled: number
  sessionId?: string
  requirements?: string
  requiresExperience?: boolean
}

export interface EventFilters extends PaginationParams {
  startDate?: string
  endDate?: string
  location?: string
  search?: string
  hasCapacity?: boolean
}

export interface RegistrationDto {
  id: string
  eventId: string
  userId: string
  registrationDate: string
  status: 'confirmed' | 'waitlist' | 'cancelled'
}

// Query keys for type safety
export type EventQueryKey = 
  | ['events']
  | ['events', 'list'] 
  | ['events', 'list', EventFilters]
  | ['events', 'detail']
  | ['events', 'detail', string]
  | ['events', 'registrations']
  | ['events', 'registrations', string]