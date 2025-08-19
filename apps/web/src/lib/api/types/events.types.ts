import type { PaginationParams } from './api.types'

// Event models based on existing API structure
export interface EventDto {
  id: string
  title: string
  description: string
  startDate: string // ISO date string
  endDate?: string
  location: string
  capacity?: number
  registrationCount?: number
  createdAt: string
  updatedAt: string
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
  startDate?: string
  endDate?: string
  location?: string
  capacity?: number
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